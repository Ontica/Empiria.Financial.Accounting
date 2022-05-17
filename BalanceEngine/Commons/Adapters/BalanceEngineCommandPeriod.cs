﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Balance Engine                             Component : Interface adapters                      *
*  Assembly : FinancialAccounting.BalanceEngine.dll      Pattern   : Command payload                         *
*  Type     : BalanceEngineCommandPeriod                 License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Command payload that describes balances' date periods.                                         *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

using Empiria.Json;

namespace Empiria.FinancialAccounting.BalanceEngine.Adapters {

  /// <summary>Command payload that describes balances' date periods.</summary>
  public class BalanceEngineCommandPeriod {

    public DateTime FromDate {
      get; set;
    }

    public DateTime ToDate {
      get; set;
    }

    public DateTime ExchangeRateDate {
      get;  set;
    } = DateTime.Today;


    public string ExchangeRateTypeUID {
      get; set;
    } = string.Empty;


    public string ValuateToCurrrencyUID {
      get; set;
    } = string.Empty;


    public bool UseDefaultValuation {
      get; set;
    } = false;


    public bool IsSecondPeriod {
      get; set;
    } = false;


    public override bool Equals(object obj) {
      return obj is BalanceEngineCommandPeriod period &&
             period.GetHashCode() == this.GetHashCode();
    }


    public override int GetHashCode() {
      var json = JsonObject.Parse(this);

      return json.GetHashCode();
    }

  }  // BalanceEngineCommandPeriod


} // namespace Empiria.FinancialAccounting.BalanceEngine.Adapters
