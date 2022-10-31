﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : External Data                              Component : Data Access Layer                       *
*  Assembly : FinancialAccounting.ExternalData.dll       Pattern   : Data Service                            *
*  Type     : ExternalVariablesData                      License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Data access layer for financial external variables definition data.                            *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

using Empiria.Data;

namespace Empiria.FinancialAccounting.ExternalData.Data {

  /// <summary>Data access layer for financial external variables definition data.</summary>
  static internal class ExternalVariablesData {

    static internal FixedList<ExternalVariable> GetExternalVariables(ExternalVariablesSet set) {
      var sql = "SELECT * FROM COF_VARIABLES_EXTERNAS " +
               $"WHERE ID_CONJUNTO_BASE = {set.Id} " +
               $"AND STATUS_VARIABLE_EXTERNA <> 'X' " +
               $"ORDER BY POSICION, CLAVE_VARIABLE";

      var op = DataOperation.Parse(sql);

      return DataReader.GetFixedList<ExternalVariable>(op);
    }

  }  // class ExternalVariablesData

}  // namespace Empiria.FinancialAccounting.ExternalData.Data
