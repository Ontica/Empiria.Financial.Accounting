﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Banobras Integration Services                 Component : Use cases Layer                      *
*  Assembly : FinancialAccounting.BanobrasIntegration.dll   Pattern   : Use case interactor class            *
*  Type     : ExportBalancesUseCases                        License   : Please read LICENSE.txt file         *
*                                                                                                            *
*  Summary  : Use cases used to export balances to other Banobras' systems.                                  *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

using Empiria.Services;

using Empiria.FinancialAccounting.BalanceEngine.UseCases;

using Empiria.FinancialAccounting.BalanceEngine.Adapters;
using Empiria.FinancialAccounting.BanobrasIntegration.Adapters;

namespace Empiria.FinancialAccounting.BanobrasIntegration.UseCases {

  /// <summary>Use cases used to export balances to other Banobras' systems.</summary>
  public class ExportBalancesUseCases : UseCase {

    #region Constructors and parsers

    protected ExportBalancesUseCases() {
      // no-op
    }

    static public ExportBalancesUseCases UseCaseInteractor() {
      return UseCase.CreateInstance<ExportBalancesUseCases>();
    }

    #endregion Constructors and parsers

    #region Use cases

    public FixedList<ExportedBalancesDto> ExportBalancesByDay(ExportBalancesCommand command) {
      Assertion.AssertObject(command, "command");

      TrialBalanceCommand trialBalanceCommand = command.MapToTrialBalanceCommandForBalancesByDay();

      using (var usecases = TrialBalanceUseCases.UseCaseInteractor()) {
        TrialBalanceDto trialBalance = usecases.BuildTrialBalance(trialBalanceCommand);

        return ExportBalancesMapper.MapToExportedBalances(command, trialBalance);
      }
    }


    public FixedList<ExportedBalancesDto> ExportBalancesByMonth(ExportBalancesCommand command) {
      Assertion.AssertObject(command, "command");

      TrialBalanceCommand trialBalanceCommand = command.MapToTrialBalanceCommandForBalancesByMonth();

      using (var usecases = TrialBalanceUseCases.UseCaseInteractor()) {
        TrialBalanceDto trialBalance = usecases.BuildTrialBalance(trialBalanceCommand);

        return ExportBalancesMapper.MapToExportedBalances(command, trialBalance);
      }
    }

    #endregion Use cases

  }  // class ExportBalancesUseCases

}  // namespace Empiria.FinancialAccounting.BanobrasIntegration.UseCases
