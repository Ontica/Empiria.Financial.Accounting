﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Reporting Services                            Component : Interface adapters                   *
*  Assembly : FinancialAccounting.Reporting.dll             Pattern   : Data Transfer Object                 *
*  Type     : IntegracionSaldosCapitalDto                   License   : Please read LICENSE.txt file         *
*                                                                                                            *
*  Summary  : Output DTO used to return 'Integración de saldos de capital' report data.                      *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

namespace Empiria.FinancialAccounting.Reporting.IntegracionSaldosCapitalIntereses.Adapters {

  /// <summary>DTO for each report row.</summary>
  public class IntegracionSaldosCapitalEntryDto : IReportEntryDto {

    public string ItemType {
      get; internal set;
    } = "Entry";


    public string SubledgerAccount {
      get; internal set;
    } = string.Empty;


    public string SubledgerAccountName {
      get; internal set;
    } = string.Empty;


    public string PrestamoName {
      get; internal set;
    } = string.Empty;


    public string CurrencyCode {
      get; internal set;
    } = string.Empty;


    public string SectorCode {
      get; internal set;
    } = string.Empty;


    public decimal CapitalCortoPlazoMonedaOrigen {
      get; internal set;
    }

    public decimal CapitalLargoPlazoMonedaOrigen {
      get; internal set;
    }

    public decimal CapitalMonedaOrigenTotal {
      get; internal set;
    }

    public DateTime? Vencimiento {
      get; internal set;
    }

  } // class IntegracionSaldosCapitalEntryDto


  public class IntegracionSaldosCapitalTotalDto : IReportEntryDto {

    public string ItemType {
      get; internal set;
    } = "Total";


    public string SubledgerAccount {
      get;
      internal set;
    }

    public decimal CapitalMonedaNacional {
      get; internal set;
    }

    public decimal InteresesMonedaNacional {
      get; internal set;
    }

    public decimal TotalMonedaNacional {
      get; internal set;
    }

  }  // class IntegracionSaldosCapitalTotalDto

} // namespace Empiria.FinancialAccounting.Reporting.IntegracionSaldosCapitalIntereses.Adapters
