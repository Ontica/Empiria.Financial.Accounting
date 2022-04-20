﻿/* Empiria Financial *****************************************************************************************
*                                                                                                            *
*  Module   : Balance Engine                             Component : Domain Layer                            *
*  Assembly : FinancialAccounting.BalanceEngine.dll      Pattern   : Helper methods                          *
*  Type     : TrialBalanceHelper                         License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Helper methods to build trial balances and related accounting information.                     *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Linq;
using System.Collections.Generic;

using Empiria.Collections;

using Empiria.FinancialAccounting.BalanceEngine.Adapters;
using Empiria.FinancialAccounting.BalanceEngine.Data;

namespace Empiria.FinancialAccounting.BalanceEngine {

  /// <summary>Helper methods to build trial balances and related accounting information.</summary>
  internal class TrialBalanceHelper {

    private readonly TrialBalanceCommand _command;

    internal TrialBalanceHelper(TrialBalanceCommand command) {
      _command = command;
    }


    internal List<TrialBalanceEntry> CombineCurrencyTotalsAndPostingEntries(
                                      List<TrialBalanceEntry> trialBalance,
                                      List<TrialBalanceEntry> summaryEntries) {
      var returnedEntries = new List<TrialBalanceEntry>();

      foreach (var currencyEntry in summaryEntries
                    .Where(a => a.ItemType == TrialBalanceItemType.BalanceTotalCurrency)) {
        var listSummaryByCurrency = trialBalance.Where(a => a.Ledger.Id == currencyEntry.Ledger.Id &&
                                                            a.Currency.Code == currencyEntry.Currency.Code).ToList();

        if (listSummaryByCurrency.Count > 0) {
          listSummaryByCurrency.Add(currencyEntry);
          returnedEntries.AddRange(listSummaryByCurrency);
        }
      }
      return OrderByLedgerAndCurrency(returnedEntries);
    }


    internal List<TrialBalanceEntry> CombineDebtorCreditorAndPostingEntries(
                                      List<TrialBalanceEntry> trialBalance,
                                      List<TrialBalanceEntry> summaryEntries) {
      var returnedEntries = new List<TrialBalanceEntry>();

      foreach (var debtorSummaryEntry in summaryEntries
                    .Where(a => a.ItemType == TrialBalanceItemType.BalanceTotalDebtor)) {
        var debtorsSummaryList = trialBalance.Where(a => a.Ledger.Id == debtorSummaryEntry.Ledger.Id &&
                                                  a.Currency.Code == debtorSummaryEntry.Currency.Code &&
                                                  a.DebtorCreditor == DebtorCreditorType.Deudora).ToList();

        if (debtorsSummaryList.Count > 0) {
          debtorsSummaryList.Add(debtorSummaryEntry);
          returnedEntries.AddRange(debtorsSummaryList);
        }
      }

      foreach (var creditorSummaryEntry in summaryEntries
                    .Where(a => a.ItemType == TrialBalanceItemType.BalanceTotalCreditor)) {
        var creditorsSummaryList = trialBalance.Where(a => a.Ledger.Id == creditorSummaryEntry.Ledger.Id &&
                                                  a.Currency.Code == creditorSummaryEntry.Currency.Code &&
                                                  a.DebtorCreditor == DebtorCreditorType.Acreedora).ToList();

        if (creditorsSummaryList.Count > 0) {
          creditorsSummaryList.Add(creditorSummaryEntry);
          returnedEntries.AddRange(creditorsSummaryList);
        }
      }
      return OrderByLedgerAndCurrency(returnedEntries);
    }


    internal List<TrialBalanceEntry> CombineGroupEntriesAndPostingEntries(
                                      List<TrialBalanceEntry> trialBalance,
                                      FixedList<TrialBalanceEntry> summaryGroupEntries) {
      var returnedEntries = new List<TrialBalanceEntry>();

      foreach (var totalGroupDebtorEntry in summaryGroupEntries
                    .Where(a => a.ItemType == TrialBalanceItemType.BalanceTotalGroupDebtor)) {

        var debtorEntries = trialBalance.Where(
                                  a => a.Account.GroupNumber == totalGroupDebtorEntry.GroupNumber &&
                                  a.Ledger.Id == totalGroupDebtorEntry.Ledger.Id &&
                                  a.Currency.Id == totalGroupDebtorEntry.Currency.Id &&
                                  a.Account.DebtorCreditor == DebtorCreditorType.Deudora).ToList();

        debtorEntries.Add(totalGroupDebtorEntry);
        returnedEntries.AddRange(debtorEntries);
      }

      foreach (var creditorEntry in summaryGroupEntries
                    .Where(a => a.ItemType == TrialBalanceItemType.BalanceTotalGroupCreditor)) {
        var creditorEntries = trialBalance.Where(
                                  a => a.Account.GroupNumber == creditorEntry.GroupNumber &&
                                  a.Ledger.Id == creditorEntry.Ledger.Id &&
                                  a.Currency.Id == creditorEntry.Currency.Id &&
                                  a.Account.DebtorCreditor == DebtorCreditorType.Acreedora).ToList();

        creditorEntries.Add(creditorEntry);
        returnedEntries.AddRange(creditorEntries);
      }

      return OrderByLedgerAndCurrency(returnedEntries);
    }


    internal List<TrialBalanceEntry> CombineSummaryAndPostingEntries(
                                      List<TrialBalanceEntry> summaryEntries,
                                      FixedList<TrialBalanceEntry> postingEntries) {
      var returnedEntries = new List<TrialBalanceEntry>(postingEntries);
      if (_command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta) {

        foreach (var entry in summaryEntries.Where(a => a.SubledgerAccountIdParent > 0)) {
          returnedEntries.Add(entry);
        }

      } else {
        returnedEntries.AddRange(summaryEntries);
      }
      returnedEntries = GetSubledgerAccountInfo(returnedEntries);
      returnedEntries = OrderingTrialBalance(returnedEntries);

      return returnedEntries;
    }


    internal List<TrialBalanceEntry> CombineTotalConsolidatedAndPostingEntries(
                                      List<TrialBalanceEntry> trialBalance,
                                      List<TrialBalanceEntry> summaryEntries) {
      var entries = new List<TrialBalanceEntry>(trialBalance);

      var consolidated = summaryEntries.FirstOrDefault(
                                  a => a.ItemType == TrialBalanceItemType.BalanceTotalConsolidated);

      if (consolidated != null) {
        entries.Add(consolidated);
      }

      return entries;
    }


    internal List<TrialBalanceEntry> CombineTotalConsolidatedByLedgerAndPostingEntries(
                                      List<TrialBalanceEntry> trialBalance,
                                      List<TrialBalanceEntry> totalConsolidatedByLedger) {
      if (totalConsolidatedByLedger.Count == 0) {
        return trialBalance;
      }

      var returnedEntries = new List<TrialBalanceEntry>();

      foreach (var consolidatedByLedger in totalConsolidatedByLedger) {
        var listSummaryByLedger = trialBalance.Where(a => a.Ledger.Id == consolidatedByLedger.Ledger.Id).ToList();
        if (listSummaryByLedger.Count > 0) {
          listSummaryByLedger.Add(consolidatedByLedger);
          returnedEntries.AddRange(listSummaryByLedger);
        }
      }
      return returnedEntries;
    }


    internal FixedList<TrialBalanceEntry> ConsolidateToTargetCurrency(
                                          FixedList<TrialBalanceEntry> trialBalance,
                                          TrialBalanceCommandPeriod commandPeriod) {

      var targetCurrency = Currency.Parse(commandPeriod.ValuateToCurrrencyUID);

      var summaryEntries = new EmpiriaHashTable<TrialBalanceEntry>();

      foreach (var entry in trialBalance) {
        string hash = $"{entry.Account.Number}||{entry.Sector.Code}||{targetCurrency.Id}||{entry.Ledger.Id}";

        if (_command.TrialBalanceType == TrialBalanceType.Balanza && _command.WithSubledgerAccount) {
          hash = $"{entry.Account.Number}||{entry.SubledgerAccountId}||" +
                 $"{entry.Sector.Code}||{targetCurrency.Id}||{entry.Ledger.Id}";
        }

        if (entry.Currency.Equals(targetCurrency)) {
          summaryEntries.Insert(hash, entry);
        } else if (summaryEntries.ContainsKey(hash)) {
          summaryEntries[hash].Sum(entry);
        } else {
          entry.Currency = targetCurrency;
          summaryEntries.Insert(hash, entry);
        }
      }

      return summaryEntries.Values.ToList()
                                  .ToFixedList();
    }


    internal List<TrialBalanceEntry> GenerateAverageBalance(List<TrialBalanceEntry> trialBalance) {
      var returnedEntries = new List<TrialBalanceEntry>(trialBalance);

      if (_command.WithAverageBalance) {

        foreach (var entry in returnedEntries.Where(a => a.ItemType == TrialBalanceItemType.Summary ||
                  (_command.TrialBalanceType == TrialBalanceType.BalanzaConContabilidadesEnCascada &&
                   (a.ItemType == TrialBalanceItemType.BalanceTotalGroupDebtor ||
                    a.ItemType == TrialBalanceItemType.BalanceTotalGroupCreditor)) ||
                  (_command.TrialBalanceType == TrialBalanceType.BalanzaValorizadaComparativa))) {

          decimal debtorCreditor = entry.DebtorCreditor == DebtorCreditorType.Deudora ?
                                   entry.Debit - entry.Credit : entry.Credit - entry.Debit;

          TimeSpan timeSpan = _command.TrialBalanceType == TrialBalanceType.BalanzaValorizadaComparativa ?
                                                  _command.FinalPeriod.ToDate - entry.LastChangeDate :
                                                  _command.InitialPeriod.ToDate - entry.LastChangeDate;
          int numberOfDays = timeSpan.Days + 1;

          entry.AverageBalance = ((numberOfDays * debtorCreditor) /
                                   _command.InitialPeriod.ToDate.Day) +
                                   entry.InitialBalance;
        }
      }

      return returnedEntries;
    }


    internal List<TrialBalanceEntry> GenerateAverageDailyBalance(List<TrialBalanceEntry> trialBalance,
                                                                 TrialBalanceCommandPeriod commandPeriod) {
      List<TrialBalanceEntry> averageBalances = new List<TrialBalanceEntry>(trialBalance);

      TimeSpan timeSpan = commandPeriod.ToDate - commandPeriod.FromDate;
      int numberOfDays = timeSpan.Days + 1;

      foreach (var entry in averageBalances) {
        entry.AverageBalance = entry.CurrentBalance / numberOfDays;
      }

      return averageBalances;
    }


    private List<TrialBalanceEntry> GetEntriesMappedForSectorization(
                                    List<TrialBalanceEntry> entriesList) {
      var returnedEntriesMapped = new List<TrialBalanceEntry>();
      var isBalanceEntry = entriesList.FirstOrDefault(a => a.ItemType == TrialBalanceItemType.Entry);
      if (isBalanceEntry != null) {
        foreach (var entry in entriesList) {
          TrialBalanceEntry balanceEntry = TrialBalanceMapper.MapToTrialBalanceEntry(entry);
          balanceEntry.LastChangeDate = entry.LastChangeDate;
          balanceEntry.AverageBalance = entry.AverageBalance;
          balanceEntry.SecondExchangeRate = entry.SecondExchangeRate;
          balanceEntry.DebtorCreditor = entry.DebtorCreditor;
          balanceEntry.SubledgerAccountIdParent = entry.SubledgerAccountIdParent;
          balanceEntry.SubledgerAccountNumber = entry.SubledgerAccountNumber;
          balanceEntry.SubledgerNumberOfDigits = entry.SubledgerNumberOfDigits;
          balanceEntry.IsParentPostingEntry = entry.IsParentPostingEntry;
          returnedEntriesMapped.Add(balanceEntry);
        }
      } else {
        returnedEntriesMapped = entriesList;
      }
      return returnedEntriesMapped;
    }


    internal FixedList<TrialBalanceEntry> GetPostingEntries() {

      FixedList<TrialBalanceEntry> postingEntries = GetTrialBalanceEntries(_command.InitialPeriod);

      if ((_command.ValuateBalances || _command.InitialPeriod.UseDefaultValuation) &&
          _command.TrialBalanceType != TrialBalanceType.BalanzaDolarizada &&
          _command.TrialBalanceType != TrialBalanceType.BalanzaEnColumnasPorMoneda) {
        postingEntries = ValuateToExchangeRate(postingEntries, _command.InitialPeriod);

        if (_command.ConsolidateBalancesToTargetCurrency) {
          postingEntries = ConsolidateToTargetCurrency(postingEntries, _command.InitialPeriod);
        }
      }

      postingEntries = RoundTrialBalanceEntries(postingEntries);

      return postingEntries;
    }


    internal List<TrialBalanceEntry> GetSummaryByLevelAndSector(List<TrialBalanceEntry> summaryEntries) {
      var entries = new List<TrialBalanceEntry>(summaryEntries);

      if (_command.UseNewSectorizationModel) {
        var summaryEntriesList = new List<TrialBalanceEntry>(summaryEntries);
        foreach (var entry in summaryEntriesList) {
          List<TrialBalanceEntry> entriesWithSummarySector;
          if (_command.TrialBalanceType == TrialBalanceType.AnaliticoDeCuentas ||
              _command.TrialBalanceType == TrialBalanceType.Balanza) {

            entriesWithSummarySector = summaryEntries.Where(a => a.Account.Number == entry.Account.Number &&
                                                                 a.Ledger.Number == entry.Ledger.Number &&
                                                                 a.Currency.Code == entry.Currency.Code &&
                                                                 a.DebtorCreditor == entry.DebtorCreditor
                                                            ).ToList();
          } else {
            entriesWithSummarySector = summaryEntries.Where(a => a.Account.Number == entry.Account.Number &&
                                                                 a.Ledger.Number == entry.Ledger.Number &&
                                                                 a.Currency.Code == entry.Currency.Code
                                                            ).ToList();
          }

          if (entry.Level > 1 &&
               (entriesWithSummarySector.Count == 2 &&
                entry.ItemType == TrialBalanceItemType.Summary) ||
               (entry.ItemType == TrialBalanceItemType.Entry &&
               entriesWithSummarySector.Count == 2 && entry.Sector.Code != "00")) {
            var entryWithoutSector = entriesWithSummarySector.FirstOrDefault(a => a.Sector.Code == "00");
            if (_command.TrialBalanceType != TrialBalanceType.AnaliticoDeCuentas &&
                _command.TrialBalanceType != TrialBalanceType.Balanza) {
              entries.Remove(entryWithoutSector);
            }
          }
        }
      }

      return entries;
    }


    internal List<TrialBalanceEntry> GetSummaryEntriesAndSectorization(
                                      List<TrialBalanceEntry> entriesList) {

      var startTime = DateTime.Now;

      var entriesMapped = GetEntriesMappedForSectorization(entriesList);

      var hashEntries = new EmpiriaHashTable<TrialBalanceEntry>();
      var checkSummaryEntries = new List<TrialBalanceEntry>(entriesMapped);
      var returnedEntries = new List<TrialBalanceEntry>(entriesMapped);

      if (_command.UseNewSectorizationModel && _command.WithSectorization) {
        returnedEntries = GetSummaryEntriesWithSectorization(
                           hashEntries, checkSummaryEntries, returnedEntries);
        EmpiriaLog.Debug($"INNER GetSummaryEntriesWithSectorization(): {DateTime.Now.Subtract(startTime).TotalSeconds} seconds.");

      } else if (_command.UseNewSectorizationModel && !_command.WithSectorization) {
        returnedEntries = GetSummaryEntriesWithoutSectorization(
                           hashEntries, checkSummaryEntries, returnedEntries);
        EmpiriaLog.Debug($"INNER GetSummaryEntriesWithoutSectorization(): {DateTime.Now.Subtract(startTime).TotalSeconds} seconds.");
      }

      GetSummaryToSectorZeroForPesosAndUdis(returnedEntries.ToList());

      returnedEntries = GetSummaryByLevelAndSector(returnedEntries.ToList());

      EmpiriaLog.Debug($"INNER GetSummaryByLevelAndSector(): {DateTime.Now.Subtract(startTime).TotalSeconds} seconds.");

      return returnedEntries;
    }


    internal FixedList<TrialBalanceEntry> GetSummaryToParentEntries(
                                            FixedList<TrialBalanceEntry> postingEntries) {
      var returnedEntries = new List<TrialBalanceEntry>(postingEntries);
      foreach (var entry in postingEntries) {
        StandardAccount currentParent = entry.Account.GetParent();

        var entryParent = returnedEntries.FirstOrDefault(a => a.Account.Number == currentParent.Number &&
                                                a.Currency.Code == entry.Currency.Code &&
                                                a.Ledger.Number == entry.Ledger.Number &&
                                                a.Sector.Code == entry.Sector.Code &&
                                                a.Account.DebtorCreditor == entry.Account.DebtorCreditor);
        if (entryParent != null) {
          entry.HasParentPostingEntry = true;
          entryParent.IsParentPostingEntry = true;
          entryParent.Sum(entry);
        }
      }

      return returnedEntries.ToFixedList();
    }


    internal List<TrialBalanceEntry> GenerateSummaryEntries(FixedList<TrialBalanceEntry> entries) {
      var summaryEntries = new EmpiriaHashTable<TrialBalanceEntry>(entries.Count);
      var detailSummaryEntries = new List<TrialBalanceEntry>();

      foreach (var entry in entries) {
        entry.DebtorCreditor = entry.Account.DebtorCreditor;
        entry.SubledgerAccountNumber = SubledgerAccount.Parse(entry.SubledgerAccountId).Number ?? "";
        StandardAccount currentParent;

        if ((entry.Account.NotHasParent) ||
            _command.WithSubledgerAccount ||
            _command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta) {
          currentParent = entry.Account;

        } else if (_command.DoNotReturnSubledgerAccounts && entry.Account.HasParent) {
          currentParent = entry.Account.GetParent();

        } else if (_command.DoNotReturnSubledgerAccounts && entry.Account.NotHasParent) {
          continue;
        } else {
          throw Assertion.AssertNoReachThisCode();
        }

        if (!entry.HasParentPostingEntry) {
          int cont = 0;
          while (true) {
            entry.DebtorCreditor = entry.Account.DebtorCreditor;
            entry.SubledgerAccountIdParent = entry.SubledgerAccountId;

            if (entry.Level > 1) {
              SummaryByEntry(summaryEntries, entry, currentParent,
                              entry.Sector, TrialBalanceItemType.Summary);

              SummaryEntryBySectorization(summaryEntries, entry, currentParent);
            }

            cont++;
            if (cont == 1 && _command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta) {
              GetDetailSummaryEntries(detailSummaryEntries, summaryEntries, currentParent, entry);
            }
            if (!currentParent.HasParent && entry.HasSector) {
              GetEntriesAndParentSector(summaryEntries, entry, currentParent);
              break;

            } else if (!currentParent.HasParent) {
              if (_command.TrialBalanceType == TrialBalanceType.AnaliticoDeCuentas &&
                  _command.WithSubledgerAccount && !entry.Account.HasParent) {
                SummaryByEntry(summaryEntries, entry, currentParent,
                                Sector.Empty, TrialBalanceItemType.Summary);
              }
              break;
            } else {
              currentParent = currentParent.GetParent();
            }

          } // while
        }
      } // foreach

      var returnedEntries = AssignLastChangeDates(entries, summaryEntries);

      if (detailSummaryEntries.Count > 0 && _command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta) {
        return detailSummaryEntries;
      }
      return returnedEntries;
    }


    internal List<TrialBalanceEntry> GenerateTotalSummaryDebtorCreditor(
                                      List<TrialBalanceEntry> postingEntries) {

      var totalSummaryDebtorCredtor = new EmpiriaHashTable<TrialBalanceEntry>(postingEntries.Count);

      foreach (var entry in postingEntries.Where(a => !a.HasParentPostingEntry)) {

        if (entry.Account.DebtorCreditor == DebtorCreditorType.Deudora) {
          SummaryByDebtorCreditorEntries(totalSummaryDebtorCredtor, entry, StandardAccount.Empty,
                                         Sector.Empty, TrialBalanceItemType.BalanceTotalDebtor);
        }
        if (entry.Account.DebtorCreditor == DebtorCreditorType.Acreedora) {
          SummaryByDebtorCreditorEntries(totalSummaryDebtorCredtor, entry, StandardAccount.Empty,
                                         Sector.Empty, TrialBalanceItemType.BalanceTotalCreditor);
        }

      }

      return totalSummaryDebtorCredtor.Values.ToList();
    }


    internal List<TrialBalanceEntry> GenerateTotalSummaryCurrency(List<TrialBalanceEntry> entries) {
      var totalSummaryCurrencies = new EmpiriaHashTable<TrialBalanceEntry>(entries.Count);

      foreach (var debtorOrCreditorEntry in entries.Where(
                a => a.ItemType == TrialBalanceItemType.BalanceTotalDebtor ||
                     a.ItemType == TrialBalanceItemType.BalanceTotalCreditor)) {

        SummaryByCurrencyEntries(totalSummaryCurrencies, debtorOrCreditorEntry, StandardAccount.Empty,
                            Sector.Empty, TrialBalanceItemType.BalanceTotalCurrency);
      }

      entries.AddRange(totalSummaryCurrencies.Values.ToList());

      return entries;
    }


    internal List<TrialBalanceEntry> GenerateTotalSummaryConsolidated(
                                      List<TrialBalanceEntry> balanceEntries) {
      var totalSummaryConsolidated = new EmpiriaHashTable<TrialBalanceEntry>(balanceEntries.Count);

      foreach (var currencyEntry in balanceEntries.Where(
                a => a.ItemType == TrialBalanceItemType.BalanceTotalCurrency)) {

        TrialBalanceEntry entry = TrialBalanceMapper.MapToTrialBalanceEntry(currencyEntry);

        entry.GroupName = "TOTAL CONSOLIDADO GENERAL";

        string hash;
        if (_command.TrialBalanceType == TrialBalanceType.BalanzaConContabilidadesEnCascada ||
             (_command.TrialBalanceType == TrialBalanceType.Balanza && _command.ShowCascadeBalances)) {
          if (_command.TrialBalanceType == TrialBalanceType.BalanzaConContabilidadesEnCascada) {
            entry.GroupName = "TOTAL DEL REPORTE";
          }
          hash = $"{entry.GroupName}";
          entry.GroupNumber = "";
        } else {
          if (_command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta &&
              ((_command.WithSubledgerAccount && _command.ShowCascadeBalances) ||
               _command.ShowCascadeBalances)) {
            hash = $"{entry.GroupName}||{Sector.Empty.Code}";
          } else {
            hash = $"{entry.GroupName}||{Sector.Empty.Code}||{entry.Ledger.Id}";
          }

        }

        GenerateOrIncreaseEntries(totalSummaryConsolidated, entry, StandardAccount.Empty, Sector.Empty,
                                  TrialBalanceItemType.BalanceTotalConsolidated, hash);
      }

      balanceEntries.AddRange(totalSummaryConsolidated.Values.ToList());

      return balanceEntries;
    }

    internal List<TrialBalanceEntry> GenerateTotalSummaryConsolidatedByLedger(
                                      List<TrialBalanceEntry> summaryCurrencies) {

      var summaryConsolidatedByLedger = new EmpiriaHashTable<TrialBalanceEntry>(summaryCurrencies.Count);
      List<TrialBalanceEntry> returnedListEntries = new List<TrialBalanceEntry>();
      if (_command.TrialBalanceType == TrialBalanceType.Balanza && _command.ShowCascadeBalances) {
        foreach (var currencyEntry in summaryCurrencies.Where(
                        a => a.ItemType == TrialBalanceItemType.BalanceTotalCurrency)) {

          TrialBalanceEntry entry = TrialBalanceMapper.MapToTrialBalanceEntry(currencyEntry);

          entry.GroupName = $"TOTAL CONSOLIDADO {entry.Ledger.FullName}";
          entry.Currency = Currency.Empty;
          string hash = $"{entry.Ledger.Id}||{entry.GroupName}||{Sector.Empty.Code}";

          GenerateOrIncreaseEntries(summaryConsolidatedByLedger, entry, StandardAccount.Empty, Sector.Empty,
                                    TrialBalanceItemType.BalanceTotalConsolidatedByLedger, hash);
        }

        returnedListEntries.AddRange(summaryConsolidatedByLedger.Values.ToList());
      }
      return returnedListEntries.OrderBy(a => a.Ledger.Number).ToList();
    }


    internal FixedList<TrialBalanceEntry> GenerateTotalSummaryGroups(FixedList<TrialBalanceEntry> entries) {

      var toReturnSummaryGroupEntries = new EmpiriaHashTable<TrialBalanceEntry>();

      foreach (var entry in entries.Where(a=> !a.HasParentPostingEntry)) {
          SummaryByGroupEntries(toReturnSummaryGroupEntries, entry);
      }

      return toReturnSummaryGroupEntries.ToFixedList();
    }


    internal FixedList<TrialBalanceEntry> GetTrialBalanceEntries(TrialBalanceCommandPeriod commandPeriod) {

      if (_command.TrialBalanceType == TrialBalanceType.BalanzaConContabilidadesEnCascada) {
        _command.ShowCascadeBalances = true;
      }

      TrialBalanceCommandData commandData = _command.MapToTrialBalanceCommandData(commandPeriod);

      return TrialBalanceDataService.GetTrialBalanceEntries(commandData);
    }

    internal List<TrialBalanceEntry> RestrictLevels(List<TrialBalanceEntry> entries) {
      if (_command.Level == 0) {
        return entries;
      }

      if (_command.DoNotReturnSubledgerAccounts) {
        return entries.FindAll(x => x.Level <= _command.Level);
      } else if (_command.WithSubledgerAccount) {
        return entries.FindAll(x => x.Level <= _command.Level);
      } else {
        throw Assertion.AssertNoReachThisCode();
      }
    }


    internal FixedList<TrialBalanceEntry> RoundTrialBalanceEntries(
                                          FixedList<TrialBalanceEntry> postingEntries) {
      FixedList<TrialBalanceEntry> roundedEntries = new FixedList<TrialBalanceEntry>(postingEntries);
      foreach (var posting in roundedEntries) {
        posting.InitialBalance = Math.Round(posting.InitialBalance, 2);
        posting.Debit = Math.Round(posting.Debit, 2);
        posting.Credit = Math.Round(posting.Credit, 2);
        posting.CurrentBalance = Math.Round(posting.CurrentBalance, 2);
      }
      return roundedEntries;
    }


    internal void SummaryByAccount(EmpiriaHashTable<TrialBalanceEntry> entries, TrialBalanceEntry balanceEntry) {

      TrialBalanceEntry entry = TrialBalanceMapper.MapToTrialBalanceEntry(balanceEntry);

      if (entry.ItemType == TrialBalanceItemType.Summary && entry.Level == 1 && entry.HasSector) {
        entry.InitialBalance = 0;
        entry.Debit = 0;
        entry.Credit = 0;
        entry.CurrentBalance = 0;
      }
      entry.LastChangeDate = balanceEntry.LastChangeDate;

      TrialBalanceItemType itemType = TrialBalanceItemType.Entry;

      string hash = $"{entry.Account.Number}";

      GenerateOrIncreaseEntries(entries, entry, entry.Account,
                                entry.Sector, itemType, hash);

    }


    internal void SummaryByCurrencyEntries(EmpiriaHashTable<TrialBalanceEntry> summaryEntries,
                                          TrialBalanceEntry balanceEntry,
                                          StandardAccount targetAccount, Sector targetSector,
                                          TrialBalanceItemType itemType) {

      TrialBalanceEntry entry = TrialBalanceMapper.MapToTrialBalanceEntry(balanceEntry);
      string hash;
      entry.GroupName = "TOTAL MONEDA " + entry.Currency.FullName;

      if (_command.TrialBalanceType != TrialBalanceType.BalanzaConContabilidadesEnCascada &&
           entry.ItemType == TrialBalanceItemType.BalanceTotalCreditor) {
        entry.InitialBalance = -1 * entry.InitialBalance;
        entry.CurrentBalance = -1 * entry.CurrentBalance;
      }

      if (_command.TrialBalanceType == TrialBalanceType.BalanzaConContabilidadesEnCascada) {
        entry.GroupNumber = "";
        hash = $"{entry.GroupName}||{entry.Currency.Id}";
      } else {
        hash = $"{entry.GroupName}||{targetSector.Code}||{entry.Currency.Id}||{entry.Ledger.Id}";
      }
      GenerateOrIncreaseEntries(summaryEntries, entry, targetAccount, targetSector, itemType, hash);
    }


    internal void SummaryByEntry(EmpiriaHashTable<TrialBalanceEntry> summaryEntries,
                                 TrialBalanceEntry entry,
                                 StandardAccount targetAccount, Sector targetSector,
                                 TrialBalanceItemType itemType) {

      string hash = $"{targetAccount.Number}||{targetSector.Code}||{entry.Currency.Id}||{entry.Ledger.Id}";
      if (_command.TrialBalanceType == TrialBalanceType.AnaliticoDeCuentas ||
          _command.TrialBalanceType == TrialBalanceType.Balanza ||
          (_command.TrialBalanceType == TrialBalanceType.BalanzaEnColumnasPorMoneda &&
           _command.UseNewSectorizationModel)) {

        hash = $"{targetAccount.Number}||{targetSector.Code}||{entry.Currency.Id}||{entry.Ledger.Id}||{entry.DebtorCreditor}";

      }
      GenerateOrIncreaseEntries(summaryEntries, entry, targetAccount, targetSector, itemType, hash);
    }


    internal void SummaryByLedgersGroupEntries(EmpiriaHashTable<TrialBalanceEntry> totalsListByGroupEntries,
                                                TrialBalanceEntry balanceEntry) {
      TrialBalanceEntry groupEntry = TrialBalanceMapper.MapToTrialBalanceEntry(balanceEntry);

      groupEntry.GroupName = $"SUMA DE DELEGACIONES";
      groupEntry.GroupNumber = balanceEntry.Account.Number;
      groupEntry.Account = balanceEntry.Account;
      groupEntry.Sector = balanceEntry.Sector;
      groupEntry.DebtorCreditor = balanceEntry.Account.DebtorCreditor;
      groupEntry.Ledger = Ledger.Empty;

      TrialBalanceItemType itemType = TrialBalanceItemType.BalanceTotalGroupDebtor;

      if (balanceEntry.DebtorCreditor == DebtorCreditorType.Acreedora) {
        itemType = TrialBalanceItemType.BalanceTotalGroupCreditor;
      }

      string hash = $"{balanceEntry.Currency.Id}||{groupEntry.GroupNumber}||" +
                    $"{groupEntry.Sector.Code}||{groupEntry.DebtorCreditor}";

      GenerateOrIncreaseEntries(totalsListByGroupEntries, groupEntry, groupEntry.Account,
                                groupEntry.Sector, itemType, hash);
    }


    internal void SummaryBySectorization(EmpiriaHashTable<TrialBalanceEntry> hashSummaryEntries,
                                         TrialBalanceEntry balanceEntry) {
      TrialBalanceEntry entry = TrialBalanceMapper.MapToTrialBalanceEntry(balanceEntry);
      if (entry.NotHasSector) {
        entry.InitialBalance = 0;
        entry.Debit = 0;
        entry.Credit = 0;
        entry.CurrentBalance = 0;
      }
      string hash = $"{entry.Account.Number}||{entry.Currency.Id}||{entry.Ledger.Id}";
      //{targetSector.Code}||
      GenerateOrIncreaseEntries(hashSummaryEntries, entry, entry.Account, Sector.Empty,
                                TrialBalanceItemType.Summary, hash);

    }


    internal void SummaryBySubledgerAccountEntry(EmpiriaHashTable<TrialBalanceEntry> summaryEntries,
                                                 TrialBalanceEntry entry, TrialBalanceItemType itemType) {

      TrialBalanceEntry balanceEntry = TrialBalanceMapper.MapToTrialBalanceEntry(entry);

      balanceEntry.SubledgerAccountIdParent = entry.SubledgerAccountIdParent;

      string hash = $"{entry.Account.Number}||{entry.Sector.Code}||{entry.Currency.Id}||{entry.Ledger.Id}";

      GenerateOrIncreaseEntries(summaryEntries, balanceEntry,
                                StandardAccount.Empty, Sector.Empty, itemType, hash);
    }


    internal void SummaryDebtorCreditorLedgersByAccount(EmpiriaHashTable<TrialBalanceEntry> summaryEntries,
                                                TrialBalanceEntry balanceEntry,
                                                TrialBalanceItemType itemType) {

      TrialBalanceEntry entry = TrialBalanceMapper.MapToTrialBalanceEntry(balanceEntry);

      if (itemType == TrialBalanceItemType.BalanceTotalDebtor) {
        entry.GroupName = "TOTAL DEUDORAS " + entry.Currency.FullName;
      } else if (itemType == TrialBalanceItemType.BalanceTotalCreditor) {
        entry.GroupName = "TOTAL ACREEDORAS " + entry.Currency.FullName;
      }
      entry.Ledger = Ledger.Empty;
      entry.DebtorCreditor = balanceEntry.DebtorCreditor;

      string hash = $"{entry.GroupName}||{entry.Currency.Id}";

      GenerateOrIncreaseEntries(summaryEntries, entry, StandardAccount.Empty, Sector.Empty, itemType, hash);
    }


    private void SummaryEntryBySectorization(EmpiriaHashTable<TrialBalanceEntry> summaryEntries,
                                             TrialBalanceEntry entry, StandardAccount currentParent) {
      if (_command.UseNewSectorizationModel) {
        if (currentParent.HasParent && entry.HasSector) {
          if (_command.WithSectorization) {
            SummaryByEntry(summaryEntries, entry, currentParent, entry.Sector.Parent,
                                            TrialBalanceItemType.Summary);
          }
        }
      }
    }


    internal List<TrialBalanceEntry> TrialBalanceWithSubledgerAccounts(List<TrialBalanceEntry> trialBalance) {
      List<TrialBalanceEntry> returnedEntries = new List<TrialBalanceEntry>(trialBalance);

      if (!_command.WithSubledgerAccount && _command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta) {
        returnedEntries = returnedEntries.Where(a => a.SubledgerNumberOfDigits == 0
                                        //&& a.CurrentBalance != 0
                                        ).ToList();
      }
      if (_command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta) {
        returnedEntries = returnedEntries.Where(
                            a => a.ItemType != TrialBalanceItemType.BalanceTotalGroupDebtor &&
                                 a.ItemType != TrialBalanceItemType.BalanceTotalGroupCreditor
                                        ).ToList();
      }

      return returnedEntries;
    }


    internal FixedList<TrialBalanceEntry> ValuateToExchangeRate(FixedList<TrialBalanceEntry> entries,
                                                                TrialBalanceCommandPeriod commandPeriod,
                                                                bool isSecondPeriod = false) {
      if (commandPeriod.UseDefaultValuation) {
        commandPeriod.ExchangeRateTypeUID = ExchangeRateType.ValorizacionBanxico.UID;
        commandPeriod.ValuateToCurrrencyUID = "01";
        commandPeriod.ExchangeRateDate = commandPeriod.ToDate;
      }

      var exchangeRateType = ExchangeRateType.Parse(commandPeriod.ExchangeRateTypeUID);

      FixedList<ExchangeRate> exchangeRates = ExchangeRate.GetList(exchangeRateType, commandPeriod.ExchangeRateDate);

      foreach (var entry in entries.Where(a => a.Currency.Code != "01")) {
        var exchangeRate = exchangeRates.FirstOrDefault(a => a.FromCurrency.Code == commandPeriod.ValuateToCurrrencyUID &&
                                                             a.ToCurrency.Code == entry.Currency.Code);

        Assertion.AssertObject(exchangeRate, $"No se ha registrado el tipo de cambio para la " +
                                             $"moneda {entry.Currency.FullName} en la fecha proporcionada.");

        if (_command.TrialBalanceType == TrialBalanceType.BalanzaValorizadaComparativa) {
          if (isSecondPeriod) {
            entry.SecondExchangeRate = exchangeRate.Value;
          } else {
            entry.ExchangeRate = exchangeRate.Value;
          }
        } else if ((_command.TrialBalanceType == TrialBalanceType.BalanzaDolarizada) ||
                  (_command.IsOperationalReport && !_command.ConsolidateBalancesToTargetCurrency)) {
          entry.ExchangeRate = exchangeRate.Value;
        } else {
          entry.MultiplyBy(exchangeRate.Value);
        }

      }
      return entries;
    }

    #region Private methods

    private List<TrialBalanceEntry> AssignLastChangeDates(
                                      FixedList<TrialBalanceEntry> entries,
                                      EmpiriaHashTable<TrialBalanceEntry> summaryEntries) {

      var summaryEntriesList = new List<TrialBalanceEntry>(summaryEntries.Values);

      foreach (var entry in entries) {
        StandardAccount currentParent;
        currentParent = entry.Account.GetParent();

        SetLastChangeDateToSubledgerAccounts(entry, summaryEntriesList);

        LastChangeDateToSummaryEntries(entry, currentParent, summaryEntriesList);
      }

      return summaryEntriesList;
    }


    private void GetEntriesAndParentSector(EmpiriaHashTable<TrialBalanceEntry> summaryEntries,
                                          TrialBalanceEntry entry, StandardAccount currentParent) {
      if (!_command.WithSectorization) {
        SummaryByEntry(summaryEntries, entry, currentParent, Sector.Empty,
                          TrialBalanceItemType.Summary);
      } else {
        var parentSector = entry.Sector.Parent;
        while (true) {
          SummaryByEntry(summaryEntries, entry, currentParent, parentSector,
                                          TrialBalanceItemType.Summary);
          if (parentSector.IsRoot) {
            break;
          } else {
            parentSector = parentSector.Parent;
          }
        }
      }
    }


    private void GetDetailSummaryEntries(List<TrialBalanceEntry> detailSummaryEntries,
                                         EmpiriaHashTable<TrialBalanceEntry> summaryEntries,
                                         StandardAccount currentParent, TrialBalanceEntry entry) {

      TrialBalanceEntry detailsEntry;
      string key = $"{currentParent.Number}||{entry.Sector.Code}||{entry.Currency.Id}||{entry.Ledger.Id}";

      summaryEntries.TryGetValue(key, out detailsEntry);
      if (detailsEntry != null) {
        var existEntry = detailSummaryEntries.FirstOrDefault(a =>
                                                       a.Account.Number == detailsEntry.Account.Number &&
                                                       a.Ledger.Id == detailsEntry.Ledger.Id &&
                                                       a.Currency.Id == detailsEntry.Currency.Id &&
                                                       a.Sector.Code == detailsEntry.Sector.Code);
        if (existEntry == null) {
          detailSummaryEntries.Add(detailsEntry);
        }
      }
    }


    private List<TrialBalanceEntry> GetSubledgerAccountInfo(List<TrialBalanceEntry> entriesList) {
      var returnedEntries = new List<TrialBalanceEntry>(entriesList);

      if (_command.WithSubledgerAccount && (_command.TrialBalanceType == TrialBalanceType.Balanza ||
          _command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta ||
          _command.TrialBalanceType == TrialBalanceType.AnaliticoDeCuentas)) {
        foreach (var entry in entriesList) {
          SubledgerAccount subledgerAccount = SubledgerAccount.Parse(entry.SubledgerAccountId);
          if (!subledgerAccount.IsEmptyInstance) {
            entry.SubledgerAccountNumber = subledgerAccount.Number != "0" ?
                                           subledgerAccount.Number : "";
            entry.SubledgerNumberOfDigits = entry.SubledgerAccountNumber != "" ?
                                            entry.SubledgerAccountNumber.Count() : 0;
          }
        }
      }
      return returnedEntries;
    }


    private List<TrialBalanceEntry> GetSummaryEntriesWithoutSectorization(
                                    EmpiriaHashTable<TrialBalanceEntry> hashEntries,
                                    List<TrialBalanceEntry> checkSummaryEntries,
                                    List<TrialBalanceEntry> returnedEntries) {
      foreach (var entry in checkSummaryEntries) {
        var sectorParent = entry.Sector.Parent;
        var summaryEntry = returnedEntries.FirstOrDefault(a => a.Account.Number == entry.Account.Number &&
                                                               a.Ledger.Number == entry.Ledger.Number &&
                                                               a.Currency.Code == entry.Currency.Code &&
                                                               a.Sector.Code == "00");

        if (summaryEntry != null && sectorParent.Code != "00" && entry.HasSector && entry.Level > 1) {
          summaryEntry.InitialBalance += entry.InitialBalance;
          summaryEntry.Debit += entry.Debit;
          summaryEntry.Credit += entry.Credit;
          summaryEntry.CurrentBalance += entry.CurrentBalance;
        } else if (entry.HasSector && entry.Level > 1) {
          SummaryByEntry(hashEntries, entry, entry.Account, Sector.Empty, TrialBalanceItemType.Summary);
        }
      }

      if (hashEntries.Count > 0) {
        foreach (var hashEntry in hashEntries.ToFixedList().ToList()) {
          var entry = returnedEntries.FirstOrDefault(
                                      a => a.Account.Number == hashEntry.Account.Number &&
                                           a.Ledger.Number == hashEntry.Ledger.Number &&
                                           a.Currency.Code == hashEntry.Currency.Code &&
                                           a.Sector.Code == hashEntry.Sector.Code && a.Sector.Code == "00");
          if (entry == null) {
            returnedEntries.Add(hashEntry);
          } else {
            hashEntry.InitialBalance += entry.InitialBalance;
            hashEntry.Debit += entry.Debit;
            hashEntry.Credit += entry.Credit;
            hashEntry.CurrentBalance += entry.CurrentBalance;
            returnedEntries.Remove(entry);
            returnedEntries.Add(hashEntry);
          }
        }
      }
      return returnedEntries;
    }


    private List<TrialBalanceEntry> GetSummaryEntriesWithSectorization(
                                    EmpiriaHashTable<TrialBalanceEntry> hashEntries,
                                    List<TrialBalanceEntry> checkSummaryEntries,
                                    List<TrialBalanceEntry> returnedEntries) {
      foreach (var entry in checkSummaryEntries) {
        var sectorParent = entry.Sector.Parent;
        var summaryEntry = returnedEntries.FirstOrDefault(a => a.Account.Number == entry.Account.Number &&
                                                               a.Ledger.Number == entry.Ledger.Number &&
                                                               a.Currency.Code == entry.Currency.Code &&
                                                               a.Sector.Code == "00");
        if (summaryEntry != null && sectorParent.Code != "00" && entry.Level > 1) {

          summaryEntry.InitialBalance += entry.InitialBalance;
          summaryEntry.Debit += entry.Debit;
          summaryEntry.Credit += entry.Credit;
          summaryEntry.CurrentBalance += entry.CurrentBalance;

        } else if ((sectorParent.Code != "00" ||
                   (entry.ItemType == TrialBalanceItemType.Entry && entry.HasSector)) &&
                    entry.Level > 1) {
          SummaryByEntry(hashEntries, entry, entry.Account, Sector.Empty, entry.ItemType);
        }
      }
      if (hashEntries.Count > 0) {
        returnedEntries.AddRange(hashEntries.ToFixedList().ToList());
      }
      return returnedEntries;
    }


    private void GetSummaryToSectorZeroForPesosAndUdis(
                                      List<TrialBalanceEntry> summaryEntries) {
      if (_command.UseNewSectorizationModel &&
          _command.TrialBalanceType == TrialBalanceType.AnaliticoDeCuentas) {

        var summaryEntriesList = summaryEntries.Where(
                                  a => a.Level > 1 && a.Currency.Code == "44" && a.Sector.Code == "00")
                                 .ToList();

        foreach (var entry in summaryEntriesList) {
          var entryWithSummarySector = summaryEntries.FirstOrDefault(
                                                        a => a.Account.Number == entry.Account.Number &&
                                                        a.Ledger.Number == entry.Ledger.Number &&
                                                        a.Currency.Code == "01" &&
                                                        a.Sector.Code == "00" &&
                                                        a.DebtorCreditor == entry.DebtorCreditor
                                                       );

          if (entryWithSummarySector != null) {
            entryWithSummarySector.InitialBalance += entry.InitialBalance;
            entryWithSummarySector.Debit += entry.Debit;
            entryWithSummarySector.Credit += entry.Credit;
            entryWithSummarySector.CurrentBalance += entry.CurrentBalance;
            entryWithSummarySector.AverageBalance += entry.AverageBalance;
          } else {
            entry.IsSummaryForAnalytics = true;
          }
        }
      }
    }


    private void LastChangeDateToSummaryEntries(TrialBalanceEntry entry, StandardAccount currentParent,
                                                 List<TrialBalanceEntry> summaryEntriesList) {
      while (true) {
        var summaryParent = summaryEntriesList.FirstOrDefault(
                                                a => a.Account.Number == currentParent.Number &&
                                                a.Currency.Code == entry.Currency.Code &&
                                                a.Sector.Code == entry.Sector.Code);

        if (summaryParent != null && entry.LastChangeDate > summaryParent.LastChangeDate) {
          summaryParent.LastChangeDate = entry.LastChangeDate;
        }

        if (!currentParent.HasParent) {
          var entryWithoutSector = summaryEntriesList.FirstOrDefault(
                                    a => a.Account.Number == currentParent.Number &&
                                    a.Currency.Code == entry.Currency.Code &&
                                    a.Sector.Code == "00");
          if (entryWithoutSector != null && entry.LastChangeDate > entryWithoutSector.LastChangeDate) {
            entryWithoutSector.LastChangeDate = entry.LastChangeDate;
          }
          break;
        } else {
          currentParent = currentParent.GetParent();
        }
      }
    }


    private List<TrialBalanceEntry> OrderByLedgerAndCurrency(List<TrialBalanceEntry> entries) {
      return entries.OrderBy(a => a.Ledger.Number)
                    .ThenBy(a => a.Currency.Code)
                    .ToList();
    }


    private List<TrialBalanceEntry> OrderingTrialBalance(List<TrialBalanceEntry> entries) {

      if (_command.WithSubledgerAccount && (_command.TrialBalanceType == TrialBalanceType.Balanza ||
          _command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta ||
          _command.TrialBalanceType == TrialBalanceType.AnaliticoDeCuentas)) {

        return entries.Where(a => !a.SubledgerAccountNumber.Contains("undefined"))
                                  .OrderBy(a => a.Ledger.Number)
                                  .ThenBy(a => a.Currency.Code)
                                  .ThenByDescending(a => a.Account.DebtorCreditor)
                                  .ThenBy(a => a.Account.Number)
                                  .ThenBy(a => a.Sector.Code)
                                  .ThenBy(a => a.SubledgerNumberOfDigits)
                                  .ThenBy(a => a.SubledgerAccountNumber)
                                  .ToList();
      } else {
        return entries.OrderBy(a => a.Ledger.Number)
                                  .ThenBy(a => a.Currency.Code)
                                  .ThenByDescending(a => a.Account.DebtorCreditor)
                                  .ThenBy(a => a.Account.Number)
                                  .ThenBy(a => a.Sector.Code)
                                  .ThenBy(a => a.SubledgerAccountNumber)
                                  .ToList();
      }
    }


    private void SetLastChangeDateToSubledgerAccounts(TrialBalanceEntry entry,
                                                      List<TrialBalanceEntry> trialBalance) {

      var accountsList = trialBalance.Where(a => a.Account.Number == entry.Account.Number &&
                                                 a.Currency.Code == entry.Currency.Code &&
                                                 a.Sector.Code == entry.Sector.Code).ToList();
      foreach (var account in accountsList) {
        if (entry.LastChangeDate > account.LastChangeDate) {
          account.LastChangeDate = entry.LastChangeDate;
        }
      }

    }


    private void SummaryByDebtorCreditorEntries(EmpiriaHashTable<TrialBalanceEntry> summaryEntries,
                                                TrialBalanceEntry balanceEntry,
                                                StandardAccount targetAccount, Sector targetSector,
                                                TrialBalanceItemType itemType) {

      TrialBalanceEntry entry = TrialBalanceMapper.MapToTrialBalanceEntry(balanceEntry);

      if (itemType == TrialBalanceItemType.BalanceTotalDebtor) {
        entry.GroupName = "TOTAL DEUDORAS " + entry.Currency.FullName;
      } else if (itemType == TrialBalanceItemType.BalanceTotalCreditor) {
        entry.GroupName = "TOTAL ACREEDORAS " + entry.Currency.FullName;
      }

      string hash = string.Empty;
      if ((_command.TrialBalanceType == TrialBalanceType.Balanza ||
           _command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta) &&
          ((_command.WithSubledgerAccount && _command.ShowCascadeBalances) ||
             _command.ShowCascadeBalances)) {

        hash = $"{entry.Ledger.Id}||{entry.Currency.Id}||{entry.GroupName}";

      } else {
        hash = $"{entry.GroupName}||{entry.Currency.Id}";
      }
      GenerateOrIncreaseEntries(summaryEntries, entry, targetAccount, targetSector, itemType, hash);
    }


    private void SummaryByGroupEntries(EmpiriaHashTable<TrialBalanceEntry> summaryEntries,
                                       TrialBalanceEntry balanceEntry) {

      TrialBalanceEntry groupEntry = TrialBalanceMapper.MapToTrialBalanceEntry(balanceEntry);

      groupEntry.GroupName = $"TOTAL GRUPO {balanceEntry.Account.GroupNumber}";
      groupEntry.GroupNumber = balanceEntry.Account.GroupNumber;
      groupEntry.DebtorCreditor = balanceEntry.Account.DebtorCreditor;
      groupEntry.Account = StandardAccount.Empty;
      groupEntry.Sector = Sector.Empty;

      string hash;

      if (balanceEntry.DebtorCreditor == DebtorCreditorType.Deudora) {

        if ((_command.TrialBalanceType == TrialBalanceType.Balanza ||
             _command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta) &&
            ((_command.WithSubledgerAccount && _command.ShowCascadeBalances) ||
             _command.ShowCascadeBalances)) {
          hash = $"{balanceEntry.Ledger.Id}||{groupEntry.DebtorCreditor}||{groupEntry.Currency.Id}||{groupEntry.GroupNumber}";
        } else {
          hash = $"{groupEntry.DebtorCreditor}||{groupEntry.Currency.Id}||{groupEntry.GroupNumber}";
        }


        GenerateOrIncreaseEntries(summaryEntries, groupEntry, StandardAccount.Empty, Sector.Empty,
                                  TrialBalanceItemType.BalanceTotalGroupDebtor, hash);

      } else if (balanceEntry.DebtorCreditor == DebtorCreditorType.Acreedora) {

        if ((_command.TrialBalanceType == TrialBalanceType.Balanza ||
             _command.TrialBalanceType == TrialBalanceType.SaldosPorCuenta) &&
            ((_command.WithSubledgerAccount && _command.ShowCascadeBalances) ||
             _command.ShowCascadeBalances)) {
          hash = $"{balanceEntry.Ledger.Id}||{groupEntry.DebtorCreditor}||{groupEntry.Currency.Id}||{groupEntry.GroupNumber}";
        } else {
          hash = $"{groupEntry.DebtorCreditor}||{groupEntry.Currency.Id}||{groupEntry.GroupNumber}";
        }

        GenerateOrIncreaseEntries(summaryEntries, groupEntry, StandardAccount.Empty, Sector.Empty,
                                  TrialBalanceItemType.BalanceTotalGroupCreditor, hash);
      }
    }


    private void GenerateOrIncreaseEntries(EmpiriaHashTable<TrialBalanceEntry> summaryEntries,
                                           TrialBalanceEntry entry,
                                           StandardAccount targetAccount, Sector targetSector,
                                           TrialBalanceItemType itemType, string hash) {

      TrialBalanceEntry summaryEntry;

      summaryEntries.TryGetValue(hash, out summaryEntry);

      if (summaryEntry == null) {

        summaryEntry = new TrialBalanceEntry {
          Ledger = entry.Ledger,
          Currency = entry.Currency,
          Sector = targetSector,
          Account = targetAccount,
          ItemType = itemType,
          GroupNumber = entry.GroupNumber,
          GroupName = entry.GroupName,
          DebtorCreditor = entry.DebtorCreditor,
          SubledgerAccountIdParent = entry.SubledgerAccountIdParent,
          LastChangeDate = entry.LastChangeDate
        };

        summaryEntry.Sum(entry);

        summaryEntries.Insert(hash, summaryEntry);

      } else {

        summaryEntry.Sum(entry);
      }
    }

    #endregion Private methods

  }  // class TrialBalanceHelper

}  // namespace Empiria.FinancialAccounting.BalanceEngine
