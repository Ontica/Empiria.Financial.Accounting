﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Vouchers Management                        Component : Use cases Layer                         *
*  Assembly : FinancialAccounting.Vouchers.dll           Pattern   : Use case interactor class               *
*  Type     : VoucherUseCases                            License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Use cases used to retrive and manage accounting vouchers.                                      *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;

using Empiria.Services;

using Empiria.FinancialAccounting.Vouchers.Adapters;
using Empiria.FinancialAccounting.Vouchers.Data;
using System.Linq;

namespace Empiria.FinancialAccounting.Vouchers.UseCases {

  /// <summary>Use cases used to retrive and manage accounting vouchers.</summary>
  public class VoucherUseCases : UseCase {

    #region Constructors and parsers

    protected VoucherUseCases() {
      // no-op
    }

    static public VoucherUseCases UseCaseInteractor() {
      return UseCase.CreateInstance<VoucherUseCases>();
    }

    #endregion Constructors and parsers

    #region Use cases

    public VoucherDto GetVoucher(long voucherId) {
      Assertion.Require(voucherId > 0, "Unrecognized voucherId.");

      Voucher voucher = Voucher.Parse(voucherId);

      return VoucherMapper.Map(voucher);
    }


    public FixedList<VoucherDto> GetVouchers(int[] voucherIdsArray) {
      Assertion.Require(voucherIdsArray, "voucherIdsArray");
      Assertion.Require(voucherIdsArray.Length > 0, "voucherIdsArray must have one or more values.");

      var vouchers = new List<VoucherDto>(voucherIdsArray.Length);

      foreach (var voucherId in voucherIdsArray) {
        var voucher = Voucher.Parse(voucherId);

        VoucherDto dto = VoucherMapper.Map(voucher);

        vouchers.Add(dto);
      }

      return vouchers.ToFixedList();
    }


    public FixedList<VoucherDto> GetVouchersToExport(int[] voucherIdsArray) {
      Assertion.Require(voucherIdsArray, "voucherIdsArray");
      Assertion.Require(voucherIdsArray.Length > 0, "voucherIdsArray must have one or more values.");

      var vouchers = new List<VoucherDto>(voucherIdsArray.Length);

      foreach (var voucherId in voucherIdsArray) {
        var voucher = VoucherData.GetVouchers(voucherId);
        
        if (voucher.Count == 0) {
          Assertion.EnsureFailed($"Una o más pólizas no contienen movimientos para exportar.");
        }

        VoucherDto dto = VoucherMapper.Map(voucher.FirstOrDefault());
        vouchers.Add(dto);
      }

      return vouchers.ToFixedList();
    }


    public VoucherEntryDto GetVoucherEntry(long voucherId, long voucherEntryId) {
      Assertion.Require(voucherId > 0, "Unrecognized voucherId.");
      Assertion.Require(voucherEntryId > 0, "Unrecognized voucherEntryId.");

      Voucher voucher = Voucher.Parse(voucherId);
      VoucherEntry voucherEntry = voucher.GetEntry(voucherEntryId);

      return VoucherMapper.MapEntry(voucherEntry);
    }

    public FixedList<VoucherDescriptorDto> SearchVouchers(VouchersQuery query) {
      Assertion.Require(query, nameof(query));

      query.EnsureIsValid();

      string filter = query.MapToFilterString();
      string sort = query.MapToSortString();

      FixedList<Voucher> list = Voucher.GetList(filter, sort, query.PageSize);

      return VoucherMapper.MapToDescriptor(list);
    }

    #endregion Use cases

  }  // class VoucherUseCases

}  // namespace Empiria.FinancialAccounting.Vouchers.UseCases
