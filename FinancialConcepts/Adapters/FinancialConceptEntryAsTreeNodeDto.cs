﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Financial Concepts                         Component : Interface adapters                      *
*  Assembly : FinancialAccounting.FinancialConcepts.dll  Pattern   : Data Transfer Object                    *
*  Type     : FinancialConceptEntryAsTreeNodeDto         License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Data transfer object for a financial concept integration entry as a tree node.                 *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

namespace Empiria.FinancialAccounting.FinancialConcepts.Adapters {

  /// <summary>Data transfer object for a financial concept integration entry as a tree node.</summary>
  public class FinancialConceptEntryAsTreeNodeDto {

    internal FinancialConceptEntryAsTreeNodeDto() {
      // no-op
    }

    public string UID {
      get; internal set;
    }


    public FinancialConceptEntryType Type {
      get; internal set;
    }


    public string ItemName {
      get; internal set;
    }


    public string ItemCode {
      get; internal set;
    }


    public string SubledgerAccount {
      get; internal set;
    }


    public string SubledgerAccountName {
      get; internal set;
    }


    public string SectorCode {
      get; internal set;
    }


    public string CurrencyCode {
      get; internal set;
    }


    public string Operator {
      get; internal set;
    }


    public string DataColumn {
      get; internal set;
    }


    public string ParentCode {
      get; internal set;
    }


    public int Level {
      get; internal set;
    }

  }  // class FinancialConceptEntryAsTreeNodeDto

}  // namespace Empiria.FinancialAccounting.FinancialConcepts.Adapters
