﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Accounts Chart Edition                     Component : Domain Layer                            *
*  Assembly : FinancialAccounting.Core.dll               Pattern   : Service provider                        *
*  Type     : AccountsChartEditionCommandsProcessor      License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Processes a set of chart of accounts edition commands.                                         *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;
using System.Linq;

using Empiria.Data;

using Empiria.FinancialAccounting.AccountsChartEdition.Adapters;
using Empiria.FinancialAccounting.AccountsChartEdition.Data;

namespace Empiria.FinancialAccounting.AccountsChartEdition {

  /// <summary>Processes a set of chart of accounts edition commands.</summary>
  internal class AccountsChartEditionCommandsProcessor {

    internal AccountsChartEditionCommandsProcessor() {
      // no-op
    }


    internal OperationSummary Execute(AccountEditionCommand command) {
      Assertion.Require(command, nameof(command));

      command.Arrange();

      var actionsBuilder = new AccountsChartEditionActionsBuilder(command);

      FixedList<AccountsChartEditionAction> commandActions = actionsBuilder.BuildActions();

      if (!command.DryRun) {

        ProcessActions(commandActions);
        RefreshCache(commandActions);
      }

      return CreateOperationSummary(command);
    }


    internal FixedList<OperationSummary> Execute(FixedList<AccountEditionCommand> commands, bool dryRun) {
      Assertion.Require(commands, nameof(commands));
      Assertion.Require(commands.Count > 0, "'commands' must have at least one element.");

      foreach (var command in commands) {
        command.Arrange();
      }

      var allActions = new List<AccountsChartEditionAction>(128);

      foreach (var command in commands) {

        var actionsBuilder = new AccountsChartEditionActionsBuilder(command);

        FixedList<AccountsChartEditionAction> commandActions = actionsBuilder.BuildActions();

        allActions.AddRange(commandActions);
      }

      if (!dryRun) {

        ProcessActions(allActions);

        RefreshCache(allActions);

      }

      return CreateOperationSummaryList(commands);
    }


    #region Helpers

    private OperationSummary CreateOperationSummary(AccountEditionCommand command) {
      var summary = new OperationSummary();

      summary.Operation = command.CommandText;

      summary.Count++;
      summary.AddItem("Cuenta " + command.AccountFields.AccountNumber);
      summary.AddErrors(command.Issues);

      return summary;
    }


    private FixedList<OperationSummary> CreateOperationSummaryList(FixedList<AccountEditionCommand> commands) {
      var list = new List<OperationSummary>();

      foreach (var summaryGroup in commands.Select(x => x.CommandText)
                                           .Distinct()) {

        var summary = new OperationSummary();

        summary.Operation = summaryGroup;

        foreach (var command in commands.FindAll(x => x.CommandText == summaryGroup)) {
          summary.Count++;
          summary.AddItem("Cuenta " + command.AccountFields.AccountNumber);
          summary.AddErrors(command.Issues);
        }

        list.Add(summary);
      }

      return list.ToFixedList();
    }


    private void ProcessActions(ICollection<AccountsChartEditionAction> allActions) {
      var list = new DataOperationList("AccountsChartEdition");

      foreach (var action in allActions) {
        list.Add(action.DataOperations);
      }

      AccountEditionDataService.Execute(list);
    }


    private void RefreshCache(ICollection<AccountsChartEditionAction> allActions) {
      AccountsChart chart = allActions.First().Command.Entities.AccountsChart;

      chart.Refresh();
    }

    #endregion Helpers

  }  // class AccountsChartEditionCommandsProcessor

}  // namespace Empiria.FinancialAccounting.AccountsChartEdition
