﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Balance Engine                             Component : Domain Layer                            *
*  Assembly : FinancialAccounting.BalanceEngine.dll      Pattern   : Service provider                        *
*  Type     : SaldosPorCuentaYMayores                    License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Genera los datos para el reporte de saldos por cuenta y mayores.                               *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;
using System.Linq;

using Empiria.Collections;
using Empiria.FinancialAccounting.BalanceEngine.Adapters;

namespace Empiria.FinancialAccounting.BalanceEngine {

  /// <summary>Genera los datos para el reporte de saldos por cuenta y mayores.</summary>
  internal class SaldosPorCuentaYMayores {

    private readonly TrialBalanceCommand _command;

    public SaldosPorCuentaYMayores(TrialBalanceCommand command) {
      _command = command;
    }


    internal TrialBalance Build() {
      var helper = new TrialBalanceHelper(_command);

      List<TrialBalanceEntry> trialBalance = helper.GetSummaryAndPostingEntries();

      List<TrialBalanceEntry> summaryByAccountAndDelegations = GenerateTotalByAccountAndLedgers(trialBalance);

      trialBalance = helper.RestrictLevels(summaryByAccountAndDelegations);

      var returnBalance = new FixedList<ITrialBalanceEntry>(trialBalance.Select(x => (ITrialBalanceEntry) x));

      return new TrialBalance(_command, returnBalance);
    }


    #region Helper methods

    private List<TrialBalanceEntry> CombineAccountsAndLedgers(List<TrialBalanceEntry> summaryAccountList,
                                                               List<TrialBalanceEntry> trialBalance) {
      List<TrialBalanceEntry> returnedEntries = new List<TrialBalanceEntry>();

      foreach (var account in summaryAccountList) {
        List<TrialBalanceEntry> entries = new List<TrialBalanceEntry>();
        entries = trialBalance.Where(a => a.Currency.Code == account.Currency.Code &&
                                          a.Account.ParentNumber == account.Account.Number).ToList();
        foreach (var entry in entries) {
          if (entry.NotHasSector && entry.Level == 1) {
            entry.GroupName = entry.Ledger.Name;
          }
        }
        entries = entries.OrderBy(a => a.Currency.Code)
                         .ThenBy(a => a.Ledger.Number)
                         .ThenBy(a => a.Account.Number)
                         .ThenBy(a => a.Sector.Code)
                         .ToList();

        returnedEntries.Add(account);
        returnedEntries.AddRange(entries);
      }

      return returnedEntries;
    }


    private void GenerateSummaryAccount(List<TrialBalanceEntry> summaryAccountList,
                                                    List<TrialBalanceEntry> ledgersGroupList) {
      var helper = new TrialBalanceHelper(_command);

      foreach (var accountGroup in ledgersGroupList) {
        var existAccount = summaryAccountList.FirstOrDefault(
                            a => a.GroupName == accountGroup.Account.Name.ToUpper() &&
                            a.Currency.Code == accountGroup.Currency.Code &&
                            a.Account.Number == accountGroup.Account.Number);

        if (existAccount == null) {
          var summaryParentEntries = new EmpiriaHashTable<TrialBalanceEntry>();

          helper.SummaryByAccount(summaryParentEntries, accountGroup, accountGroup.Account, Sector.Empty,
                                  TrialBalanceItemType.BalanceSummary);

          summaryAccountList.AddRange(summaryParentEntries.Values.ToList());
        }
      }
    }


    private List<TrialBalanceEntry> GenerateTotalByAccountAndLedgers(
                               List<TrialBalanceEntry> trialBalance) {

      List<TrialBalanceEntry> summaryAccountList = new List<TrialBalanceEntry>();
      List<TrialBalanceEntry> ledgersGroupList = trialBalance.Where(
                                                  a => a.NotHasSector && a.Level == 1).ToList();

      GenerateSummaryAccount(summaryAccountList, ledgersGroupList);

      SumAccountListAndTotalByLedger(summaryAccountList, ledgersGroupList);

      summaryAccountList = summaryAccountList.OrderBy(a => a.Ledger.Id)
                                             .ThenBy(a => a.Currency.Code)
                                             .ThenBy(a => a.Account.Number)
                                             .ToList();

      trialBalance = CombineAccountsAndLedgers(summaryAccountList, trialBalance);

      return trialBalance;
    }


    private void SumAccountListAndTotalByLedger(List<TrialBalanceEntry> summaryAccountList,
                                                            List<TrialBalanceEntry> ledgersGroupList) {
      foreach (var summary in summaryAccountList) {
        var ledgersById = ledgersGroupList.Where(a => a.Currency.Code == summary.Currency.Code &&
                                                 a.Account.Number == summary.Account.Number).ToList();
        foreach (var ledger in ledgersById) {
          summary.Sum(ledger);
        }
      }
    }


    #endregion Helper methods

  }  // class AnaliticoDeCuentas

}  // namespace Empiria.FinancialAccounting.BalanceEngine
