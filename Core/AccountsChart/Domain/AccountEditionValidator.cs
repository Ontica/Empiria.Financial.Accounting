﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Accounts Chart                             Component : Domain Layer                            *
*  Assembly : FinancialAccounting.Core.dll               Pattern   : Service provider                        *
*  Type     : AccountEditionValidator                    License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Provides account edition validation services.                                                  *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;

using Empiria.FinancialAccounting.Adapters;

namespace Empiria.FinancialAccounting {

  /// <summary>Provides account edition validation services.</summary>
  internal class AccountEditionValidator {

    #region Fields

    private readonly AccountsChart _accountsChart;
    private readonly AccountEditionCommand _command;
    private readonly List<string> _issuesList;

    #endregion Fields

    #region Constructors and parsers

    public AccountEditionValidator(AccountEditionCommand command) {
      Assertion.Require(command, nameof(command));

      _command = command;
      _accountsChart = command.GetAccountsChart();
      _issuesList = new List<string>(16);
    }

    #endregion Constructors and parsers

    #region Properties

    public FixedList<string> Issues {
      get {
        return _issuesList.ToFixedList();
      }
    }

    #endregion Properties

    #region Methods

    internal void AddIssue(string issue) {
      Assertion.Require(issue, nameof(issue));

      _issuesList.Add(issue);
    }


    internal bool EnsureCanAddCurrenciesTo(Account account) {
      FixedList<Currency> currenciesToBeAdded = _command.GetCurrencies();

      if (currenciesToBeAdded.Count == 0) {
        this.AddIssue("No se proporcionó la lista con las nuevas monedas a registrar.");

        return false;
      }

      bool isOK = true;

      foreach (Currency currencyToAdd in currenciesToBeAdded) {
        if (account.CurrencyRules.Contains(x => x.Currency.Equals(currencyToAdd))) {
          this.AddIssue($"La cuenta ya tiene registrada la moneda {currencyToAdd.FullName}.");
          isOK = false;

        } else if (!_accountsChart.MasterData.Currencies.Contains(currencyToAdd)) {
          this.AddIssue($"El catálogo de cuentas no maneja la moneda {currencyToAdd.FullName}.");
          isOK = false;

        }
      }

      return isOK;
    }


    internal bool EnsureCanAddSectorsTo(Account account) {
      FixedList<Sector> sectorsToBeAdded = _command.GetSectors();

      if (sectorsToBeAdded.Count == 0) {
        this.AddIssue("No se proporcionó la lista con los nuevos sectores a registrar.");

        return false;
      }

      bool isOK = true;

      foreach (Sector sectorToAdd in sectorsToBeAdded) {
        if (account.SectorRules.Contains(x => x.Sector.Equals(sectorToAdd))) {
          this.AddIssue($"La cuenta ya tiene registrado el sector {sectorToAdd.FullName}.");
          isOK = false;

        } else if (sectorToAdd.HasChildren) {
          this.AddIssue($"El sector {sectorToAdd.FullName} tiene sectores hijos, " +
                        $"por lo que no puede ser asignado directamente a una cuenta.");
          isOK = false;

        } else if (!_accountsChart.MasterData.Sectors.Contains(sectorToAdd)) {
          this.AddIssue($"El catálogo de cuentas no maneja el sector {sectorToAdd.FullName}.");
          isOK = false;

        }
      }

      return isOK;
    }


    internal bool EnsureCanCreateAccount() {
      return false;
    }

    #endregion Methods

  }  // class AccountEditionValidator

}  // namespace Empiria.FinancialAccounting
