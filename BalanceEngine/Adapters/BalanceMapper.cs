﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Balance Engine                             Component : Interface adapters                      *
*  Assembly : FinancialAccounting.BalanceEngine.dll      Pattern   : Mapper class                            *
*  Type     : BalanceMapper                              License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Methods used to map balances.                                                                  *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;

namespace Empiria.FinancialAccounting.BalanceEngine.Adapters {

  /// <summary>Methods used to map balances.</summary>
  static internal class BalanceMapper {

    #region Public mappers

    static internal BalanceDto Map(Balance balance) {
      return new BalanceDto {
        Command = balance.Command,
        Columns = MapColumns(balance.Command),
        Entries = MapToDto(balance.Entries, balance.Command)
      };
    }

    #endregion Public mappers


    #region Private methods

    private static FixedList<DataTableColumn> MapColumns(BalanceCommand command) {
      List<DataTableColumn> columns = new List<DataTableColumn>();
      if (command.TrialBalanceType == TrialBalanceType.SaldosPorAuxiliar) {
        columns.Add(new DataTableColumn("ledgerNumber", "Cont", "text"));
      } 
      if (command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta) {
        columns.Add(new DataTableColumn("ledgerName", "Cont", "text"));
      }
      columns.Add(new DataTableColumn("currencyCode", "Mon", "text"));
      columns.Add(new DataTableColumn("accountNumber", "Cuenta / Auxiliar", "text-nowrap"));
      columns.Add(new DataTableColumn("sectorCode", "Sct", "text"));
      columns.Add(new DataTableColumn("accountName", "Nombre", "text"));
      columns.Add(new DataTableColumn("currentBalance", "Saldo actual", "decimal"));
      columns.Add(new DataTableColumn("debtorCreditor", "Naturaleza", "text"));
      columns.Add(new DataTableColumn("lastChangeDate", "Último movimiento", "date"));

      return columns.ToFixedList();
    }

    static private FixedList<IBalanceEntryDto> MapToDto(FixedList<IBalanceEntry> list, BalanceCommand command) {
      switch (command.TrialBalanceType) {
        case TrialBalanceType.SaldosPorAuxiliar:
          
          var mapped = list.Select((x) => MapToBalanceBySubledgerAccount((BalanceEntry) x, command));
          return new FixedList<IBalanceEntryDto>(mapped);

        case TrialBalanceType.SaldosPorCuenta:

          var mappedItems = list.Select((x) => MapToBalanceByAccount((BalanceEntry) x, command));
          return new FixedList<IBalanceEntryDto>(mappedItems);

        default:
          throw Assertion.AssertNoReachThisCode(
                $"Unhandled balance type {command.TrialBalanceType}.");
      }
      
    }


    static private BalanceEntryDto MapToBalanceByAccount(BalanceEntry entry, BalanceCommand command) {

      var dto = new BalanceEntryDto();
      dto.ItemType = entry.ItemType;
      dto.LedgerNumber = entry.Ledger.Number;
      dto.LedgerName = entry.Ledger.Number != string.Empty ? entry.Ledger.FullName : "";
      dto.CurrencyCode = entry.Currency.Code;

      if (entry.ItemType == TrialBalanceItemType.BalanceTotalCurrency) {
        dto.AccountNumber = "";
      } else if (entry.SubledgerAccountNumber != string.Empty && command.WithSubledgerAccount) {
        dto.AccountNumber = entry.SubledgerAccountNumber;
      } else {
        dto.AccountNumber = entry.Account.Number == "Empty" ? "" : entry.Account.Number;
      }

      dto.AccountName = entry.GroupName == string.Empty ? entry.Account.Name : entry.GroupName;
      dto.SectorCode = entry.Sector.Code;
      dto.CurrentBalance = entry.CurrentBalance;
      dto.DebtorCreditor = entry.DebtorCreditor.ToString();
      dto.LastChangeDate = entry.LastChangeDate;

      return dto;
    }


    static private BalanceEntryDto MapToBalanceBySubledgerAccount(
                                    BalanceEntry entry, BalanceCommand command) {

      var dto = new BalanceEntryDto();
      dto.ItemType = entry.ItemType;
      dto.LedgerNumber = entry.Ledger.Number;
      dto.LedgerName = entry.Ledger.Number != string.Empty ? entry.Ledger.FullName : "";
      dto.CurrencyCode = entry.Currency.Code;
      dto.AccountNumber = entry.Account.Number == "Empty" ? "" : entry.Account.Number;
      dto.AccountName = entry.GroupName == string.Empty ? entry.Account.Name : entry.GroupName;
      dto.SectorCode = entry.Sector.Code;
      dto.CurrentBalance = entry.CurrentBalance;
      dto.DebtorCreditor = entry.DebtorCreditor.ToString();
      dto.LastChangeDate = entry.LastChangeDate;

      return dto;
    }


    static internal BalanceEntry MapToBalanceEntry(BalanceEntry entry) {
      
      return new BalanceEntry { 
        ItemType = entry.ItemType,
        Ledger = entry.Ledger,
        Currency = entry.Currency,
        Sector = entry.Sector,
        Account = entry.Account,
        GroupName = entry.GroupName,
        GroupNumber = entry.GroupNumber,
        CurrentBalance = entry.CurrentBalance,
        DebtorCreditor = entry.DebtorCreditor,
        LastChangeDate = entry.LastChangeDate
      };
    }

    #endregion Private methods

  } // class BalanceMapper

} // Empiria.FinancialAccounting.BalanceEngine.Adapters
