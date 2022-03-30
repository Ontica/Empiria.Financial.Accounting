﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Reporting Services                           Component : Excel Exporters                       *
*  Assembly : FinancialAccounting.Reporting.dll            Pattern   : IExcelExporter                        *
*  Type     : TrialBalanceExcelExporter                    License   : Please read LICENSE.txt file          *
*                                                                                                            *
*  Summary  : Creates a Microsoft Excel file with trial balance information.                                 *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;

using Empiria.FinancialAccounting.BalanceEngine;
using Empiria.FinancialAccounting.BalanceEngine.Adapters;

namespace Empiria.FinancialAccounting.Reporting.Exporters.Excel {

  /// <summary>Creates a Microsoft Excel file with trial balance information.</summary>
  internal class TrialBalanceExcelExporter {

    private TrialBalanceCommand _command = new TrialBalanceCommand();
    private readonly FileTemplateConfig _templateConfig;

    private readonly DateTime MIN_LAST_CHANGE_DATE_TO_REPORT = DateTime.Parse("01/01/1970");

    private ExcelFile _excelFile;

    public TrialBalanceExcelExporter(FileTemplateConfig templateConfig) {
      Assertion.AssertObject(templateConfig, "templateConfig");

      _templateConfig = templateConfig;
    }


    internal ExcelFile CreateExcelFile(TrialBalanceDto trialBalance) {
      Assertion.AssertObject(trialBalance, "trialBalance");

      _command = trialBalance.Command;

      _excelFile = new ExcelFile(_templateConfig);

      _excelFile.Open();

      SetHeader();

      SetTable(trialBalance);

      _excelFile.Save();

      _excelFile.Close();

      return _excelFile;
    }


    #region Private methods

    private void SetHeader() {
      _excelFile.SetCell($"A2", _templateConfig.Title);

      var subTitle = $"Del {_command.InitialPeriod.FromDate.ToString("dd/MMM/yyyy")} " +
                     $"al {_command.InitialPeriod.ToDate.ToString("dd/MMM/yyyy")}";

      if (_command.ValuateBalances) {
        subTitle += $". Saldos valorizados al {_command.InitialPeriod.ExchangeRateDate.ToString("dd/MMM/yyyy")}.";
      }

      _excelFile.SetCell($"A3", subTitle);
    }


    private void SetTable(TrialBalanceDto trialBalance) {
      switch (trialBalance.Command.TrialBalanceType) {
        case TrialBalanceType.AnaliticoDeCuentas:
          FillOutAnaliticoDeCuentas(trialBalance.Entries.Select(x => (TwoColumnsTrialBalanceEntryDto) x));
          return;

        case TrialBalanceType.BalanzaValorizadaComparativa:
          SetBalanzaComparativaHeaders();
          FillOutBalanzaComparativa(trialBalance.Entries.Select(x => (TrialBalanceComparativeDto) x));
          return;


        case TrialBalanceType.BalanzaEnColumnasPorMoneda:
          FillOutBalanzaColumnasPorMoneda(trialBalance.Entries.Select(x => (TrialBalanceByCurrencyDto) x));
          return;

        case TrialBalanceType.BalanzaDolarizada:
          FillOutBalanzaDolarizada(trialBalance.Entries.Select(x => (ValuedTrialBalanceDto) x));
          return;

        case TrialBalanceType.BalanzaConContabilidadesEnCascada:
          FillOutSaldosPorCuentayMayor(trialBalance.Entries.Select(x => (TrialBalanceEntryDto) x));
          return;

        case TrialBalanceType.Balanza:
        case TrialBalanceType.Saldos:
          FillOutBalanza(trialBalance.Entries.Select(x => (TrialBalanceEntryDto) x));
          return;

        case TrialBalanceType.SaldosPorAuxiliar:
          FillOutSaldosAuxiliar(trialBalance.Entries.Select(x => (TrialBalanceEntryDto) x));
          return;

        case TrialBalanceType.SaldosPorCuenta:
          FillOutSaldosCuenta(trialBalance.Entries.Select(x => (TrialBalanceEntryDto) x),
                              trialBalance.Command.WithSubledgerAccount);
          return;

        default:
          throw Assertion.AssertNoReachThisCode();
      }
    }


    private void FillOutAnaliticoDeCuentas(IEnumerable<TwoColumnsTrialBalanceEntryDto> entries) {
      int i = 5;

      foreach (var entry in entries) {
        if (_command.ShowCascadeBalances) {
          _excelFile.SetCell($"A{i}", entry.LedgerNumber);
          _excelFile.SetCell($"B{i}", entry.LedgerName);
        } else {
          _excelFile.SetCell($"A{i}", "Consolidada");
        }
        if (entry.ItemType == TrialBalanceItemType.Entry) {
          if (!entry.IsParentPostingEntry) {
            _excelFile.SetCell($"C{i}", "*");
          } else {
            _excelFile.SetCell($"C{i}", "**");
          }
        }
        _excelFile.SetCell($"D{i}", entry.AccountNumber);
        _excelFile.SetCell($"E{i}", entry.AccountName);
        _excelFile.SetCell($"F{i}", entry.SectorCode);
        _excelFile.SetCell($"G{i}", entry.DomesticBalance);
        _excelFile.SetCell($"H{i}", entry.ForeignBalance);
        _excelFile.SetCell($"I{i}", entry.TotalBalance);

        if (MustFillOutAverageBalance(entry.AverageBalance, entry.LastChangeDate)) {
          _excelFile.SetCell($"J{i}", entry.AverageBalance);
          _excelFile.SetCell($"K{i}", entry.LastChangeDate);
        }

        if (entry.ItemType != TrialBalanceItemType.Entry &&
            entry.ItemType != TrialBalanceItemType.Summary) {
          _excelFile.SetRowStyleBold(i);
        }
        i++;
      }
      if (!_command.WithAverageBalance) {
        _excelFile.RemoveColumn("K");
        _excelFile.RemoveColumn("J");
      }
      if (!_command.ShowCascadeBalances) {
        _excelFile.RemoveColumn("B");
      }
    }


    private void FillOutBalanzaComparativa(IEnumerable<TrialBalanceComparativeDto> entries) {
      int i = 5;

      foreach (var entry in entries) {
        if (_command.ShowCascadeBalances) {
          _excelFile.SetCell($"A{i}", entry.LedgerNumber);
          _excelFile.SetCell($"B{i}", entry.LedgerName);
        } else {
          _excelFile.SetCell($"A{i}", "Consolidada");
        }
        
        _excelFile.SetCell($"C{i}", entry.CurrencyCode);
        _excelFile.SetCell($"D{i}", GetLedgerLevelAccountNumber(entry.AccountNumber));
        _excelFile.SetCell($"E{i}", GetSubAccountNumberWithSector(entry.AccountNumber, entry.SectorCode));
        _excelFile.SetCell($"F{i}", entry.AccountNumber);
        _excelFile.SetCell($"G{i}", entry.SectorCode);
        _excelFile.SetCell($"H{i}", entry.SubledgerAccountNumber);
        _excelFile.SetCell($"I{i}", entry.SubledgerAccountName);
        _excelFile.SetCell($"J{i}", entry.FirstTotalBalance);
        _excelFile.SetCell($"K{i}", entry.FirstExchangeRate);
        _excelFile.SetCell($"L{i}", entry.FirstValorization);
        _excelFile.SetCell($"M{i}", entry.Debit);
        _excelFile.SetCell($"N{i}", entry.Credit);
        _excelFile.SetCell($"O{i}", entry.SecondTotalBalance);
        _excelFile.SetCell($"P{i}", entry.SecondExchangeRate);
        _excelFile.SetCell($"Q{i}", entry.SecondValorization);
        _excelFile.SetCell($"R{i}", entry.AccountName);
        _excelFile.SetCell($"S{i}", Convert.ToString((char) entry.DebtorCreditor));
        _excelFile.SetCell($"T{i}", entry.Variation);
        _excelFile.SetCell($"U{i}", entry.VariationByER);
        _excelFile.SetCell($"V{i}", entry.RealVariation);

        if (MustFillOutAverageBalance(entry.AverageBalance, entry.LastChangeDate)) {
          _excelFile.SetCell($"W{i}", entry.AverageBalance);
          _excelFile.SetCell($"X{i}", entry.LastChangeDate.ToString("dd/MMM/yyyy"));
        }

        i++;
      }

      if (!_command.WithAverageBalance) {
        _excelFile.RemoveColumn("X");
        _excelFile.RemoveColumn("W");
      }
      if (!_command.ShowCascadeBalances) {
        _excelFile.RemoveColumn("B");
      }
    }


    private void SetBalanzaComparativaHeaders() {
      _excelFile.SetCell($"I4", $"{_command.InitialPeriod.ToDate.ToString("MMM_yyyy")}");
      _excelFile.SetCell($"K4", $"{_command.InitialPeriod.ToDate.ToString("MMM")}_VAL_A");
      _excelFile.SetCell($"N4", $"{_command.FinalPeriod.ToDate.ToString("MMM_yyyy")}");
      _excelFile.SetCell($"P4", $"{_command.FinalPeriod.ToDate.ToString("MMM")}_VAL_B");
    }


    private void FillOutBalanzaColumnasPorMoneda(IEnumerable<TrialBalanceByCurrencyDto> entries) {
      int i = 5;

      foreach (var entry in entries) {
        _excelFile.SetCell($"A{i}", entry.AccountNumber);
        _excelFile.SetCell($"B{i}", entry.AccountName);
        _excelFile.SetCell($"C{i}", entry.DomesticBalance);
        _excelFile.SetCell($"D{i}", entry.DollarBalance);
        _excelFile.SetCell($"E{i}", entry.YenBalance);
        _excelFile.SetCell($"F{i}", entry.EuroBalance);
        _excelFile.SetCell($"G{i}", entry.UdisBalance);
        if (entry.ItemType == TrialBalanceItemType.Summary) {
          _excelFile.SetRowStyleBold(i);
        }
        i++;
      }
    }


    private void FillOutBalanzaDolarizada(IEnumerable<ValuedTrialBalanceDto> entries) {
      int i = 5;

      foreach (var entry in entries) {
        _excelFile.SetCell($"A{i}", entry.AccountNumber);
        _excelFile.SetCell($"B{i}", entry.AccountName);
        _excelFile.SetCell($"C{i}", entry.CurrencyName);
        _excelFile.SetCell($"D{i}", entry.CurrencyCode);
        if (entry.ItemType != TrialBalanceItemType.BalanceTotalCurrency) {
          _excelFile.SetCell($"E{i}", (decimal) entry.TotalBalance);
          _excelFile.SetCell($"F{i}", (decimal) entry.ValuedExchangeRate);
        }
        _excelFile.SetCell($"G{i}", entry.TotalEquivalence);

        if (entry.ItemType != TrialBalanceItemType.Entry) {
          _excelFile.SetRowStyleBold(i);
        }

        i++;
      }
    }


    private void FillOutSaldosPorCuentayMayor(IEnumerable<TrialBalanceEntryDto> entries) {
      int i = 5;

      foreach (var entry in entries) {
        var account = StandardAccount.Parse(entry.StandardAccountId);

        _excelFile.SetCell($"A{i}", entry.CurrencyCode);
        if (account.IsEmptyInstance) {
          _excelFile.SetCell($"B{i}", entry.AccountNumber);
        } else {
          _excelFile.SetCell($"B{i}", account.Number);
        }
        if (entry.LedgerNumber.Length == 0) {
          _excelFile.SetCell($"C{i}", entry.AccountName);
          _excelFile.SetCell($"D{i}", entry.SectorCode);
          _excelFile.SetCell($"E{i}", "00");
          _excelFile.SetCell($"F{i}", "Todas");
        } else {
          _excelFile.SetCell($"C{i}", account.Name);
          _excelFile.SetCell($"D{i}", entry.SectorCode);
          _excelFile.SetCell($"E{i}", entry.LedgerNumber);
          _excelFile.SetCell($"F{i}", entry.AccountName);
        }

        _excelFile.SetCell($"G{i}", entry.InitialBalance);
        _excelFile.SetCell($"H{i}", entry.Debit);
        _excelFile.SetCell($"I{i}", entry.Credit);
        _excelFile.SetCell($"J{i}", (decimal) entry.CurrentBalance);

        if (MustFillOutAverageBalance((decimal) entry.AverageBalance, entry.LastChangeDate)) {
          _excelFile.SetCell($"K{i}", (decimal) entry.AverageBalance);
          _excelFile.SetCell($"L{i}", entry.LastChangeDate.ToString("dd/MMM/yyyy"));
        }

        if (entry.LedgerNumber.Length == 0) {
          _excelFile.SetRowStyleBold(i);
        }
        i++;
      }

      if (!_command.WithAverageBalance) {
        _excelFile.RemoveColumn("L");
        _excelFile.RemoveColumn("K");
      }
    }


    private void FillOutBalanza(IEnumerable<TrialBalanceEntryDto> entries) {
      int i = 5;

      foreach (var entry in entries) {
        if (_command.ShowCascadeBalances) {
          _excelFile.SetCell($"A{i}", entry.LedgerNumber);
          if (entry.ItemType == TrialBalanceItemType.Entry ||
              entry.ItemType == TrialBalanceItemType.Summary) {
            _excelFile.SetCell($"B{i}", entry.LedgerName);
          }
        } else {
          _excelFile.SetCell($"A{i}", "Consolidada");
        }
        
        _excelFile.SetCell($"C{i}", entry.CurrencyCode);
        if (entry.ItemType == TrialBalanceItemType.Entry) {
          if (!entry.IsParentPostingEntry) {
            _excelFile.SetCell($"D{i}", "*");
          } else {
            _excelFile.SetCell($"D{i}", "**");
          }
        }
        _excelFile.SetCell($"E{i}", entry.AccountNumber);
        _excelFile.SetCell($"F{i}", entry.AccountName);
        _excelFile.SetCell($"G{i}", entry.SectorCode);
        _excelFile.SetCell($"H{i}", entry.InitialBalance);
        _excelFile.SetCell($"I{i}", entry.Debit);
        _excelFile.SetCell($"J{i}", entry.Credit);
        _excelFile.SetCell($"K{i}", (decimal) entry.CurrentBalance);
        _excelFile.SetCell($"L{i}", Math.Round(entry.ExchangeRate, 6));

        if (MustFillOutAverageBalance((decimal) entry.AverageBalance, entry.LastChangeDate)) {
          _excelFile.SetCell($"M{i}", (decimal) entry.AverageBalance);
          _excelFile.SetCell($"N{i}", entry.LastChangeDate.ToString("dd/MMM/yyyy"));
        }

        if (entry.ItemType != TrialBalanceItemType.Entry &&
            entry.ItemType != TrialBalanceItemType.Summary) {
          _excelFile.SetRowStyleBold(i);
        }
        i++;
      }

      if (!_command.WithAverageBalance) {
        _excelFile.RemoveColumn("N");
        _excelFile.RemoveColumn("M");
      }
      if (!_command.UseDefaultValuation &&
            (_command.InitialPeriod.ValuateToCurrrencyUID.Length == 0 &&
             _command.InitialPeriod.ExchangeRateTypeUID.Length == 0)) {
        _excelFile.RemoveColumn("L");
      }
      if (!_command.ShowCascadeBalances) {
        _excelFile.RemoveColumn("B");
      }
    }


    private void FillOutSaldosAuxiliar(IEnumerable<TrialBalanceEntryDto> entries) {
      int i = 5;

      foreach (var entry in entries) {
        var account = StandardAccount.Parse(entry.StandardAccountId);

        if ((entry.ItemType == TrialBalanceItemType.Entry ||
            entry.ItemType == TrialBalanceItemType.Summary) &&
            (_command.ShowCascadeBalances)) {
          _excelFile.SetCell($"A{i}", entry.LedgerNumber);
          _excelFile.SetCell($"B{i}", entry.LedgerName);
        } else {
          _excelFile.SetCell($"A{i}", "");
        }

        if (entry.ItemType == TrialBalanceItemType.Entry) {
          _excelFile.SetCell($"C{i}", $"({entry.CurrencyCode}) {entry.CurrencyName}");
          if (!entry.IsParentPostingEntry) {
            _excelFile.SetCell($"D{i}", "*");
          } else {
            _excelFile.SetCell($"D{i}", "**");
          }
        }
        if (!account.IsEmptyInstance) {
          _excelFile.SetCell($"E{i}", account.Number);
          _excelFile.SetCell($"F{i}", account.Name);
        } else {
          _excelFile.SetCell($"E{i}", entry.AccountNumber);
          _excelFile.SetCell($"F{i}", entry.AccountName);
        }
        _excelFile.SetCell($"G{i}", entry.SectorCode);
        if (entry.ItemType == TrialBalanceItemType.Entry ||
            entry.ItemType == TrialBalanceItemType.Total) {
          _excelFile.SetCell($"H{i}", (decimal) entry.CurrentBalance);
        }
        
        _excelFile.SetCell($"I{i}", entry.DebtorCreditor);

        if (entry.ItemType == TrialBalanceItemType.Entry) {
          if (MustFillOutAverageBalance((decimal) entry.AverageBalance, entry.LastChangeDate)) {
            _excelFile.SetCell($"J{i}", (decimal) entry.AverageBalance);
          }
        }
        

        if (entry.LastChangeDate != ExecutionServer.DateMaxValue &&
            entry.LastChangeDate >= MIN_LAST_CHANGE_DATE_TO_REPORT) {
          _excelFile.SetCell($"K{i}", entry.LastChangeDate.ToString("dd/MMM/yyyy"));
        }

        if (entry.ItemType == TrialBalanceItemType.Summary ||
            entry.ItemType == TrialBalanceItemType.Total) {
          _excelFile.SetRowStyleBold(i);
        }
        if (entry.ItemType == TrialBalanceItemType.Total) {
          i++;
        }
        i++;
      }
      if (!_command.WithAverageBalance) {
        _excelFile.RemoveColumn("J");
      }
      if (!_command.ShowCascadeBalances) {
        _excelFile.RemoveColumn("B");
      }
    }


    private void FillOutSaldosCuenta(IEnumerable<TrialBalanceEntryDto> entries,
                                     bool includeSubledgerAccounts) {
      int i = 5;

      foreach (var entry in entries) {
        var account = StandardAccount.Parse(entry.StandardAccountId);
        var subledgerAccount = SubledgerAccount.Parse(entry.SubledgerAccountId);

        if (_command.ShowCascadeBalances) {
          _excelFile.SetCell($"A{i}", entry.LedgerNumber);
          if (entry.ItemType == TrialBalanceItemType.Entry ||
            entry.ItemType == TrialBalanceItemType.Summary) {
            _excelFile.SetCell($"B{i}", entry.LedgerName);
          }
        } else {
          _excelFile.SetCell($"A{i}", "Consolidada");
        }
        
        _excelFile.SetCell($"C{i}", entry.CurrencyCode);
        if (entry.ItemType == TrialBalanceItemType.Entry) {
          if (!entry.IsParentPostingEntry) {
            _excelFile.SetCell($"D{i}", "*");
          } else {
            _excelFile.SetCell($"D{i}", "**");
          }
        }
        if (!account.IsEmptyInstance) {
          _excelFile.SetCell($"E{i}", account.Number);
          _excelFile.SetCell($"F{i}", account.Name);
        } else {
          _excelFile.SetCell($"E{i}", entry.AccountNumber);
          _excelFile.SetCell($"F{i}", entry.AccountName);
        }
        _excelFile.SetCell($"G{i}", entry.SectorCode);
        
        if (includeSubledgerAccounts && !subledgerAccount.IsEmptyInstance) {
          _excelFile.SetCell($"H{i}", subledgerAccount.Number);
          _excelFile.SetCell($"I{i}", subledgerAccount.Name);
        }
        _excelFile.SetCell($"J{i}", (decimal) entry.CurrentBalance);
        _excelFile.SetCell($"K{i}", entry.DebtorCreditor);
        
        if (MustFillOutAverageBalance((decimal) entry.AverageBalance, entry.LastChangeDate)) {
          _excelFile.SetCell($"L{i}", (decimal) entry.AverageBalance);
        }

        if (entry.LastChangeDate != ExecutionServer.DateMaxValue &&
            entry.LastChangeDate >= MIN_LAST_CHANGE_DATE_TO_REPORT) {
          _excelFile.SetCell($"M{i}", entry.LastChangeDate.ToString("dd/MMM/yyyy"));
        }

        if (entry.ItemType != TrialBalanceItemType.Entry &&
            entry.ItemType != TrialBalanceItemType.Summary) {
          _excelFile.SetRowStyleBold(i);
        }
        i++;
      }
      if (!_command.WithAverageBalance) {
        _excelFile.RemoveColumn("L");
      }
      if (!includeSubledgerAccounts) {
        _excelFile.RemoveColumn("I");
        _excelFile.RemoveColumn("H");
      }
      if (!_command.ShowCascadeBalances) {
        _excelFile.RemoveColumn("B");
      }
    }

    #endregion Private methods

    #region Utility methods

    // TODO: CLEAN THIS CODE. ISSUE USING NEW CHART OF ACCOUNTS
    private string GetLedgerLevelAccountNumber(string accountNumber) {
      var temp = string.Empty;

      if (accountNumber.Contains("-")) {
        temp = accountNumber.Substring(0, 4);
      } else if (accountNumber.Contains(".")) {
        temp = accountNumber.Substring(0, 1);
      }

      return temp;
    }


    // TODO: CLEAN THIS CODE. ISSUE USING NEW CHART OF ACCOUNTS
    private string GetSubAccountNumberWithSector(string accountNumber, string sectorCode) {
      var temp = string.Empty;

      if (accountNumber.Contains("-")) {

        temp = accountNumber.Substring(4);

        temp = temp.Replace("-", String.Empty);

        temp = temp.PadRight(12, '0');

      } else if (accountNumber.Contains(".")) {

        temp = accountNumber.Substring(2);

        temp = temp.Replace(".", String.Empty);

        temp = temp.PadRight(20, '0');
      }

      return temp + sectorCode;
    }


    private bool MustFillOutAverageBalance(decimal averageBalance, DateTime lastChangeDate) {
      if (!_command.WithAverageBalance) {
        return false;
      }
      if (averageBalance != 0) {
        return true;
      }
      if (lastChangeDate < MIN_LAST_CHANGE_DATE_TO_REPORT) {
        return false;
      }
      if (lastChangeDate == ExecutionServer.DateMaxValue) {
        return false;
      }
      return true;
    }

    #endregion Utility methods

  }  // class TrialBalanceExcelExporter

}  // namespace Empiria.FinancialAccounting.Reporting.Exporters.Excel
