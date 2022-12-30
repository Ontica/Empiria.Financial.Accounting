﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Vouchers Management                        Component : Domain Layer                            *
*  Assembly : FinancialAccounting.Vouchers.dll           Pattern   : Concrete Builder                        *
*  Type     : CancelacionCuentasResultadosVoucherBuilder License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Builds a voucher that cancels the balances of profit and loss accounts                         *
*             at a given date (cuentas de resultados).                                                       *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;

using Empiria.FinancialAccounting.BalanceEngine;
using Empiria.FinancialAccounting.BalanceEngine.Adapters;
using Empiria.FinancialAccounting.BalanceEngine.UseCases;

using Empiria.FinancialAccounting.Vouchers.Adapters;

namespace Empiria.FinancialAccounting.Vouchers.SpecialCases {

  /// <summary>Builds a voucher that cancels the balances of profit and loss
  /// accounts at a given date (cuentas de resultados).</summary>
  internal class CancelacionCuentasResultadosVoucherBuilder : VoucherBuilder {

    internal CancelacionCuentasResultadosVoucherBuilder() {
      // no-op
    }

    internal override FixedList<string> DryRun() {
      FixedList<VoucherEntryFields> entries = BuildVoucherEntries();

      return ImplementsDryRun(entries);
    }


    internal override Voucher GenerateVoucher() {
      FixedList<VoucherEntryFields> entries = BuildVoucherEntries();

      FixedList<string> issues = this.ImplementsDryRun(entries);

      Assertion.Require(issues.Count == 0,
        $"There were one or more issues generating '{base.SpecialCaseType.Name}' voucher: " +
        EmpiriaString.ToString(issues));

      var voucher = new Voucher(base.Fields);

      voucher.Save();

      CreateVoucherEntries(voucher, entries);

      return voucher;
    }


    private void CreateVoucherEntries(Voucher voucher, FixedList<VoucherEntryFields> entries) {
      foreach (var entryFields in entries) {

        entryFields.VoucherId = voucher.Id;

        voucher.AppendAndSaveEntry(entryFields);
      }
    }


    private FixedList<string> ImplementsDryRun(FixedList<VoucherEntryFields> entries) {
      var validator = new VoucherValidator(Ledger.Parse(base.Fields.LedgerUID),
                                           base.Fields.AccountingDate);

      return validator.Validate(entries);
    }


    private FixedList<TrialBalanceEntryDto> GetBalances() {
      var query = new TrialBalanceQuery {
        TrialBalanceType = TrialBalanceType.GeneracionDeSaldos,
        AccountsChartUID = base.Fields.AccountsChartUID,
        Ledgers = new[] { base.Fields.LedgerUID },
        BalancesType = BalancesType.WithCurrentBalance,
        ShowCascadeBalances = true,
        WithSubledgerAccount = true,
        InitialPeriod = new BalancesPeriod {
          FromDate = base.Fields.CalculationDate,
          ToDate = base.Fields.CalculationDate
        }
      };

      using (var usecases = TrialBalanceUseCases.UseCaseInteractor()) {
        var entries = usecases.BuildTrialBalance(query)
                              .Entries;

        return entries.Select(x => (TrialBalanceEntryDto) x)
                      .ToFixedList();
      }
    }


    private FixedList<VoucherEntryFields> BuildVoucherEntries() {
      FixedList<AccountsListItem> cancelationRulesList = base.SpecialCaseType.AccountsList.GetItems();

      FixedList<TrialBalanceEntryDto> balances = GetBalances();

      var voucherEntries = new List<VoucherEntryFields>();

      foreach (var cancelationRule in cancelationRulesList) {
        FixedList<TrialBalanceEntryDto> accountsToCancelBalances = balances.FindAll(x => x.AccountNumberForBalances.StartsWith(cancelationRule.AccountNumber));

        var ruleVoucherEntries = new List<VoucherEntryFields>();

        foreach (var accountBalance in accountsToCancelBalances) {
          VoucherEntryFields voucherEntry = BuildVoucherEntry(accountBalance);

          if (voucherEntry != null) {     /// ToDo - WARNING: Remove this IF after fix locked balances. No nulls must be returned
            ruleVoucherEntries.Add(voucherEntry);
          }
        }

        FixedList<VoucherEntryFields> targetAccountVoucherEntries = BuildTargetAccountVoucherEntry(ruleVoucherEntries,
                                                                                                   cancelationRule.TargetAccountNumber);

        ruleVoucherEntries.AddRange(targetAccountVoucherEntries);

        voucherEntries.AddRange(ruleVoucherEntries);
      }

      Assertion.Require(voucherEntries.Count >= 2,
                        "No hay saldos de cuentas de resultados por cancelar a la fecha proporcionada.");

      return voucherEntries.ToFixedList();
    }


    private FixedList<VoucherEntryFields> BuildTargetAccountVoucherEntry(List<VoucherEntryFields> accumulatedEntries,
                                                                         string targetAccountNumber) {
      decimal totalDebits = 0m;
      decimal totalCredits = 0m;

      foreach (var entry in accumulatedEntries) {
        if (entry.VoucherEntryType == VoucherEntryType.Debit) {
          totalDebits += entry.Amount;
        } else {
          totalCredits += entry.Amount;
        }
      }

      var entries = new List<VoucherEntryFields>();

      if (totalDebits != 0) {
        entries.Add(BuildVoucherEntryFields(VoucherEntryType.Credit, targetAccountNumber,
                                            "00", SubledgerAccount.Empty, totalDebits));
      }
      if (totalCredits != 0) {
        entries.Add(BuildVoucherEntryFields(VoucherEntryType.Debit, targetAccountNumber,
                                            "00", SubledgerAccount.Empty, totalCredits));
      }

      return entries.ToFixedList();
    }


    private VoucherEntryFields BuildVoucherEntry(TrialBalanceEntryDto accountBalance) {

      if (accountBalance.AccountNumberForBalances == "6.05.01.02.03.03" &&     // ToDo - WARNING: Remove this code after fix locked balances
          accountBalance.SubledgerAccountId <= 0) {
        return null;
      }

      if (accountBalance.DebtorCreditor == "Deudora" &&
          accountBalance.CurrentBalance > 0) {
        return BuildVoucherEntryFields(VoucherEntryType.Credit, accountBalance);

      } else if (accountBalance.DebtorCreditor == "Deudora" &&
                 accountBalance.CurrentBalance < 0) {

        return BuildVoucherEntryFields(VoucherEntryType.Debit, accountBalance);

      } else if (accountBalance.DebtorCreditor == "Acreedora" &&
                 accountBalance.CurrentBalance > 0) {
        return BuildVoucherEntryFields(VoucherEntryType.Debit, accountBalance);

      } else if (accountBalance.DebtorCreditor == "Acreedora" &&
                 accountBalance.CurrentBalance < 0) {
        return BuildVoucherEntryFields(VoucherEntryType.Credit, accountBalance);
      }

      throw Assertion.EnsureNoReachThisCode();
    }


    private VoucherEntryFields BuildVoucherEntryFields(VoucherEntryType entryType,
                                                       TrialBalanceEntryDto accountBalance) {
      // ToDo: Must Parse the subledger account using the id because subledgerNumbers are coming empty from the balance engine.
      var subledgerAccount = SubledgerAccount.Parse(accountBalance.SubledgerAccountId);

      return BuildVoucherEntryFields(entryType,
                                     accountBalance.AccountNumberForBalances,
                                     accountBalance.SectorCode,
                                     subledgerAccount,
                                     Math.Abs(accountBalance.CurrentBalance.Value)
                                    );
    }


    private VoucherEntryFields BuildVoucherEntryFields(VoucherEntryType entryType,
                                                   string accountNumber,
                                                   string sectorCode,
                                                   SubledgerAccount subledgerAccount,
                                                   decimal balance) {

      StandardAccount stdAccount = AccountsChart.Parse(base.Fields.AccountsChartUID)
                                                .GetStandardAccount(accountNumber);

      var ledger = Ledger.Parse(base.Fields.LedgerUID);

      LedgerAccount ledgerAccount = ledger.AssignAccount(stdAccount);

      return new VoucherEntryFields {
        Amount = balance,
        BaseCurrencyAmount = balance,
        CurrencyUID = ledger.BaseCurrency.UID,
        SectorId = Sector.Parse(sectorCode).Id,
        SubledgerAccountId = subledgerAccount.Id,
        SubledgerAccountNumber = subledgerAccount.IsEmptyInstance ?
                                                    string.Empty : subledgerAccount.Number,
        StandardAccountId = stdAccount.Id,
        LedgerAccountId = ledgerAccount.Id,
        VoucherEntryType = entryType
      };
    }


  }  // class CancelacionCuentasResultadosVoucherBuilder

}  // namespace Empiria.FinancialAccounting.Vouchers.SpecialCases
