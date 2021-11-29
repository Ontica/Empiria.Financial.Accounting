﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Banobras Integration Services                 Component : Vouchers Importer                    *
*  Assembly : FinancialAccounting.BanobrasIntegration.dll   Pattern   : Structurer                           *
*  Type     : ToImportVoucherEntry                          License   : Please read LICENSE.txt file         *
*                                                                                                            *
*  Summary  : Holds a voucher's structure coming from database tables.                                       *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;

using Empiria.FinancialAccounting.Vouchers;

namespace Empiria.FinancialAccounting.BanobrasIntegration.VouchersImporter {

  public class ToImportVoucherEntry {

    private readonly List<ToImportVoucherIssue> _issues = new List<ToImportVoucherIssue>();

    internal ToImportVoucherEntry(ToImportVoucherHeader header) {
      this.ToImportVoucherHeader = header;
    }


    public ToImportVoucherHeader ToImportVoucherHeader {
      get;
    }


    public LedgerAccount LedgerAccount {
      get; internal set;
    } = LedgerAccount.Empty;


    public StandardAccount StandardAccount {
      get; internal set;
    } = StandardAccount.Empty;


    public Sector Sector {
      get; internal set;
    } = Sector.Empty;


    public SubledgerAccount SubledgerAccount {
      get; internal set;
    } = SubledgerAccount.Empty;


    public string SubledgerAccountNo {
      get; internal set;
    } = string.Empty;


    public FunctionalArea ResponsibilityArea {
      get; internal set;
    } = FunctionalArea.Empty;


    public string BudgetConcept {
      get; internal set;
    } = string.Empty;


    public EventType EventType {
      get; internal set;
    } = EventType.Empty;


    public string VerificationNumber {
      get; internal set;
    } = string.Empty;


    public VoucherEntryType VoucherEntryType {
      get; internal set;
    }


    public DateTime Date {
      get; internal set;
    } = ExecutionServer.DateMinValue;


    public string Concept {
      get; internal set;
    } = string.Empty;


    public Currency Currency {
      get; internal set;
    } = Currency.Empty;


    public decimal Amount {
      get; internal set;
    }


    public decimal ExchangeRate {
      get; internal set;
    }


    public decimal BaseCurrencyAmount {
      get; internal set;
    }


    public bool Protected {
      get; internal set;
    }


    public string DataSource {
      get; internal set;
    }


    public FixedList<ToImportVoucherIssue> Issues {
      get {
        return _issues.ToFixedList();
      }
    }


    internal void AddIssue(string description) {
      var issue = new ToImportVoucherIssue(VoucherIssueType.Error,
                                           this.ToImportVoucherHeader.ImportationSet,
                                           this.DataSource,
                                           description);

      _issues.Add(issue);
    }


    internal void AddIssue(ToImportVoucherIssue issue) {
      _issues.Add(issue);
    }


    internal void AddIssues(FixedList<ToImportVoucherIssue> issuesList) {
      _issues.AddRange(issuesList);
    }

  }  // class ToImportVoucherEntry

}  // namespace Empiria.FinancialAccounting.BanobrasIntegration.VouchersImporter
