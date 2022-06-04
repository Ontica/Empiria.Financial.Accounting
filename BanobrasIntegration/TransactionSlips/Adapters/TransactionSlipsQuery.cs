﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Transaction Slips                             Component : Interface adapters                   *
*  Assembly : FinancialAccounting.BanobrasIntegration.dll   Pattern   : Query payload                        *
*  Type     : TransactionSlipsQuery                         License   : Please read LICENSE.txt file         *
*                                                                                                            *
*  Summary  : Query payload used to search transaction slips (volantes).                                     *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

using Empiria.FinancialAccounting.Vouchers.Adapters;

namespace Empiria.FinancialAccounting.BanobrasIntegration.TransactionSlips.Adapters {

  /// <summary>Query payload used to search transaction slips (volantes).</summary>
  public class TransactionSlipsQuery {

    public string AccountsChartUID {
      get;
      set;
    } = string.Empty;


    public string SystemUID {
      get;
      set;
    } = string.Empty;


    public string Keywords {
      get;
      set;
    } = string.Empty;


    public DateTime FromDate {
      get;
      set;
    } = ExecutionServer.DateMinValue;


    public DateTime ToDate {
      get;
      set;
    } = ExecutionServer.DateMaxValue;


    public DateSearchField DateSearchField {
      get;
      set;
    } = DateSearchField.None;


    public TransactionSlipStatus Status {
      get;
      set;
    } = TransactionSlipStatus.Pending;


  }  // class TransactionSlipsQuery

}  // namespace Empiria.FinancialAccounting.BanobrasIntegration.TransactionSlips.Adapters
