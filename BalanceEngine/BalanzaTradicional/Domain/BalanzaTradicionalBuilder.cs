﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Balance Engine                             Component : Domain Layer                            *
*  Assembly : FinancialAccounting.BalanceEngine.dll      Pattern   : Service provider                        *
*  Type     : BalanzaTradicional                         License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Genera los datos para el reporte de balanzas tradicionales.                                    *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;
using System.Linq;
using Empiria.Collections;
using Empiria.FinancialAccounting.BalanceEngine.Adapters;

namespace Empiria.FinancialAccounting.BalanceEngine {

  /// <summary>Genera los datos para el reporte de balanzas tradicionales.</summary>
  internal class BalanzaTradicionalBuilder {

    private readonly TrialBalanceCommand _command;

    public BalanzaTradicionalBuilder(TrialBalanceCommand command) {
      _command = command;
    }


    internal TrialBalance Build() {
      var balanzaHelper = new BalanzaTradicionalHelper(_command);

      if (_command.TrialBalanceType == TrialBalanceType.Saldos) {
        _command.WithSubledgerAccount = true;
      }

      var startTime = DateTime.Now;

      EmpiriaLog.Debug($"START BalanzaTradicional: {startTime}");

      FixedList<TrialBalanceEntry> accountEntries = balanzaHelper.GetPostingEntries();

      var helper = new TrialBalanceHelper(_command);

      helper.SetSummaryToParentEntries(accountEntries);

      FixedList<TrialBalanceEntry> parentAccounts = balanzaHelper.GetCalculatedParentAccounts(
                                                    accountEntries);

      EmpiriaLog.Debug($"AFTER GenerateSummaryEntries: {DateTime.Now.Subtract(startTime).TotalSeconds} seconds.");

      List<TrialBalanceEntry> accountEntriesMapped = helper.GetEntriesMappedForSectorization(
                                                     accountEntries.ToList());

      List<TrialBalanceEntry> accountEntriesAndSectorization = 
                              helper.GetSummaryAccountEntriesAndSectorization(accountEntriesMapped);

      EmpiriaLog.Debug($"AFTER GetSummaryEntriesAndSectorization (postingEntries): {DateTime.Now.Subtract(startTime).TotalSeconds} seconds.");

      List<TrialBalanceEntry> parentAccountEntriesAndSectorization =
                              helper.GetSummaryAccountEntriesAndSectorization(parentAccounts.ToList());

      EmpiriaLog.Debug($"AFTER GetSummaryEntriesAndSectorization (summaryEntries): {DateTime.Now.Subtract(startTime).TotalSeconds} seconds.");


      List<TrialBalanceEntry> balanzaTradicional = balanzaHelper.CombineParentsAndAccountEntries(
                                                   parentAccountEntriesAndSectorization, 
                                                   accountEntriesAndSectorization);

      EmpiriaLog.Debug($"AFTER CombineSummaryAndPostingEntries: {DateTime.Now.Subtract(startTime).TotalSeconds} seconds.");

      balanzaTradicional = GenerateBalanzaTradicional(balanzaTradicional, accountEntries);

      EmpiriaLog.Debug($"AFTER GetTrialBalanceType: {DateTime.Now.Subtract(startTime).TotalSeconds} seconds.");

      helper.RestrictLevels(balanzaTradicional);

      EmpiriaLog.Debug($"AFTER RestrictLevels: {DateTime.Now.Subtract(startTime).TotalSeconds} seconds.");

      var returnBalance = new FixedList<ITrialBalanceEntry>(
                              balanzaTradicional.Select(x => (ITrialBalanceEntry) x));

      EmpiriaLog.Debug($"END BalanzaTradicional: {DateTime.Now.Subtract(startTime).TotalSeconds} seconds.");

      return new TrialBalance(_command, returnBalance);
    }


    #region Private methods


    private List<TrialBalanceEntry> GenerateBalanzaTradicional(
                                    List<TrialBalanceEntry> balanzaTradicional,
                                    FixedList<TrialBalanceEntry> accountEntries) {

      var helper = new TrialBalanceHelper(_command);
      var balanzaHelper = new BalanzaTradicionalHelper(_command);

      FixedList<TrialBalanceEntry> groupTotalsEntries = balanzaHelper.GenerateTotalGroupEntries(
                                                         accountEntries);

      List<TrialBalanceEntry> returnedTrialBalance = 
                              balanzaHelper.CombineTotalGroupEntriesAndAccountEntries(
                              balanzaTradicional, groupTotalsEntries);

      List<TrialBalanceEntry> summaryTotalDebtorCreditorEntries =
                              helper.GenerateTotalSummaryDebtorCreditor(accountEntries.ToList());

      returnedTrialBalance = helper.CombineDebtorCreditorAndPostingEntries(returnedTrialBalance,
                                                                   summaryTotalDebtorCreditorEntries);

      List<TrialBalanceEntry> summaryTotalCurrencies = helper.GenerateTotalSummaryCurrency(
                                                              summaryTotalDebtorCreditorEntries);

      returnedTrialBalance = helper.CombineCurrencyTotalsAndPostingEntries(returnedTrialBalance, summaryTotalCurrencies);

      List<TrialBalanceEntry> summaryTotalConsolidatedByLedger =
                              helper.GenerateTotalSummaryConsolidatedByLedger(summaryTotalCurrencies);

      returnedTrialBalance = helper.CombineTotalConsolidatedByLedgerAndPostingEntries(
                            returnedTrialBalance, summaryTotalConsolidatedByLedger);

      List<TrialBalanceEntry> summaryTrialBalanceConsolidated = helper.GenerateTotalSummaryConsolidated(
                                                                     summaryTotalCurrencies);

      returnedTrialBalance = helper.CombineTotalConsolidatedAndPostingEntries(
                            returnedTrialBalance, summaryTrialBalanceConsolidated);

      returnedTrialBalance = helper.TrialBalanceWithSubledgerAccounts(returnedTrialBalance);

      return returnedTrialBalance;
    }




    private List<TrialBalanceEntry> GenerateOperationalBalance(List<TrialBalanceEntry> trialBalance) {
      var helper = new TrialBalanceHelper(_command);
      var totalByAccountEntries = new EmpiriaHashTable<TrialBalanceEntry>(trialBalance.Count);

      if (_command.ConsolidateBalancesToTargetCurrency == true) {

        foreach (var entry in trialBalance) {
          helper.SummaryByAccount(totalByAccountEntries, entry);
        }

        return totalByAccountEntries.ToFixedList().ToList();

      } else {

        return trialBalance;

      }
    }

    #endregion

  }  // class BalanzaTradicional

}  // namespace Empiria.FinancialAccounting.BalanceEngine
