﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Reporting Services                           Component : Web Api                               *
*  Assembly : Empiria.FinancialAccounting.WebApi.dll       Pattern   : Controller                            *
*  Type     : ReportGenerationController                   License   : Please read LICENSE.txt file          *
*                                                                                                            *
*  Summary  : Query web API used to generate financial accounting reports: financial, operational, fiscal.   *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Web.Http;

using Empiria.WebApi;

using Empiria.FinancialAccounting.Reporting;
using Empiria.FinancialAccounting.Reporting.Adapters;
using Empiria.FinancialAccounting.Reporting.UseCases;

namespace Empiria.FinancialAccounting.WebApi.Reporting {

  /// <summary>Query web API used to generate financial accounting reports:
  /// financial, operational, fiscal.</summary>
  public class ReportGenerationController : WebApiController {

    #region Web Apis


    [HttpPost]
    [Route("v2/financial-accounting/reporting/{reportType}/data")]
    public SingleObjectModel BuildReport([FromUri] string reportType,
                                         [FromBody] ReportBuilderQuery buildQuery) {
      base.RequireBody(buildQuery);

      buildQuery.ReportType = reportType;

      using (var service = ReportingService.ServiceInteractor()) {
        ReportDataDto reportData = service.GenerateReport(buildQuery);

        return new SingleObjectModel(this.Request, reportData);
      }
    }


    [HttpPost]
    [Route("v2/financial-accounting/reporting/{reportType}/export")]
    public SingleObjectModel ExportReportData([FromUri] string reportType,
                                              [FromBody] ReportBuilderQuery buildQuery) {
      base.RequireBody(buildQuery);

      buildQuery.ReportType = reportType;

      using (var service = ReportingService.ServiceInteractor()) {
        FileReportDto fileReportDto = service.ExportReport(buildQuery);

        return new SingleObjectModel(this.Request, fileReportDto);
      }
    }


    #endregion Web Apis

  } // class ReportGenerationController

} // namespace Empiria.FinancialAccounting.WebApi.Reporting
