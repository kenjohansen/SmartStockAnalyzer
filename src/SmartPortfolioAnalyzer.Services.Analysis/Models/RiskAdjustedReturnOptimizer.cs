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

public class RiskAdjustedReturnOptimizer
{
    private readonly Dictionary<string, decimal> _riskFactors;
    private readonly Dictionary<string, decimal> _returnFactors;
    private readonly Dictionary<string, decimal> _correlationFactors;
    private readonly decimal _defaultRiskTolerance = 0.1m; // 10% default risk tolerance
    private readonly decimal _minimumReturnThreshold = 0.05m; // 5% minimum return threshold
    private readonly decimal _maximumRiskThreshold = 0.2m; // 20% maximum risk threshold

    public RiskAdjustedReturnOptimizer()
    {
        // Initialize default factors
        _riskFactors = new Dictionary<string, decimal>
        {
            { "Equities", 1.2m },
            { "Bonds", 0.8m },
            { "Cash", 0.2m }
        };

        _returnFactors = new Dictionary<string, decimal>
        {
            { "Equities", 0.08m },
            { "Bonds", 0.04m },
            { "Cash", 0.01m }
        };

        _correlationFactors = new Dictionary<string, Dictionary<string, decimal>>
        {
            { "Equities", new Dictionary<string, decimal>
                {
                    { "Equities", 0.8m },
                    { "Bonds", 0.3m },
                    { "Cash", 0.1m }
                }
            },
            { "Bonds", new Dictionary<string, decimal>
                {
                    { "Equities", 0.3m },
                    { "Bonds", 0.6m },
                    { "Cash", 0.2m }
                }
            },
            { "Cash", new Dictionary<string, decimal>
                {
                    { "Equities", 0.1m },
                    { "Bonds", 0.2m },
                    { "Cash", 0.4m }
                }
            }
        };
    }

    public RiskAdjustedReturnResult Optimize(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions,
        RiskProfile riskProfile)
    {
        var currentMetrics = CalculateCurrentMetrics(portfolio, marketPrediction, securityPredictions);
        var optimalAllocation = CalculateOptimalAllocation(currentMetrics, riskProfile);
        var optimizationPlan = GenerateOptimizationPlan(portfolio, optimalAllocation);

        return new RiskAdjustedReturnResult
        {
            CurrentMetrics = currentMetrics,
            OptimalAllocation = optimalAllocation,
            OptimizationPlan = optimizationPlan,
            PerformanceMetrics = CalculatePerformanceMetrics(optimalAllocation),
            RiskMetrics = CalculateRiskMetrics(optimalAllocation)
        };
    }

    private RiskAdjustedReturnMetrics CalculateCurrentMetrics(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions)
    {
        var currentAllocation = CalculateCurrentAllocation(portfolio);
        var marketMetrics = CalculateMarketMetrics(marketPrediction);
        var securityMetrics = CalculateSecurityMetrics(securityPredictions);
        var portfolioMetrics = CalculatePortfolioMetrics(portfolio);

        return new RiskAdjustedReturnMetrics
        {
            CurrentAllocation = currentAllocation,
            MarketMetrics = marketMetrics,
            SecurityMetrics = securityMetrics,
            PortfolioMetrics = portfolioMetrics,
            SharpeRatio = CalculateSharpeRatio(portfolioMetrics.ExpectedReturn, portfolioMetrics.Volatility),
            SortinoRatio = CalculateSortinoRatio(portfolioMetrics.ExpectedReturn, portfolioMetrics.DownsideVolatility)
        };
    }

    private Dictionary<string, decimal> CalculateOptimalAllocation(
        RiskAdjustedReturnMetrics metrics,
        RiskProfile riskProfile)
    {
        var optimalAllocation = new Dictionary<string, decimal>();
        var totalRisk = metrics.PortfolioMetrics.TotalRisk;
        var riskTolerance = riskProfile.RiskTolerance;

        foreach (var assetClass in _riskFactors.Keys)
        {
            var baseWeight = CalculateBaseWeight(assetClass, metrics);
            var riskFactor = _riskFactors[assetClass];
            var returnFactor = _returnFactors[assetClass];

            var adjustedWeight = baseWeight * 
                (1 + (riskTolerance - riskFactor) * 0.1m) * 
                (1 + (returnFactor - totalRisk) * 0.1m);

            optimalAllocation[assetClass] = Math.Max(
                Math.Min(adjustedWeight, 1),
                0
            );
        }

        // Normalize weights to sum to 1
        var totalWeight = optimalAllocation.Values.Sum();
        foreach (var assetClass in optimalAllocation.Keys)
        {
            optimalAllocation[assetClass] /= totalWeight;
        }

        // Ensure risk constraints are met
        var riskMetrics = CalculateRiskMetrics(optimalAllocation);
        if (riskMetrics.TotalRisk > _maximumRiskThreshold)
        {
            optimalAllocation = AdjustForRiskConstraints(optimalAllocation);
        }

        return optimalAllocation;
    }

    private Dictionary<string, decimal> CalculateBaseWeight(
        string assetClass,
        RiskAdjustedReturnMetrics metrics)
    {
        var baseWeight = _defaultRiskTolerance;
        var marketVolatility = metrics.MarketMetrics.Volatility;
        var assetVolatility = _riskFactors[assetClass];

        return baseWeight * 
            (1 + (metrics.PortfolioMetrics.ExpectedReturn - marketVolatility) * 0.1m) * 
            (1 - (assetVolatility - _defaultRiskTolerance) * 0.1m);
    }

    private Dictionary<string, decimal> AdjustForRiskConstraints(
        Dictionary<string, decimal> allocation)
    {
        var adjustedAllocation = new Dictionary<string, decimal>(allocation);
        var excessRisk = CalculateRiskMetrics(allocation).TotalRisk - _maximumRiskThreshold;

        while (excessRisk > 0)
        {
            var highestRiskAsset = adjustedAllocation
                .OrderByDescending(a => _riskFactors[a.Key])
                .First().Key;

            var reduction = Math.Min(
                adjustedAllocation[highestRiskAsset] * 0.1m,
                excessRisk / _riskFactors[highestRiskAsset]
            );

            adjustedAllocation[highestRiskAsset] -= reduction;
            excessRisk -= reduction * _riskFactors[highestRiskAsset];

            // Normalize weights
            var totalWeight = adjustedAllocation.Values.Sum();
            foreach (var asset in adjustedAllocation.Keys)
            {
                adjustedAllocation[asset] /= totalWeight;
            }
        }

        return adjustedAllocation;
    }

    private OptimizationPlan GenerateOptimizationPlan(
        Portfolio portfolio,
        Dictionary<string, decimal> optimalAllocation)
    {
        var currentAllocation = CalculateCurrentAllocation(portfolio);
        var plan = new OptimizationPlan();

        foreach (var assetClass in optimalAllocation.Keys)
        {
            var currentWeight = currentAllocation.TryGetValue(assetClass, out var weight) ? weight : 0;
            var targetWeight = optimalAllocation[assetClass];
            var weightDifference = targetWeight - currentWeight;

            if (Math.Abs(weightDifference) > 0.01m) // Minimum threshold
            {
                plan.Add(new OptimizationAction
                {
                    AssetClass = assetClass,
                    CurrentWeight = currentWeight,
                    TargetWeight = targetWeight,
                    WeightDifference = weightDifference,
                    Type = weightDifference > 0 ? OptimizationActionType.Buy : OptimizationActionType.Sell
                });
            }
        }

        return plan;
    }

    private PerformanceMetrics CalculatePerformanceMetrics(
        Dictionary<string, decimal> allocation)
    {
        var expectedReturn = CalculateExpectedReturn(allocation);
        var volatility = CalculatePortfolioVolatility(allocation);
        var downsideVolatility = CalculateDownsideVolatility(allocation);
        var sharpeRatio = CalculateSharpeRatio(expectedReturn, volatility);
        var sortinoRatio = CalculateSortinoRatio(expectedReturn, downsideVolatility);

        return new PerformanceMetrics
        {
            ExpectedReturn = expectedReturn,
            Volatility = volatility,
            DownsideVolatility = downsideVolatility,
            SharpeRatio = sharpeRatio,
            SortinoRatio = sortinoRatio
        };
    }

    private RiskMetrics CalculateRiskMetrics(
        Dictionary<string, decimal> allocation)
    {
        var totalRisk = CalculateTotalRisk(allocation);
        var marketRisk = CalculateMarketRisk(allocation);
        var assetClassRisk = CalculateAssetClassRisk(allocation);
        var correlationRisk = CalculateCorrelationRisk(allocation);

        return new RiskMetrics
        {
            TotalRisk = totalRisk,
            MarketRisk = marketRisk,
            AssetClassRisk = assetClassRisk,
            CorrelationRisk = correlationRisk
        };
    }

    private decimal CalculateExpectedReturn(Dictionary<string, decimal> allocation)
    {
        var totalReturn = 0m;
        foreach (var assetClass in allocation.Keys)
        {
            totalReturn += allocation[assetClass] * _returnFactors[assetClass];
        }
        return totalReturn;
    }

    private decimal CalculatePortfolioVolatility(Dictionary<string, decimal> allocation)
    {
        var totalVolatility = 0m;
        foreach (var assetClass in allocation.Keys)
        {
            totalVolatility += allocation[assetClass] * 
                _riskFactors[assetClass] * 
                _returnFactors[assetClass];
        }
        return totalVolatility;
    }

    private decimal CalculateDownsideVolatility(Dictionary<string, decimal> allocation)
    {
        var downsideVolatility = 0m;
        var targetReturn = CalculateExpectedReturn(allocation);

        foreach (var assetClass in allocation.Keys)
        {
            var assetReturn = _returnFactors[assetClass];
            if (assetReturn < targetReturn)
            {
                downsideVolatility += allocation[assetClass] * 
                    Math.Pow(targetReturn - assetReturn, 2);
            }
        }

        return (decimal)Math.Sqrt(downsideVolatility);
    }

    private decimal CalculateSharpeRatio(decimal returnRate, decimal volatility)
    {
        const decimal riskFreeRate = 0.02m;
        return (returnRate - riskFreeRate) / volatility;
    }

    private decimal CalculateSortinoRatio(decimal returnRate, decimal downsideVolatility)
    {
        const decimal targetReturn = 0.03m;
        return (returnRate - targetReturn) / downsideVolatility;
    }

    private decimal CalculateTotalRisk(Dictionary<string, decimal> allocation)
    {
        var totalRisk = 0m;
        foreach (var assetClass in allocation.Keys)
        {
            totalRisk += allocation[assetClass] * _riskFactors[assetClass];
        }
        return totalRisk;
    }

    private decimal CalculateMarketRisk(Dictionary<string, decimal> allocation)
    {
        var totalRisk = 0m;
        foreach (var assetClass in allocation.Keys)
        {
            totalRisk += allocation[assetClass] * 
                _riskFactors[assetClass] * 
                _returnFactors[assetClass];
        }
        return totalRisk;
    }

    private Dictionary<string, decimal> CalculateAssetClassRisk(
        Dictionary<string, decimal> allocation)
    {
        var assetClassRisk = new Dictionary<string, decimal>();
        foreach (var assetClass in allocation.Keys)
        {
            assetClassRisk[assetClass] = allocation[assetClass] * 
                _riskFactors[assetClass] * 
                _returnFactors[assetClass];
        }
        return assetClassRisk;
    }

    private decimal CalculateCorrelationRisk(Dictionary<string, decimal> allocation)
    {
        var correlationSum = 0m;
        var count = 0;

        foreach (var pair in GetAssetClassPairs(allocation.Keys))
        {
            var asset1 = pair.Item1;
            var asset2 = pair.Item2;

            var correlation = _correlationFactors[asset1][asset2];
            correlationSum += correlation;
            count++;
        }

        return count > 0 ? correlationSum / count : 0m;
    }

    private IEnumerable<(string, string)> GetAssetClassPairs(IEnumerable<string> assetClasses)
    {
        var list = assetClasses.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                yield return (list[i], list[j]);
            }
        }
    }

    private Dictionary<string, decimal> CalculateCurrentAllocation(Portfolio portfolio)
    {
        var allocation = new Dictionary<string, decimal>();
        var totalValue = portfolio.Positions.Sum(p => p.CurrentValue);

        foreach (var position in portfolio.Positions)
        {
            var assetClass = GetAssetClass(position.Symbol);
            if (!allocation.ContainsKey(assetClass))
            {
                allocation[assetClass] = 0;
            }
            allocation[assetClass] += position.CurrentValue / totalValue;
        }

        return allocation;
    }

    private string GetAssetClass(string symbol)
    {
        // TODO: Implement actual asset class classification
        if (symbol.EndsWith(".BOND")) return "Bonds";
        if (symbol.EndsWith(".CASH")) return "Cash";
        return "Equities";
    }
}
