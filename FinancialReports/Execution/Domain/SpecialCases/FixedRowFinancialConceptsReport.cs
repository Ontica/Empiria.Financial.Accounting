﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Financial Reports                          Component : Domain Layer                            *
*  Assembly : FinancialAccounting.FinancialReports.dll   Pattern   : Service provider                        *
*  Type     : FixedRowFinancialConceptsReport            License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Generates a report with fixed rows linked to financial concepts (e.g. R01, R10, R12).          *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;
using System.Linq;

using Empiria.Collections;

using Empiria.FinancialAccounting.BalanceEngine.Adapters;

using Empiria.FinancialAccounting.ExternalData;
using Empiria.FinancialAccounting.FinancialReports.Adapters;
using Empiria.FinancialAccounting.FinancialConcepts;

namespace Empiria.FinancialAccounting.FinancialReports {

  /// <summary>Generates a report with fixed rows linked to financial concepts (e.g. R01, R10, R12).</summary>
  internal class FixedRowFinancialConceptsReport {

    private readonly FinancialReportQuery _buildQuery;
    private readonly EmpiriaHashTable<FixedList<ITrialBalanceEntryDto>> _balances;

    #region Public methods

    internal FixedRowFinancialConceptsReport(FinancialReportQuery buildQuery) {
      _buildQuery = buildQuery;
      _balances = GetBalancesHashTable();

      this.FinancialReportType = _buildQuery.GetFinancialReportType();
    }

    public FinancialReportType FinancialReportType {
      get;
    }


    internal FinancialReport Generate() {
      FixedList<FinancialReportRow> fixedRows = GetReportFixedRows();

      FixedList<FixedRowFinancialReportEntry> reportEntries = CreateReportEntriesWithoutTotals(fixedRows);

      FillEntries(reportEntries);

      CalculateColumns(reportEntries);

      return MapToFinancialReport(reportEntries);
    }


    internal FinancialReport GenerateBreakdown(string reportRowUID) {
      FinancialReportRow row = GetReportBreakdownRow(reportRowUID);

      FixedRowFinancialReportEntry reportEntry = CreateReportEntryWithoutTotals(row);

      FixedList<FinancialReportBreakdownEntry> breakdownEntries = GetBreakdownEntries(reportEntry);

      ReportEntryTotals breakdownTotal = FillBreakdown(breakdownEntries);

      breakdownTotal.CopyTotalsTo(reportEntry);

      var reportEntries = new List<FinancialReportEntry>();

      reportEntries.AddRange(breakdownEntries);
      reportEntries.Add(reportEntry);

      CalculateColumns(reportEntries);

      return MapToFinancialReport(reportEntries.ToFixedList());
    }



    internal FinancialReport GenerateIntegration() {
      FixedList<FinancialReportRow> integrationRows = GetReportRowsWithIntegrationAccounts();

      var reportEntries = new List<FinancialReportEntry>();

      foreach (var row in integrationRows) {
        FixedRowFinancialReportEntry reportEntry = CreateReportEntryWithoutTotals(row);

        FixedList<FinancialReportBreakdownEntry> breakdownEntries = GetBreakdownEntries(reportEntry).
                                                                    FindAll(x => x.IntegrationEntry.Type == FinancialConceptEntryType.Account);

        ReportEntryTotals breakdownTotal = FillBreakdown(breakdownEntries);

        reportEntries.AddRange(breakdownEntries);

        breakdownTotal.CopyTotalsTo(reportEntry);

        reportEntries.Add(reportEntry);
      }

      return MapToFinancialReport(reportEntries.ToFixedList());
    }

    #endregion Public methods

    #region Private methods

    private void CalculateColumns(IEnumerable<FixedRowFinancialReportEntry> reportEntries) {
      var calculator = new FinancialReportCalculator(this.FinancialReportType);

      IEnumerable<FinancialReportEntry> castedEntries = reportEntries.Select(entry => (FinancialReportEntry) entry);

      var columnsToCalculate = this.FinancialReportType.DataColumns.FindAll(x => x.IsCalculated);

      calculator.CalculateColumns(columnsToCalculate, castedEntries);
    }


    private void CalculateColumns(IEnumerable<FinancialReportEntry> reportEntries) {
      var calculator = new FinancialReportCalculator(this.FinancialReportType);

      var columnsToCalculate = this.FinancialReportType.BreakdownColumns.FindAll(x => x.IsCalculated);

      calculator.CalculateColumns(columnsToCalculate, reportEntries);
    }


    private ReportEntryTotals ProcessAccount(FinancialConceptEntry integrationEntry) {
      if (!_balances.ContainsKey(integrationEntry.AccountNumber)) {
        return CreateReportEntryTotalsObject();
      }

      FixedList<ITrialBalanceEntryDto> accountBalances = GetAccountBalances(integrationEntry);

      var totals = CreateReportEntryTotalsObject();

      foreach (var balance in accountBalances) {

        if (integrationEntry.CalculationRule == "SaldoDeudorasMenosSaldoAcreedoras") {
          totals = totals.SumDebitsOrSubstractCredits(balance, integrationEntry.DataColumn);
        } else {
          totals = totals.Sum(balance, integrationEntry.DataColumn);
        }
      }

      if (FinancialReportType.RoundDecimals) {
        totals = totals.Round();
      }

      if (integrationEntry.Operator == OperatorType.AbsoluteValue) {
        totals = totals.AbsoluteValue();
      }

      return totals;
    }


    private ReportEntryTotals FillBreakdown(FixedList<FinancialReportBreakdownEntry> breakdown) {

      ReportEntryTotals granTotal = CreateReportEntryTotalsObject();

      foreach (var breakdownItem in breakdown) {

        ReportEntryTotals breakdownTotals = CalculateBreakdown(breakdownItem.IntegrationEntry);

        if (FinancialReportType.RoundDecimals) {
          breakdownTotals = breakdownTotals.Round();
        }

        breakdownTotals.CopyTotalsTo(breakdownItem);

        switch (breakdownItem.IntegrationEntry.Operator) {

          case OperatorType.Add:
            granTotal = granTotal.Sum(breakdownTotals, breakdownItem.IntegrationEntry.DataColumn);
            break;

          case OperatorType.Substract:
            granTotal = granTotal.Substract(breakdownTotals, breakdownItem.IntegrationEntry.DataColumn);
            break;

          case OperatorType.AbsoluteValue:
            granTotal = granTotal.Sum(breakdownTotals, breakdownItem.IntegrationEntry.DataColumn)
                                 .AbsoluteValue();
            break;

        } // switch

      } // foreach

      return granTotal;
    }


    private ReportEntryTotals CalculateBreakdown(FinancialConceptEntry integrationEntry) {

      switch (integrationEntry.Type) {

        case FinancialConceptEntryType.FinancialConceptReference:
          return ProcessFinancialConcept(integrationEntry.ReferencedFinancialConcept);

        case FinancialConceptEntryType.Account:
          return ProcessAccount(integrationEntry);

        case FinancialConceptEntryType.ExternalVariable:
          return ProcessExternalVariable(integrationEntry);

        default:
          throw Assertion.EnsureNoReachThisCode();
      }
    }


    private void FillEntries(FixedList<FixedRowFinancialReportEntry> reportEntries) {

      foreach (var reportEntry in reportEntries) {

        if (reportEntry.FinancialConcept.IsEmptyInstance) {
          continue;
        }

        ReportEntryTotals totals = ProcessFinancialConcept(reportEntry.FinancialConcept);

        if (FinancialReportType.RoundDecimals) {
          totals = totals.Round();
        }

        totals.CopyTotalsTo(reportEntry);
      }
    }


    private ReportEntryTotals ProcessExternalVariable(FinancialConceptEntry integrationEntry) {
      var variable = ExternalVariable.TryParseWithCode(integrationEntry.ExternalVariableCode);

      ExternalValue value = ExternalValue.Empty;

      if (variable != null) {
        value = variable.GetValue(_buildQuery.ToDate);
      }

      var totals = CreateReportEntryTotalsObject();

      return totals.Sum(value, integrationEntry.DataColumn);
    }


    private ReportEntryTotals ProcessFinancialConcept(FinancialConcept financialConcept) {
      Assertion.Require(!financialConcept.IsEmptyInstance,
                        "Cannot process the empty FinancialConcept instance.");

      ReportEntryTotals totals = CreateReportEntryTotalsObject();

      foreach (var integrationItem in financialConcept.Integration) {

        switch (integrationItem.Type) {
          case FinancialConceptEntryType.FinancialConceptReference:

            totals = CalculateFinancialConceptTotals(integrationItem, totals);
            break;

          case FinancialConceptEntryType.Account:
            totals = CalculateAccountTotals(integrationItem, totals);
            break;

          case FinancialConceptEntryType.ExternalVariable:
            totals = CalculateExternalVariableTotals(integrationItem, totals);
            break;

        }

      }  // foreach

      return totals;
    }


    private ReportEntryTotals CalculateAccountTotals(FinancialConceptEntry integrationEntry,
                                                     ReportEntryTotals totals) {

      Assertion.Require(integrationEntry.Type == FinancialConceptEntryType.Account,
                        "Invalid integrationEntry.Type");

      switch (integrationEntry.Operator) {

        case OperatorType.Add:

          return totals.Sum(ProcessAccount(integrationEntry),
                            integrationEntry.DataColumn);

        case OperatorType.Substract:

          return totals.Substract(ProcessAccount(integrationEntry),
                                  integrationEntry.DataColumn);

        case OperatorType.AbsoluteValue:

          return totals.Sum(ProcessAccount(integrationEntry),
                            integrationEntry.DataColumn)
                       .AbsoluteValue();

        default:
          throw Assertion.EnsureNoReachThisCode($"Unhandled operator '{integrationEntry.Operator}'.");
      }

    }


    private ReportEntryTotals CalculateFinancialConceptTotals(FinancialConceptEntry integrationEntry,
                                                              ReportEntryTotals totals) {

      Assertion.Require(integrationEntry.Type == FinancialConceptEntryType.FinancialConceptReference,
                      "Invalid integrationEntry.Type");

      switch (integrationEntry.Operator) {

        case OperatorType.Add:

          return totals.Sum(ProcessFinancialConcept(integrationEntry.ReferencedFinancialConcept),
                            integrationEntry.DataColumn);

        case OperatorType.Substract:

          return totals.Substract(ProcessFinancialConcept(integrationEntry.ReferencedFinancialConcept),
                                  integrationEntry.DataColumn);

        case OperatorType.AbsoluteValue:

          return totals.Sum(ProcessFinancialConcept(integrationEntry.ReferencedFinancialConcept),
                            integrationEntry.DataColumn)
                       .AbsoluteValue();

        default:
          throw Assertion.EnsureNoReachThisCode($"Unhandled operator '{integrationEntry.Operator}'.");

      }

    }


    private ReportEntryTotals CalculateExternalVariableTotals(FinancialConceptEntry integrationEntry,
                                                              ReportEntryTotals totals) {

      Assertion.Require(integrationEntry.Type == FinancialConceptEntryType.ExternalVariable,
                       "Invalid integrationEntry.Type");

      switch (integrationEntry.Operator) {

        case OperatorType.Add:

          return totals.Sum(ProcessExternalVariable(integrationEntry),
                            integrationEntry.DataColumn);

        case OperatorType.Substract:

          return totals.Substract(ProcessExternalVariable(integrationEntry),
                                  integrationEntry.DataColumn);

        case OperatorType.AbsoluteValue:

          return totals.Sum(ProcessExternalVariable(integrationEntry),
                            integrationEntry.DataColumn)
                       .AbsoluteValue();

        default:
          throw Assertion.EnsureNoReachThisCode($"Unhandled operator '{integrationEntry.Operator}'.");
      }

    }

    #endregion Private methods

    #region Helpers

    private ReportEntryTotals CreateReportEntryTotalsObject() {
      switch (FinancialReportType.DataSource) {
        case FinancialReportDataSource.AnaliticoCuentas:
          return new AnaliticoCuentasReportEntryTotals();

        case FinancialReportDataSource.AnaliticoCuentasDynamic:
          return new DynamicReportEntryTotals(this.FinancialReportType.DataColumns);

        case FinancialReportDataSource.BalanzaEnColumnasPorMoneda:
          return new BalanzaEnColumnasPorMonedaReportEntryTotals();

        case FinancialReportDataSource.BalanzaTradicionalDynamic:
          return new DynamicReportEntryTotals(this.FinancialReportType.DataColumns);

        default:
          throw Assertion.EnsureNoReachThisCode($"Unhandled data source {FinancialReportType.DataSource}.");
      }
    }


    private FixedList<FixedRowFinancialReportEntry> CreateReportEntriesWithoutTotals(FixedList<FinancialReportRow> rows) {
      return rows.Select(x => CreateReportEntryWithoutTotals(x))
                 .ToFixedList();
    }


    private FixedRowFinancialReportEntry CreateReportEntryWithoutTotals(FinancialReportRow row) {
      return new FixedRowFinancialReportEntry {
        Row = row,
        FinancialConcept = row.FinancialConcept
      };
    }


    private FixedList<ITrialBalanceEntryDto> GetAccountBalances(FinancialConceptEntry integrationEntry) {
      FixedList<ITrialBalanceEntryDto> balances = _balances[integrationEntry.AccountNumber];

      FixedList<ITrialBalanceEntryDto> filtered;

      if (integrationEntry.HasSector && integrationEntry.HasSubledgerAccount) {
        filtered = balances.FindAll(x => x.SectorCode == integrationEntry.SectorCode &&
                                         x.SubledgerAccountNumber == integrationEntry.SubledgerAccountNumber);

      } else if (integrationEntry.HasSector && !integrationEntry.HasSubledgerAccount) {
        filtered = balances.FindAll(x => x.SectorCode == integrationEntry.SectorCode &&
                                         x.SubledgerAccountNumber.Length == 0);

      } else if (!integrationEntry.HasSector && integrationEntry.HasSubledgerAccount) {
        filtered = balances.FindAll(x => x.SectorCode == "00" &&
                                         x.SubledgerAccountNumber == integrationEntry.SubledgerAccountNumber);
        if (filtered.Count == 0) {
          filtered = balances.FindAll(x => x.SectorCode != "00" &&
                                           x.SubledgerAccountNumber == integrationEntry.SubledgerAccountNumber);
        }
      } else {
        filtered = balances.FindAll(x => x.SectorCode == "00" &&
                                         x.SubledgerAccountNumber.Length == 0);
        if (filtered.Count == 0) {
          filtered = balances.FindAll(x => x.SectorCode != "00" &&
                                           x.SubledgerAccountNumber.Length == 0);
        }
      }

      if (FinancialReportType.DataSource == FinancialReportDataSource.AnaliticoCuentasDynamic) {
        return ConvertToDynamicTrialBalanceEntryDto(filtered);
      } else {
        return filtered;
      }
    }


    private FixedList<FinancialReportBreakdownEntry> GetBreakdownEntries(FixedRowFinancialReportEntry reportEntry) {
      var breakdown = new List<FinancialReportBreakdownEntry>();

      var financialConcept = reportEntry.FinancialConcept;

      foreach (var integrationEntry in financialConcept.Integration) {
        breakdown.Add(new FinancialReportBreakdownEntry { IntegrationEntry = integrationEntry });
      }

      return breakdown.ToFixedList();
    }


    private FinancialReportRow GetReportBreakdownRow(string reportRowUID) {
      return FinancialReportType.GetRow(reportRowUID);
    }


    private FixedList<FinancialReportRow> GetReportRowsWithIntegrationAccounts() {
      FixedList<FinancialReportRow> rows = GetReportFixedRows();

      return rows.FindAll(x => !x.FinancialConcept.IsEmptyInstance &&
                                x.FinancialConcept.Integration.Contains(item => item.Type == FinancialConceptEntryType.Account));
    }


    private FixedList<FinancialReportRow> GetReportFixedRows() {
      return FinancialReportType.GetRows();
    }


    private EmpiriaHashTable<FixedList<ITrialBalanceEntryDto>> GetBalancesHashTable() {
      var balancesProvider = new AccountBalancesProvider(_buildQuery);

      return balancesProvider.GetBalancesAsHashTable();
    }


    private FinancialReport MapToFinancialReport<T>(FixedList<T> reportEntries) where T : FinancialReportEntry {
      var convertedEntries = new FixedList<FinancialReportEntry>(reportEntries.Select(x => (FinancialReportEntry) x));

      return new FinancialReport(_buildQuery, convertedEntries);
    }


    private FixedList<ITrialBalanceEntryDto> ConvertToDynamicTrialBalanceEntryDto(FixedList<ITrialBalanceEntryDto> sourceEntries) {
      var converter = new DynamicTrialBalanceEntryConverter();

      FixedList<DynamicTrialBalanceEntryDto> convertedEntries = converter.Convert(sourceEntries);

      return convertedEntries.Select(entry => (ITrialBalanceEntryDto) entry)
                             .ToFixedList();
    }

    #endregion Helpers

  }  // class FixedRowGroupingRulesReport

}  // namespace Empiria.FinancialAccounting.FinancialReports
