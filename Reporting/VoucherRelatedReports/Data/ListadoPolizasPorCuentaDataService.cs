﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Balance Engine                             Component : Data Layer                              *
*  Assembly : FinancialAccounting.Reporting.dll          Pattern   : Data Service                            *
*  Type     : ListadoPolizasPorCuentaDataService         License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Provides data read methods for voucher list by account.                                       *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

using Empiria.Data;
using Empiria.FinancialAccounting.Reporting.AccountStatements.Domain;
using Empiria.FinancialAccounting.Reporting.Data;

namespace Empiria.FinancialAccounting.Reporting.AccountStatements {

  /// <summary>Provides data read methods for voucher list by account.</summary>
  static class ListadoPolizasPorCuentaDataService {


    static internal FixedList<AccountStatementEntry> GetVouchersByAccountEntries(
                                                        ListadoPolizasSqlClauses commandData) {

      var operation = DataOperation.Parse("@qryVouchersByAccount",
                                          commandData.Fields,
                                          commandData.Filters,
                                          commandData.Grouping
                                          );

      return DataReader.GetPlainObjectFixedList<AccountStatementEntry>(operation);
    }


    static internal FixedList<AccountStatementEntry> GetVouchersByAccountEntries(string filter, string sortBy) {
        var sql = "SELECT * FROM VW_COF_MOVIMIENTO ";

        if (!string.IsNullOrWhiteSpace(filter)) {
            sql += $"WHERE {filter} ";
        }

        if (!string.IsNullOrWhiteSpace(sortBy)) {
            sql += $"ORDER BY {sortBy} ";
        }

        var op = DataOperation.Parse(sql);

        return DataReader.GetPlainObjectFixedList<AccountStatementEntry>(op);
    }


    } // class ListadoPolizasPorCuentaDataService

} // namespace Empiria.FinancialAccounting.Reporting.Data
