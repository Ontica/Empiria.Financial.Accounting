﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Exchange Rates                             Component : Interface adapters                      *
*  Assembly : FinancialAccounting.Core.dll               Pattern   : Data Transfer Object                    *
*  Type     : ExchangeRateFields                         License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Input DTO used to store an exchange rate.                                                      *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

namespace Empiria.FinancialAccounting.Adapters {

  /// <summary>Input DTO used to store an exchange rate.</summary>
  public class ExchangeRateFields {

    public string ExchangeRateTypeUID {
      get; set;
    }


    public DateTime Date {
      get; set;
    } = ExecutionServer.DateMinValue;


    public ExchangeRateFieldValue[] Values {
      get; set;
    } = new ExchangeRateFieldValue[0];


    internal void EnsureValid() {
      Assertion.AssertObject(ExchangeRateTypeUID, "ExchangeRateTypeUID");

      Assertion.Assert(Date != ExecutionServer.DateMinValue,
          "Exchange rate date must be provided.");
      Assertion.AssertObject(Values, "Values array can not be null.");
      Assertion.Assert(Values.Length != 0, "Values array must have one or more values.");

      for (int i = 0; i < Values.Length; i++) {
        Assertion.AssertObject(Values[i].ToCurrencyUID,
                               $"Exchange rate currency is missed for values element {i}.");
        Assertion.Assert(Values[i].Value > 0,
                         $"Exchange rate value must be a positive decimal for values element {i}.");
      }
    }

  }  // public class ExchangeRateFields



  public class ExchangeRateFieldValue {

    public string ToCurrencyUID {
      get; set;
    }


    public decimal Value {
      get; set;
    }

  }  // class ExchangeRateFieldValue

}  // namespace Empiria.FinancialAccounting.Adapters
