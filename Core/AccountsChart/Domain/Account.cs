﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Accounts Chart                             Component : Domain Layer                            *
*  Assembly : FinancialAccounting.Core.dll               Pattern   : Empiria Data Object                     *
*  Type     : Account                                    License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Contains information about an account.                                                         *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

namespace Empiria.FinancialAccounting {

  /// <summary>Contains information about an account.</summary>
  public class Account : BaseObject {

    #region Fields

    private Lazy<FixedList<Currency>> _currencies = null;

    private Lazy<FixedList<Sector>> _sectors = null;

    #endregion Fields

    #region Constructors and parsers

    private Account() {
      // Required by Empiria Framework.
    }


    static public Account Parse(int id) {
      return BaseObject.ParseId<Account>(id);
    }


    static public Account Parse(string uid) {
      return BaseObject.ParseKey<Account>(uid);
    }


    static public Account Empty => BaseObject.ParseEmpty<Account>();


    protected override void OnLoad() {
      base.OnLoad();

      this.ResetCurrencies();
      this.ResetSectors();
    }


    #endregion Constructors and parsers

    #region Public properties

    [DataField("ID_TIPO_CUENTAS_STD", ConvertFrom=typeof(long))]
    internal int AccountsChartId {
      get; private set;
    } = -1;


    public AccountsChart AccountsChart {
      get {
        return AccountsChart.Parse(this.AccountsChartId);
      }
    }


    [DataField("NUMERO_CUENTA_ESTANDAR")]
    public string Number {
      get; private set;
    } = string.Empty;


    [DataField("NOMBRE_CUENTA_ESTANDAR")]
    public string Name {
      get; private set;
    } = string.Empty;


    [DataField("DESCRIPCION")]
    public string Description {
      get; private set;
    } = string.Empty;


    [DataField("ROL_CUENTA", Default = AccountRole.Sumaria)]
    public AccountRole Role {
      get; private set;
    } = AccountRole.Sumaria;


    [DataField("ID_TIPO_CUENTA")]
    private AccountType _accountType = FinancialAccounting.AccountType.Empty;

    public string AccountType {
      get {
        return _accountType.Name;
      }
    }


    [DataField("NATURALEZA", Default = DebtorCreditorType.Deudora)]
    public DebtorCreditorType DebtorCreditor {
      get; private set;
    } = DebtorCreditorType.Deudora;


    public int Level {
      get {
        return EmpiriaString.CountOccurences(Number, this.AccountsChart.MasterData.AccountNumberSeparator) + 1;
      }
    }


    public FixedList<Currency> Currencies {
      get {
        return _currencies.Value;
      }
    }


    public FixedList<Sector> Sectors {
      get {
        return _sectors.Value;
      }
    }


    #endregion Public properties

    #region Private methods

    private void ResetCurrencies() {
      _currencies = new Lazy<FixedList<Currency>>(() => Data.AccountsChartData.GetAccountCurrencies(this));
    }


    private void ResetSectors() {
      _sectors = new Lazy<FixedList<Sector>>(() => Data.AccountsChartData.GetAccountSectors(this));
    }

    #endregion Private methods

  }  // class Account

}  // namespace Empiria.FinancialAccounting
