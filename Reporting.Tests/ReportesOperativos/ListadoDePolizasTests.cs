﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Balance Engine                             Component : Test cases                              *
*  Assembly : FinancialAccounting.BalanceEngine.Tests    Pattern   : Use cases tests                         *
*  Type     : ListadoDePolizasTests                      License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Test cases for Listado de pólizas.                                                             *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using Empiria.FinancialAccounting.Reporting;
using Xunit;

namespace Empiria.FinancialAccounting.Tests.Reporting {

  /// <summary>Test cases for Comparativo de cuentas.</summary>
  public class ListadoDePolizasTests {


    [Fact]
    public void Should_Build_Listado_De_Polizas() {

      ReportBuilderQuery query = new ReportBuilderQuery {
        AccountsChartUID = "47ec2ec7-0f4f-482e-9799-c23107b60d8a",
        ElaboratedBy = "1857",
        ReportType = ReportTypes.ListadoDePolizas,
        FromDate = new DateTime(2023, 06, 01),
        ToDate = new DateTime(2023, 06, 30)
      };

      using (var service = ReportingService.ServiceInteractor()) {
        ReportDataDto sut = service.GenerateReport(query);

        Assert.NotNull(sut);
        Assert.Equal(query, sut.Query);
        Assert.NotEmpty(sut.Entries);
      }
    }


  } // class ListadoDePolizasTests

} // namespace Empiria.FinancialAccounting.Tests.Reporting
