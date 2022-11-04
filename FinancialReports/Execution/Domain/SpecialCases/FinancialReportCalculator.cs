﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Financial Reports                          Component : Domain Layer                            *
*  Assembly : FinancialAccounting.FinancialReports.dll   Pattern   : Service provider                        *
*  Type     : FinancialReportCalculator                  License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Performs data calculation over financial reports data.                                         *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;

using Empiria.FinancialAccounting.FinancialReports.Expressions;

namespace Empiria.FinancialAccounting.FinancialReports {

  /// <summary>Performs data calculation over financial reports data.</summary>
  internal class FinancialReportCalculator {

    private FinancialReportType _financialReportType;

    public FinancialReportCalculator(FinancialReportType financialReportType) {
      Assertion.Require(financialReportType, nameof(financialReportType));

      _financialReportType = financialReportType;
    }


    internal void CalculateColumns(FixedList<DataTableColumn> columns,
                                   IEnumerable<FinancialReportEntry> entries) {
      foreach (var entry in entries) {
        CalculateColumns(columns, entry);
      }
    }


    internal void CalculateColumns(FixedList<DataTableColumn> columns,
                                   FinancialReportEntry entry) {
      foreach (var column in columns) {
        CalculateEntryColumn(column, entry);
      }
    }

    #region Helpers


    private void CalculateEntryColumn(DataTableColumn column, FinancialReportEntry entry) {
      var expressionParser = new ExpressionParser(column.Formula);

      Expression expression = expressionParser.Compile();

      decimal result = expression.Evaluate<decimal>(entry);

      entry.SetTotalField(column.Field, result);
    }


    #endregion Helpers

  }   // class FinancialReportCalculator

}  // namespace Empiria.FinancialAccounting.FinancialReports.Expressions
