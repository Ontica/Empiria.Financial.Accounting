﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Accounts Chart Edition                     Component : Domain Layer                            *
*  Assembly : FinancialAccounting.Core.dll               Pattern   : Service provider                        *
*  Type     : AccountEditionCommandValidator             License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Provides services to validate account edition commands.                                        *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;

using Empiria.FinancialAccounting.AccountsChartEdition.Adapters;

namespace Empiria.FinancialAccounting.AccountsChartEdition {

  /// <summary>Provides services to validate account edition commands.</summary>
  internal class AccountEditionCommandValidator {

    private readonly AccountEditionCommand _command;
    private readonly List<string> _issues;

    internal AccountEditionCommandValidator(AccountEditionCommand command) {
      Assertion.Require(command, nameof(command));

      _command = command;
      _issues = new List<string>();
    }


    #region Public methods

    internal FixedList<string> GetIssues() {
      InitialRequire();

      _issues.Clear();

      switch (_command.CommandType) {
        case AccountEditionCommandType.CreateAccount:
          SetCreateAccountIssues();
          break;

        case AccountEditionCommandType.DeleteAccount:
          SetDeleteAccountIssues();
          break;

        case AccountEditionCommandType.FixAccountName:
          SetFixAccountNameIssues();
          break;

        case AccountEditionCommandType.UpdateAccount:
          SetUpdateAccountIssues();
          break;

        default:
          throw Assertion.EnsureNoReachThisCode($"Unhandled command type '{_command.CommandType}'.");
      }

      return _issues.ToFixedList();
    }


    internal void InitialRequire() {
      Assertion.Require(_command.CommandType != AccountEditionCommandType.Undefined, "CommandType");
      Assertion.Require(_command.AccountsChartUID, "AccountsChartUID");
      Assertion.Require(_command.ApplicationDate != ExecutionServer.DateMinValue, "ApplicationDate");

      Assertion.Require(_command.AccountFields, "AccountFields");
      Assertion.Require(_command.AccountFields.AccountNumber, "AccountFields.AccountNumber");
      Assertion.Require(_command.AccountFields.Name, "AccountFields.Name");
      Assertion.Require(_command.AccountFields.AccountTypeUID, "AccountFields.AccountTypeUID");
      Assertion.Require(_command.AccountFields.Role != AccountRole.Undefined,
                        "AccountFields.Role");
      Assertion.Require(_command.AccountFields.DebtorCreditor != DebtorCreditorType.Undefined,
                        "AccountFields.DebtorCreditor");

      switch (_command.CommandType) {
        case AccountEditionCommandType.CreateAccount:
          Assertion.Require(_command.AccountUID.Length == 0,
              "command.AccountUID was provided but it's not needed for a CreateAccount command.");
          return;

        case AccountEditionCommandType.DeleteAccount:
        case AccountEditionCommandType.FixAccountName:
          RequireAccountToEditIsValid();
          return;

        case AccountEditionCommandType.UpdateAccount:
          RequireAccountToEditIsValid();

          Assertion.Require(_command.DataToBeUpdated, "DataToBeUpdated");
          Assertion.Require(_command.DataToBeUpdated.Length != 0,
                            "DataToBeUpdated must contain one or more values.");
          return;

        default:
          throw Assertion.EnsureNoReachThisCode($"Unhandled command type '{_command.CommandType}'.");
      }

      void RequireAccountToEditIsValid() {
        Assertion.Require(_command.AccountUID, "AccountUID");

        AccountsChart chart = AccountsChart.Parse(_command.AccountsChartUID);
        Account account = Account.Parse(_command.AccountUID);

        Assertion.Require(account.AccountsChart.Equals(chart),
            $"Account to be edited '{account.Number}' does not belong to " +
            $"the chart of accounts {chart.Name}.");

        Assertion.Require(account.EndDate == Account.MAX_END_DATE,
            "The given accountUID corresponds to an historic account, so it can not be edited.");

        Assertion.Require(account.Number.Equals(_command.AccountFields.AccountNumber),
            "AccountFields.AccountNumber does not match with the given accountUID.");

        Assertion.Require(account.StartDate < _command.ApplicationDate,
            $"ApplicationDate ({_command.ApplicationDate.ToString("dd/MMM/yyyy")}) " +
            $"must be greater than the given account's " +
            $"start date {account.StartDate.ToString("dd/MMM/yyyy")}.");
      }
    }

    #endregion Public methods

    #region Main validators

    private void SetCreateAccountIssues() {
      AccountsChart chart = _command.Entities.AccountsChart;

      string accountNumber = _command.AccountFields.AccountNumber;

      Require(chart.IsValidAccountNumber(accountNumber),
          $"La cuenta '{accountNumber}' tiene un formato que no reconozco.");

      Account account = chart.TryGetAccount(accountNumber);

      Require(account == null,
          $"La cuenta '{accountNumber} ya existe en el catálogo de cuentas.");

      SetCurrenciesIssues();
      SetSectorsIssues();
    }


    private void SetDeleteAccountIssues() {
      Account account = _command.Entities.Account;

      Require(account.GetChildren().Count != 0,
          "La cuenta no se puede eliminar porque tiene cuentas hijas.");

      Require(account.LedgerRules.Count == 0,
          "La cuenta no se puede eliminar porque ya fue asignada a una o más contabilidades.");

    }


    private void SetFixAccountNameIssues() {
      Account account = _command.Entities.Account;

      Require(!account.Name.Equals(_command.AccountFields.Name),
          $"La descripción de la cuenta es igual a la que ya está registrada.");
    }


    private void SetUpdateAccountIssues() {
      Account account = _command.Entities.Account;

      Require(account.StartDate <= _command.ApplicationDate,
          $"El último cambio de la cuenta fue el día {account.StartDate.ToString("dd/MMM/yyyy")}, " +
          $"por lo que la fecha de aplicación de los cambios debe ser posterior a ese día.");

      SetDataToBeUpdatedIssues();
      SetChangeRoleIssues();
      SetCurrenciesIssues();
      SetSectorsIssues();
    }


    #endregion Main validators

    #region Secondary validators

    private void SetCurrenciesIssues() {

      if (_command.AccountFields.Role == AccountRole.Sumaria) {

        Require(_command.Currencies.Length == 0,
            $"La cuenta es sumaria por lo que la lista de monedas debe estar vacía.");

      } else {

        Require(_command.Currencies.Length > 0,
            $"La lista de monedas no puede estar vacía para una cuenta que no es sumaria.");
      }

      foreach (var currencyCode in _command.Currencies) {

        if (!Currency.Exists(currencyCode)) {
          Require(false,
              $"La lista de monedas contiene la moneda '{currencyCode}' que no está registrada.");
          continue;
        }

        Require(_command.Currencies.ToFixedList().CountAll(x => x == currencyCode) == 1,
            $"La lista de monedas contiene más de una vez la moneda '{currencyCode}'.");

      }
    }


    private void SetDataToBeUpdatedIssues() {
      Account account = _command.Entities.Account;

      FixedList<AccountDataToBeUpdated> dataToBeUpdated = _command.DataToBeUpdated.ToFixedList();

      bool updateRole = dataToBeUpdated.Contains(AccountDataToBeUpdated.MainRole) ||
                        dataToBeUpdated.Contains(AccountDataToBeUpdated.SubledgerRole);

      bool updateCurrencies = dataToBeUpdated.Contains(AccountDataToBeUpdated.Currencies);
      bool updateSectors = dataToBeUpdated.Contains(AccountDataToBeUpdated.Sectors);

      if (!updateRole && account.Role == AccountRole.Sumaria) {
        Require(!updateCurrencies,
            "No se pueden modificar las monedas de una cuenta sumaria.");
        Require(!updateSectors,
            "No se pueden modificar los sectores de una cuenta sumaria.");
      }
    }


    private void SetChangeRoleIssues() {
      Account account = _command.Entities.Account;

      FixedList<AccountDataToBeUpdated> dataToBeUpdated = _command.DataToBeUpdated.ToFixedList();

      bool updateRole = dataToBeUpdated.Contains(AccountDataToBeUpdated.MainRole) ||
                        dataToBeUpdated.Contains(AccountDataToBeUpdated.SubledgerRole);

      AccountRole currentRole = _command.Entities.Account.Role;
      AccountRole newRole = _command.AccountFields.Role;

      if (!updateRole) {
        Require(currentRole.Equals(newRole),
            "No se está modificando el rol, pero el rol actual de la cuenta no es igual al que se proporcionó.");
        return;
      } else if (account.Role.Equals(_command.AccountFields.Role)) {

        Require(false,
            "Se está solicitando modificar el rol, pero la cuenta ya tiene ese rol.");
        return;
      }

      if (newRole == AccountRole.Sumaria) {
        Require(account.GetChildren().Count == 0,
          "No se puede convertir la cuenta a sumaria debido a que tiene cuentas hijas.");
      }

      if (newRole != AccountRole.Sumaria) {
        Require(account.GetChildren().Count == 0,
          "No se puede convertir la cuenta de sumaria a detalle debido a que tiene cuentas hijas.");
      }
    }


    private void SetSectorsIssues() {

      if (_command.AccountFields.Role == AccountRole.Sectorizada) {

        Require(_command.SectorRules.Length != 0,
            $"La cuenta maneja sectores pero la lista de sectores está vacía.");

      } else {

        Require(_command.SectorRules.Length == 0,
            $"La cuenta no maneja sectores pero se está proporcionando una lista de sectores.");
      }


      foreach (var sectorRule in _command.SectorRules) {

        var sector = Sector.TryParse(sectorRule.Code);

        if (sector == null) {
          Require(false,
              $"La lista de sectores contiene el sector '{sectorRule.Code}' que no está registrado.");
          continue;
        }

        Require(!sector.IsSummary,
            $"Se solicita registrar el sector '({sector.Code}) {sector.Name}', " +
            $"pero dicho sector es sumarizador.");

        Require(_command.SectorRules.ToFixedList().CountAll(x => x.Code == sector.Code) == 1,
            $"La lista de sectores contiene más de una vez " +
            $"el sector '{sector.Code}'.");
      }
    }

    #endregion Secondary validators

    #region Helpers

    private void Require(bool condition, string issue) {
      if (_command.DataSource.Length != 0) {
        issue += $" {_command.DataSource}";
      }

      if (!condition) {
        _issues.Add(issue);
      }
    }

    #endregion Helpers

  }  // class AccountEditionCommandValidator

}  // namespace Empiria.FinancialAccounting.AccountsChartEdition
