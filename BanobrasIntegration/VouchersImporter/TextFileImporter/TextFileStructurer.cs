﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Banobras Integration Services                 Component : Vouchers Importer                    *
*  Assembly : FinancialAccounting.BanobrasIntegration.dll   Pattern   : Structurer                           *
*  Type     : TextFileStructurer                            License   : Please read LICENSE.txt file         *
*                                                                                                            *
*  Summary  : Provides information structure services for vouchers contained in text Files.                  *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;
using System.Linq;

using Empiria.FinancialAccounting.Vouchers;
using Empiria.FinancialAccounting.BanobrasIntegration.VouchersImporter.Adapters;

namespace Empiria.FinancialAccounting.BanobrasIntegration.VouchersImporter {

  /// <summary>Provides information structure services for vouchers contained in text Files.</summary>
  internal class TextFileStructurer {

    private readonly ImportVouchersCommand _command;
    private readonly string[] _textFileLines;

    private readonly FixedList<TextFileVoucherEntry> _entries;

    public TextFileStructurer(ImportVouchersCommand command, string[] textFileLines) {
      Assertion.Require(command, "command");
      Assertion.Require(textFileLines, "textFileLines");

      _command = command;
      _textFileLines = textFileLines;

      _entries = ParseTextLinesToTextFileVoucherEntries();
    }


    public FixedList<TextFileVoucherEntry> Entries {
      get {
        return _entries;
      }
    }


    #region Methods

    internal FixedList<ToImportVoucher> GetToImportVouchersList() {
      string[] voucherUniqueIDsArray = GetVoucherUniqueIds();

      var vouchersListToImport = new List<ToImportVoucher>(voucherUniqueIDsArray.Length);

      foreach (string voucherUniqueID in voucherUniqueIDsArray) {
        ToImportVoucher voucherToImport = BuildVoucherToImport(voucherUniqueID);

        vouchersListToImport.Add(voucherToImport);
      }

      return vouchersListToImport.ToFixedList();
    }


    private ToImportVoucher BuildVoucherToImport(string voucherUniqueID) {
      ToImportVoucherHeader header = MapToImportVoucherHeader(voucherUniqueID);
      FixedList<ToImportVoucherEntry> entries = MapToImportVoucherEntries(header);

      return new ToImportVoucher(header, entries);
    }


    private ToImportVoucherHeader MapToImportVoucherHeader(string voucherUniqueID) {
      var sourceHeader = _entries.Find(x => x.VoucherUniqueID == voucherUniqueID);

      var header = new ToImportVoucherHeader();

      header.ImportationSet = sourceHeader.GetImportationSet();
      header.UniqueID = sourceHeader.VoucherUniqueID;
      header.Ledger = sourceHeader.GetLedger();
      header.Concept = sourceHeader.GetConcept();
      header.AccountingDate = sourceHeader.GetAccountingDate();
      header.VoucherType = VoucherType.Parse(_command.VoucherTypeUID);
      header.TransactionType = TransactionType.Parse(_command.TransactionTypeUID);
      header.FunctionalArea = sourceHeader.GetFunctionalArea();
      header.RecordingDate = DateTime.Today;
      header.ElaboratedBy = Participant.Current;

      header.Issues = sourceHeader.GetHeaderIssues();

      return header;
    }


    private FixedList<ToImportVoucherEntry> MapToImportVoucherEntries(ToImportVoucherHeader header) {
      var entries = _entries.FindAll(x => x.VoucherUniqueID == header.UniqueID);

      var mapped = entries.Select(entry => MapToImportVoucherEntry(header, entry));

      return new List<ToImportVoucherEntry>(mapped).ToFixedList();
    }


    private ToImportVoucherEntry MapToImportVoucherEntry(ToImportVoucherHeader header,
                                                         TextFileVoucherEntry sourceEntry) {
      var entry = new ToImportVoucherEntry(header) {
        StandardAccount = sourceEntry.GetStandardAccount(),
        Sector = sourceEntry.GetSector(),
        SubledgerAccount = sourceEntry.GetSubledgerAccount(),
        SubledgerAccountNo = sourceEntry.GetSubledgerAccountNo(),
        ResponsibilityArea = sourceEntry.GetResponsibilityArea(),
        BudgetConcept = string.Empty,
        EventType = sourceEntry.GetEventType(),
        VerificationNumber = string.Empty,
        VoucherEntryType = sourceEntry.GetVoucherEntryType(),
        Date = header.AccountingDate,
        Currency = sourceEntry.GetCurrency(),
        Amount = sourceEntry.GetAmount(),
        ExchangeRate = sourceEntry.GetExchangeRate(),
        BaseCurrencyAmount = sourceEntry.GetBaseCurrencyAmount(),
        DataSource = $"Línea {sourceEntry.TextLineIndex}",
        Protected = false,
      };

      entry.AddIssues(sourceEntry.GetEntryIssues());

      return entry;
    }


    private FixedList<TextFileVoucherEntry> ParseTextLinesToTextFileVoucherEntries() {
      List<TextFileVoucherEntry> entries = new List<TextFileVoucherEntry>(_textFileLines.Length);

      AccountsChart accountsChart = _command.GetAccountsChart();

      EnsureTextLinesHaveTheRightLength(accountsChart);

      for (int lineIndex = 0; lineIndex < _textFileLines.Length; lineIndex++) {
        entries.Add(new TextFileVoucherEntry(accountsChart, _textFileLines[lineIndex], lineIndex + 1));
      }

      return entries.ToFixedList();
    }

    private void EnsureTextLinesHaveTheRightLength(AccountsChart accountsChart) {
      int textFileLineLengthForAccountsChart = accountsChart.Equals(AccountsChart.IFRS) ?
                                                  TextFileVoucherEntry.WIDE_TEXT_LINE_LENGTH : TextFileVoucherEntry.STANDARD_TEXT_LINE_LENGTH;

      Assertion.Require(_textFileLines.All(x => x.Length == textFileLineLengthForAccountsChart),
            $"El archivo de texto tiene líneas de longitud distinta a {textFileLineLengthForAccountsChart}, " +
            $"tamaño definido para importar pólizas para el catálogo de cuentas {accountsChart.Name}.");
    }

    private string[] GetVoucherUniqueIds() {
      return this.Entries.Select(x => x.VoucherUniqueID)
                         .Distinct()
                         .ToArray();
    }


    #endregion Methods

  }  // class TextFileStructurer

}  // namespace Empiria.FinancialAccounting.BanobrasIntegration.VouchersImporter
