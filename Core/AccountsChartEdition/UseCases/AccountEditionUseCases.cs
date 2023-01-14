﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Accounts Chart Edition                     Component : Use cases Layer                         *
*  Assembly : FinancialAccounting.Core.dll               Pattern   : Use case interactor class               *
*  Type     : AccountEditionUseCases                     License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Use cases used to create or update accounts in a chart of accounts.                            *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.IO;

using Empiria.Services;
using Empiria.Storage;

using Empiria.FinancialAccounting.AccountsChartEdition.Adapters;
using Empiria.FinancialAccounting.Adapters;

namespace Empiria.FinancialAccounting.AccountsChartEdition.UseCases {

  /// <summary>Use cases used to create or update accounts in a chart of accounts.</summary>
  public class AccountEditionUseCases : UseCase {

    #region Constructors and parsers

    protected AccountEditionUseCases() {
      // no-op
    }

    static public AccountEditionUseCases UseCaseInteractor() {
      return CreateInstance<AccountEditionUseCases>();
    }

    #endregion Constructors and parsers

    #region Use cases

    public ExecutionResult<AccountDto> ExecuteCommand(AccountEditionCommand command) {
      Assertion.Require(command, nameof(command));

      command.Arrange();

      if (!command.IsValid || command.DryRun) {
        return command.MapToExecutionResult<AccountDto>();
      }

      var processor = new AccountsChartEditionCommandsProcessor();

      Account account = processor.Execute(command);

      AccountDto outcome = AccountsChartMapper.MapAccount(account);

      string message = GetCommandDoneMessage(command.CommandType, account);

      command.Done(outcome, message);

      return command.MapToExecutionResult<AccountDto>();
    }


    public FixedList<OperationSummary> ExecuteCommandsFromExcelFile(UpdateAccountsFromFileCommand command,
                                                                    InputFile excelFile) {
      Assertion.Require(command, nameof(command));
      Assertion.Require(excelFile, nameof(excelFile));

      AccountsChart chart = command.GetAccountsChart();
      DateTime applicationDate = command.ApplicationDate;

      Assertion.Require(chart.MasterData.StartDate <= applicationDate &&
                        applicationDate < chart.MasterData.EndDate,
                 $"La fecha de aplicación de cambios al catálogo {chart.Name} " +
                 $"debe estar los días {chart.MasterData.StartDate.ToString("dd/MMMM/yyyy")} " +
                 $"y {chart.MasterData.EndDate.ToShortDateString()}.");

      FileInfo excelFileInfo = FileUtilities.SaveFile(excelFile);

      var reader = new AccountsChartEditionFileReader(chart, applicationDate, excelFileInfo, command.DryRun);

      FixedList<AccountEditionCommand> commands = reader.GetCommands();

      var processor = new AccountsChartEditionCommandsProcessor();

      return processor.Execute(commands, command.DryRun);
    }

    #endregion Use cases

    #region Helpers

    private string GetCommandDoneMessage(AccountEditionCommandType commandType, Account account) {
      switch (commandType) {
        case AccountEditionCommandType.CreateAccount:
          return $"Se agregó la cuenta {account.Number} {account.Name} al catálogo de cuentas.";

        case AccountEditionCommandType.UpdateAccount:
          return $"La cuenta {account.Number} {account.Name} fue modificada satisfactoriamente.";

        case AccountEditionCommandType.FixAccountName:
          return $"Se corrigió la descripción de la cuenta {account.Number} {account.Name}.";

        default:
          throw Assertion.EnsureNoReachThisCode($"Unhandled commandType '{commandType}'.");
      }
    }

    #endregion Helpers

  }  // class AccountEditionUseCases

}  // namespace Empiria.FinancialAccounting.AccountsChartEdition.UseCases
