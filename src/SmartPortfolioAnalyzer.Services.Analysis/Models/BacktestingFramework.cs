/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using SmartPortfolioAnalyzer.Core.Models;
using SmartPortfolioAnalyzer.Services.Analysis.Interfaces;

namespace SmartPortfolioAnalyzer.Services.Analysis.Models;

public class BacktestingFramework
{
    private readonly Dictionary<string, decimal> _initialCapital;
    private readonly Dictionary<string, decimal> _transactionCosts;
    private readonly Dictionary<string, decimal> _slippage;
    private readonly Dictionary<string, decimal> _commissionRates;

    public BacktestingFramework()
    {
        _initialCapital = new Dictionary<string, decimal>
        {
            { "USD", 100000m },
            { "EUR", 80000m },
            { "GBP", 70000m }
        };

        _transactionCosts = new Dictionary<string, decimal>
        {
            { "Stock", 0.001m },
            { "ETF", 0.0005m },
            { "Index", 0.0001m }
        };

        _slippage = new Dictionary<string, decimal>
        {
            { "HighVolume", 0.0005m },
            { "MediumVolume", 0.001m },
            { "LowVolume", 0.002m }
        };

        _commissionRates = new Dictionary<string, decimal>
        {
            { "Stock", 0.001m },
            { "ETF", 0.0005m },
            { "Index", 0.0001m }
        };
    }

    public BacktestResult RunBacktest(
        IEnumerable<BacktestScenario> scenarios,
        DateTime startDate,
        DateTime endDate)
    {
        var results = new List<ScenarioResult>();

        foreach (var scenario in scenarios)
        {
            var scenarioResult = RunScenarioBacktest(scenario, startDate, endDate);
            results.Add(scenarioResult);
        }

        return new BacktestResult
        {
            Scenarios = results,
            OverallPerformance = CalculateOverallPerformance(results),
            RiskMetrics = CalculateRiskMetrics(results),
            PerformanceMetrics = CalculatePerformanceMetrics(results)
        };
    }

    private ScenarioResult RunScenarioBacktest(
        BacktestScenario scenario,
        DateTime startDate,
        DateTime endDate)
    {
        var portfolio = InitializePortfolio(scenario.InitialCapital);
        var transactions = new List<Transaction>();
        var performance = new List<PerformanceMetric>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var marketData = GetMarketData(date);
            var economicContext = GetEconomicContext(date);

            // Run predictions
            var marketPrediction = RunMarketPrediction(scenario, marketData, economicContext);
            var securityPredictions = RunSecurityPredictions(scenario, marketData);
            var portfolioPrediction = RunPortfolioPrediction(scenario, portfolio, marketPrediction);

            // Execute trades based on predictions
            var trades = GenerateTrades(portfolio, securityPredictions, scenario.RiskProfile);
            ExecuteTrades(trades, marketData, transactions);

            // Update portfolio
            UpdatePortfolio(portfolio, marketData, transactions);

            // Calculate performance
            var dailyPerformance = CalculateDailyPerformance(portfolio, marketData);
            performance.Add(dailyPerformance);
        }

        return new ScenarioResult
        {
            Scenario = scenario,
            Transactions = transactions,
            Performance = performance,
            FinalPortfolio = portfolio
        };
    }

    private Portfolio InitializePortfolio(decimal initialCapital)
    {
        return new Portfolio
        {
            InitialCapital = initialCapital,
            CurrentValue = initialCapital,
            Positions = new List<PortfolioPosition>(),
            Transactions = new List<Transaction>()
        };
    }

    private MarketData GetMarketData(DateTime date)
    {
        // TODO: Implement actual market data retrieval
        return new MarketData
        {
            Date = date,
            Open = 100m,
            High = 105m,
            Low = 95m,
            Close = 102m,
            Volume = 1000000m
        };
    }

    private EconomicContext GetEconomicContext(DateTime date)
    {
        // TODO: Implement actual economic context retrieval
        return new EconomicContext
        {
            Date = date,
            Indicators = new Dictionary<string, decimal>
            {
                { "GDP", 2.5m },
                { "Inflation", 1.5m },
                { "Unemployment", 4.5m }
            }
        };
    }

    private MarketPredictionResult RunMarketPrediction(
        BacktestScenario scenario,
        MarketData marketData,
        EconomicContext economicContext)
    {
        // TODO: Implement actual market prediction
        return new MarketPredictionResult
        {
            Prediction = new MarketPrediction
            {
                Direction = PredictionDirection.Up,
                ExpectedReturn = 0.01m,
                Volatility = 0.05m,
                RiskLevel = 2
            },
            ConfidenceScore = 0.8m,
            TimeHorizon = 1,
            EconomicContext = economicContext
        };
    }

    private IEnumerable<SecurityPredictionResult> RunSecurityPredictions(
        BacktestScenario scenario,
        MarketData marketData)
    {
        var results = new List<SecurityPredictionResult>();

        foreach (var symbol in scenario.TargetSymbols)
        {
            // TODO: Implement actual security prediction
            results.Add(new SecurityPredictionResult
            {
                Symbol = symbol,
                Prediction = new SecurityPrediction
                {
                    ExpectedReturn = 0.01m,
                    Volatility = 0.05m,
                    RiskLevel = 2,
                    TechnicalScore = 0.8m
                },
                ConfidenceScore = 0.8m,
                TimeHorizon = 1
            });
        }

        return results;
    }

    private PortfolioPredictionResult RunPortfolioPrediction(
        BacktestScenario scenario,
        Portfolio portfolio,
        MarketPredictionResult marketPrediction)
    {
        // TODO: Implement actual portfolio prediction
        return new PortfolioPredictionResult
        {
            Portfolio = portfolio,
            Prediction = new PortfolioPrediction
            {
                ExpectedReturn = 0.01m,
                Volatility = 0.05m,
                RiskLevel = 2,
                AssetAllocation = CalculateOptimalAllocation(scenario.TargetSymbols)
            },
            ConfidenceScore = 0.8m,
            TimeHorizon = 1,
            MarketPrediction = marketPrediction
        };
    }

    private IEnumerable<Transaction> GenerateTrades(
        Portfolio portfolio,
        IEnumerable<SecurityPredictionResult> predictions,
        RiskProfile riskProfile)
    {
        var trades = new List<Transaction>();

        foreach (var prediction in predictions)
        {
            var position = portfolio.Positions.FirstOrDefault(p => p.Symbol == prediction.Symbol);
            var targetWeight = CalculateTargetWeight(prediction, riskProfile);

            if (position != null)
            {
                var currentWeight = position.Quantity * prediction.Prediction.ExpectedReturn;
                var weightDifference = targetWeight - currentWeight;

                if (Math.Abs(weightDifference) > 0.01m) // Minimum trade threshold
                {
                    trades.Add(new Transaction
                    {
                        Symbol = prediction.Symbol,
                        Quantity = CalculateTradeQuantity(weightDifference, portfolio),
                        Price = prediction.Prediction.ExpectedReturn,
                        Type = weightDifference > 0 ? TransactionType.Buy : TransactionType.Sell,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            else if (targetWeight > 0.01m)
            {
                trades.Add(new Transaction
                {
                    Symbol = prediction.Symbol,
                    Quantity = CalculateTradeQuantity(targetWeight, portfolio),
                    Price = prediction.Prediction.ExpectedReturn,
                    Type = TransactionType.Buy,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        return trades;
    }

    private void ExecuteTrades(
        IEnumerable<Transaction> trades,
        MarketData marketData,
        List<Transaction> transactionHistory)
    {
        foreach (var trade in trades)
        {
            var totalCost = CalculateTotalCost(trade, marketData);
            trade.TotalCost = totalCost;
            trade.NetAmount = trade.Quantity * trade.Price - totalCost;
            transactionHistory.Add(trade);
        }
    }

    private void UpdatePortfolio(
        Portfolio portfolio,
        MarketData marketData,
        List<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            var position = portfolio.Positions.FirstOrDefault(p => p.Symbol == transaction.Symbol);

            if (transaction.Type == TransactionType.Buy)
            {
                if (position != null)
                {
                    position.Quantity += transaction.Quantity;
                    position.AveragePrice = (position.Quantity * position.AveragePrice + transaction.Quantity * transaction.Price) / (position.Quantity + transaction.Quantity);
                }
                else
                {
                    portfolio.Positions.Add(new PortfolioPosition
                    {
                        Symbol = transaction.Symbol,
                        Quantity = transaction.Quantity,
                        AveragePrice = transaction.Price,
                        EntryDate = transaction.Timestamp
                    });
                }
            }
            else if (transaction.Type == TransactionType.Sell)
            {
                if (position != null)
                {
                    position.Quantity -= transaction.Quantity;
                    if (position.Quantity <= 0)
                    {
                        portfolio.Positions.Remove(position);
                    }
                }
            }
        }

        portfolio.Transactions.AddRange(transactions);
        portfolio.UpdateValue(marketData.Close);
    }

    private PerformanceMetric CalculateDailyPerformance(
        Portfolio portfolio,
        MarketData marketData)
    {
        return new PerformanceMetric
        {
            Date = marketData.Date,
            PortfolioValue = portfolio.CurrentValue,
            Return = portfolio.Return,
            Volatility = portfolio.Volatility,
            SharpeRatio = portfolio.SharpeRatio,
            SortinoRatio = portfolio.SortinoRatio,
            InformationRatio = portfolio.InformationRatio
        };
    }

    private BacktestPerformance CalculateOverallPerformance(
        IEnumerable<ScenarioResult> results)
    {
        var totalReturn = results.Sum(r => r.Performance.Last().Return);
        var totalVolatility = results.Average(r => r.Performance.Last().Volatility);
        var totalSharpe = results.Average(r => r.Performance.Last().SharpeRatio);
        var totalSortino = results.Average(r => r.Performance.Last().SortinoRatio);
        var totalInformation = results.Average(r => r.Performance.Last().InformationRatio);

        return new BacktestPerformance
        {
            TotalReturn = totalReturn,
            AverageVolatility = totalVolatility,
            AverageSharpeRatio = totalSharpe,
            AverageSortinoRatio = totalSortino,
            AverageInformationRatio = totalInformation
        };
    }

    private BacktestRiskMetrics CalculateRiskMetrics(
        IEnumerable<ScenarioResult> results)
    {
        var maxDrawdown = results.Max(r => r.Performance.Max(p => p.Drawdown));
        var averageDrawdown = results.Average(r => r.Performance.Average(p => p.Drawdown));
        var volatility = results.Average(r => r.Performance.Average(p => p.Volatility));
        var valueAtRisk = CalculateValueAtRisk(results);
        var conditionalValueAtRisk = CalculateConditionalValueAtRisk(results);

        return new BacktestRiskMetrics
        {
            MaximumDrawdown = maxDrawdown,
            AverageDrawdown = averageDrawdown,
            AverageVolatility = volatility,
            ValueAtRisk = valueAtRisk,
            ConditionalValueAtRisk = conditionalValueAtRisk
        };
    }

    private BacktestPerformanceMetrics CalculatePerformanceMetrics(
        IEnumerable<ScenarioResult> results)
    {
        var totalReturn = results.Sum(r => r.Performance.Last().Return);
        var sharpeRatio = results.Average(r => r.Performance.Last().SharpeRatio);
        var sortinoRatio = results.Average(r => r.Performance.Last().SortinoRatio);
        var informationRatio = results.Average(r => r.Performance.Last().InformationRatio);
        var winRate = CalculateWinRate(results);
        var profitFactor = CalculateProfitFactor(results);

        return new BacktestPerformanceMetrics
        {
            TotalReturn = totalReturn,
            AverageSharpeRatio = sharpeRatio,
            AverageSortinoRatio = sortinoRatio,
            AverageInformationRatio = informationRatio,
            WinRate = winRate,
            ProfitFactor = profitFactor
        };
    }

    private decimal CalculateTargetWeight(
        SecurityPredictionResult prediction,
        RiskProfile riskProfile)
    {
        var baseWeight = prediction.ConfidenceScore * prediction.Prediction.TechnicalScore;
        var riskAdjustment = 1 - riskProfile.RiskTolerance / 100m;
        
        return baseWeight * riskAdjustment;
    }

    private decimal CalculateTradeQuantity(
        decimal targetWeight,
        Portfolio portfolio)
    {
        return targetWeight * portfolio.CurrentValue / portfolio.MarketValue;
    }

    private decimal CalculateTotalCost(
        Transaction transaction,
        MarketData marketData)
    {
        var transactionCost = transaction.Quantity * _transactionCosts["Stock"];
        var slippage = transaction.Quantity * _slippage["HighVolume"];
        var commission = transaction.Quantity * _commissionRates["Stock"];
        return transactionCost + slippage + commission;
    }

    private Dictionary<string, decimal> CalculateOptimalAllocation(
        IEnumerable<string> symbols)
    {
        var allocation = new Dictionary<string, decimal>();
        var total = symbols.Count();
        var baseWeight = 1m / total;

        foreach (var symbol in symbols)
        {
            allocation[symbol] = baseWeight;
        }

        return allocation;
    }

    private decimal CalculateValueAtRisk(
        IEnumerable<ScenarioResult> results)
    {
        var returns = results.SelectMany(r => r.Performance.Select(p => p.Return)).OrderByDescending(r => r);
        var threshold = (int)(returns.Count() * 0.05m); // 5% VaR
        return returns.ElementAt(threshold);
    }

    private decimal CalculateConditionalValueAtRisk(
        IEnumerable<ScenarioResult> results)
    {
        var returns = results.SelectMany(r => r.Performance.Select(p => p.Return)).OrderByDescending(r => r);
        var threshold = (int)(returns.Count() * 0.05m); // 5% CVaR
        return returns.Take(threshold).Average();
    }

    private decimal CalculateWinRate(
        IEnumerable<ScenarioResult> results)
    {
        var totalTrades = results.Sum(r => r.Transactions.Count);
        var winningTrades = results.Sum(r => r.Transactions.Count(t => t.NetAmount > 0));
        return totalTrades > 0 ? winningTrades / totalTrades : 0;
    }

    private decimal CalculateProfitFactor(
        IEnumerable<ScenarioResult> results)
    {
        var totalProfit = results.Sum(r => r.Transactions.Where(t => t.NetAmount > 0).Sum(t => t.NetAmount));
        var totalLoss = results.Sum(r => r.Transactions.Where(t => t.NetAmount < 0).Sum(t => t.NetAmount));
        return totalLoss != 0 ? totalProfit / Math.Abs(totalLoss) : 0;
    }
}
