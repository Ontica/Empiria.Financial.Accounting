﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Reconciliation Services                    Component : Use cases Layer                         *
*  Assembly : FinancialAccounting.Reconciliation.dll     Pattern   : Use case interactor class               *
*  Type     : OperationalDataUseCases                    License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Use cases used to read and write operational data for reconciliation processes.                *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;

using Empiria.Services;

using Empiria.FinancialAccounting.Datasets;
using Empiria.FinancialAccounting.Datasets.UseCases;
using Empiria.FinancialAccounting.Datasets.Adapters;

using Empiria.FinancialAccounting.Reconciliation.Adapters;

namespace Empiria.FinancialAccounting.Reconciliation.UseCases {

  /// <summary>Use cases used to read and write operational data for reconciliation processes.</summary>
  public class OperationalDataUseCases : UseCase {

    #region Constructors and parsers

    protected OperationalDataUseCases() {
      // no-op
    }


    static public OperationalDataUseCases UseCaseInteractor() {
      return UseCase.CreateInstance<OperationalDataUseCases>();
    }

    #endregion Constructors and parsers

    #region Use cases


    public DatasetsLoadStatusDto CreateDataset(OperationalDataCommand command,
                                               FileData fileData) {
      Assertion.Require(command, "command");
      Assertion.Require(fileData, "fileData");

      command.EnsureValid();

      using (var usecase = DatasetsUseCases.UseCaseInteractor()) {
        var coreDatasetCommand = command.MapToCoreDatasetsCommand();

        Dataset dataset = usecase.CreateDataset(coreDatasetCommand, fileData);

        var reader = new OperationalEntriesReader(dataset);

        if (!reader.AllEntriesAreValid()) {

          usecase.RemoveDataset(dataset.UID);

          Assertion.RequireFail(
            "El archivo tiene un formato que no reconozco o la información que contiene es incorrecta."
          );
        }

        //var entries = reader.GetEntries();

        //foreach (var entry in entries) {
        //  var re = new ReconciliationEntry(dataset, entry);
        //  re.Save();
        //}

      }

      return GetDatasetsLoadStatus(command);
    }


    public DatasetDto GetDataset(string datasetUID) {
      Assertion.Require(datasetUID, "datasetUID");

      using (var usecase = DatasetsUseCases.UseCaseInteractor()) {
        return usecase.GetDataset(datasetUID);
      }
    }


    public DatasetsLoadStatusDto GetDatasetsLoadStatus(OperationalDataCommand command) {
      Assertion.Require(command, "command");

      command.EnsureValid();

      RemoveOldDatasetsFor(command.GetReconciliationType());

      using (var usecase = DatasetsUseCases.UseCaseInteractor()) {

        var datasetCommand = command.MapToCoreDatasetsCommand();

        return usecase.GetDatasetsLoadStatus(datasetCommand);
      }
    }


    public DatasetsLoadStatusDto RemoveDataset(string datasetUID) {
      Assertion.Require(datasetUID, "datasetUID");

      using (var usecase = DatasetsUseCases.UseCaseInteractor()) {

        return usecase.RemoveDataset(datasetUID);
      }
    }


    internal void RemoveOldDatasetsFor(ReconciliationType reconciliationType) {
      Assertion.Require(reconciliationType, "reconciliationType");

      using (var usecase = DatasetsUseCases.UseCaseInteractor()) {

        usecase.RemoveOldDatasets(reconciliationType.DatasetFamily.UID,
                                  TimeSpan.FromHours(2));
      }
    }

    #endregion Use cases

  } // class OperationalDatasetsUseCases

} // Empiria.FinancialAccounting.Reconciliation.UseCases
