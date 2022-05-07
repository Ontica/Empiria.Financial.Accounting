﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Reconciliation Services                    Component : Domain Layer                            *
*  Assembly : FinancialAccounting.Reconciliation.dll     Pattern   : Service provider                        *
*  Type     : ReconciliationEngine                       License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Processes a financial accounting reconciliation.                                               *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;

using Empiria.FinancialAccounting.BalanceEngine;
using Empiria.FinancialAccounting.BalanceEngine.Adapters;
using Empiria.FinancialAccounting.BalanceEngine.UseCases;

using Empiria.FinancialAccounting.Datasets;

using Empiria.FinancialAccounting.Reconciliation.Adapters;

namespace Empiria.FinancialAccounting.Reconciliation {

  /// <summary>Processes a financial accounting reconciliation.</summary>
  internal class ReconciliationEngine {

    private readonly ReconciliationCommand _command;
    private readonly ReconciliationType _reconciliationType;

    public ReconciliationEngine(ReconciliationCommand command) {
      Assertion.AssertObject(command, "command");

      command.EnsureValid();

      _command = command;
      _reconciliationType = ReconciliationType.Parse(_command.ReconciliationTypeUID);
    }


    #region Methods


    internal ReconciliationResult Reconciliate() {

      FixedList<Dataset> operationalDatasets = GetOperationalDatasets();

      FixedList<OperationalEntryDto> operationalData = GetOperationalData(operationalDatasets);

      FixedList<AccountsListItem> involvedAccounts = GetAccountsToReconciliate();

      FixedList<TrialBalanceEntryDto> filteredBalances = GetFilteredBalancesForAccountstoReconciliate(involvedAccounts);


      FixedList<ReconciliationResultEntry> reconciliationResult = PerformReconciliation(involvedAccounts,
                                                                                        operationalData,
                                                                                        filteredBalances);

      return MapToReconciliationResult(operationalDatasets, reconciliationResult);
    }


    private FixedList<ReconciliationResultEntry> PerformReconciliation(FixedList<AccountsListItem> involvedAccounts,
                                                                       FixedList<OperationalEntryDto> operationalData,
                                                                       FixedList<TrialBalanceEntryDto> balances) {

      int ESTIMATED_LIST_SIZE = 2 * involvedAccounts.Count;

      var resultBuilder = new ReconciliationResultBuilder(operationalData, balances, ESTIMATED_LIST_SIZE);

      foreach (var account in involvedAccounts) {
        resultBuilder.InsertEntriesFor(account);
      }

      return resultBuilder.ToFixedList();
    }


    private ReconciliationResult MapToReconciliationResult(FixedList<Dataset> operationalDatasets,
                                                           FixedList<ReconciliationResultEntry> reconciliationResult) {
      return new ReconciliationResult(_command, operationalDatasets, reconciliationResult);
    }

    #endregion Methods

    #region Helpers


    private FixedList<AccountsListItem> GetAccountsToReconciliate() {
      AccountsList list = _reconciliationType.AccountsList;

      FixedList<AccountsListItem> items = list.GetItems();

      Assertion.Assert(items.Count > 0,
        $"No se han definido las cuentas a conciliar del tipo '{_reconciliationType.Name}'."
      );

      return items;
    }


    private FixedList<OperationalEntryDto> GetOperationalData(FixedList<Dataset> datasets) {
      int ESTIMATED_LIST_SIZE = datasets.Count * 4096;

      var list = new List<OperationalEntryDto>(ESTIMATED_LIST_SIZE);

      foreach (var dataset in datasets) {
        var reader = new OperationalEntriesReader(dataset);

        FixedList<OperationalEntryDto> entries = reader.GetEntries();

        list.AddRange(entries);
      }

      return list.ToFixedList();
    }


    private FixedList<Dataset> GetOperationalDatasets() {
      FixedList<Dataset> list = _reconciliationType.GetDatasetsList(_command.Date);

      Assertion.Assert(list.Count > 0,
            $"No se han cargado conjuntos o archivos de datos para " +
            $"la conciliación del día {_command.Date.ToString("dd/MMM/yyyy")}.");

      return list;
    }


    private FixedList<TrialBalanceEntryDto> GetFilteredBalancesForAccountstoReconciliate(FixedList<AccountsListItem> accountstoReconciliate) {
      FixedList<TrialBalanceEntryDto> allBalances = GetAllBalances(_command.Date);

      return allBalances.FindAll(x => accountstoReconciliate.Exists(y => y.AccountNumber == x.AccountNumber));


      #region Local functions

      FixedList<TrialBalanceEntryDto> GetAllBalances(DateTime reconciliationDate) {
        TrialBalanceCommand trialBalanceCommand = DetermineTrialBalanceCommand(reconciliationDate);

        using (var usecases = TrialBalanceUseCases.UseCaseInteractor()) {
          TrialBalanceDto trialBalance = usecases.BuildTrialBalance(trialBalanceCommand);

          IEnumerable<TrialBalanceEntryDto> balances = trialBalance.Entries.Select(x => (TrialBalanceEntryDto) x);

          return new FixedList<TrialBalanceEntryDto>(balances);
        }

      }  // GetAllBalances()


      TrialBalanceCommand DetermineTrialBalanceCommand(DateTime reconciliationDate) {
        const string RECONCILIATION_LEDGER_UID = "09";

        var ledger = AccountsChart.IFRS.GetLedger(RECONCILIATION_LEDGER_UID);

        return new TrialBalanceCommand {
          TrialBalanceType = TrialBalanceType.Balanza,
          AccountsChartUID = AccountsChart.IFRS.UID,
          BalancesType = BalancesType.WithMovements,
          ShowCascadeBalances = true,
          Ledgers = new string[] { ledger.UID },
          InitialPeriod = new TrialBalanceCommandPeriod {
            FromDate = reconciliationDate,
            ToDate = reconciliationDate
          }
        };

      }  // DetermineTrialBalanceCommand()

      #endregion Local functions

    }  // GetFilteredBalancesForAccountstoReconciliate()

    #endregion Helpers

  }  // class ReconciliationEngine

}  // namespace Empiria.FinancialAccounting.Reconciliation
