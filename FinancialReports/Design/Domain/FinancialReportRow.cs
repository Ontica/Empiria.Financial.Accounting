﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Financial Reports                          Component : Domain Layer                            *
*  Assembly : FinancialAccounting.FinancialReports.dll   Pattern   : Information Holder                      *
*  Type     : FinancialReportRow                         License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Describes a financial report fixed row.                                                        *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

using Empiria.FinancialAccounting.FinancialReports.Data;

namespace Empiria.FinancialAccounting.FinancialReports {

  /// <summary>Describes a financial report fixed row.</summary>
  public class FinancialReportRow: FinancialReportItemDefinition {

    #region Constructors and parsers

    protected FinancialReportRow() {
      // Required by Empiria Framework.
    }


    static new public FinancialReportRow Parse(int id) {
      return BaseObject.ParseId<FinancialReportRow>(id);
    }


    static new public FinancialReportRow Parse(string uid) {
      return BaseObject.ParseKey<FinancialReportRow>(uid);
    }

    #endregion Constructors and parsers

    #region Methods

    protected override void OnSave() {
      FinancialReportsData.Write(this);
    }

    #endregion Methods

  }  // class FinancialReportRow

}  // namespace Empiria.FinancialAccounting.FinancialReports
