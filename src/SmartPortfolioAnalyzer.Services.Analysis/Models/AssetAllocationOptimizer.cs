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

public class AssetAllocationOptimizer
{
    private readonly Dictionary<string, decimal> _defaultAllocation;
    private readonly Dictionary<string, decimal> _riskFactors;
    private readonly Dictionary<string, decimal> _expectedReturns;
    private readonly Dictionary<string, decimal> _volatilityFactors;

    public AssetAllocationOptimizer()
    {
        _defaultAllocation = new Dictionary<string, decimal>
        {
            { "Equities", 0.6m },
            { "Bonds", 0.3m },
            { "Cash", 0.1m }
        };

        _riskFactors = new Dictionary<string, decimal>
        {
            { "Equities", 1.2m },
            { "Bonds", 0.8m },
            { "Cash", 0.2m }
        };

        _expectedReturns = new Dictionary<string, decimal>
        {
            { "Equities", 0.08m },
            { "Bonds", 0.04m },
            { "Cash", 0.01m }
        };

        _volatilityFactors = new Dictionary<string, decimal>
        {
            { "Equities", 0.15m },
            { "Bonds", 0.05m },
            { "Cash", 0.01m }
        };
    }

    public AssetAllocationResult OptimizeAllocation(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions,
        RiskProfile riskProfile)
    {
        var currentMetrics = CalculateCurrentMetrics(portfolio, marketPrediction, securityPredictions);
        var optimalAllocation = CalculateOptimalAllocation(currentMetrics, riskProfile);
        var rebalancingPlan = GenerateRebalancingPlan(portfolio, optimalAllocation);

        return new AssetAllocationResult
        {
            CurrentMetrics = currentMetrics,
            OptimalAllocation = optimalAllocation,
            RebalancingPlan = rebalancingPlan,
            PerformanceMetrics = CalculatePerformanceMetrics(optimalAllocation),
            RiskMetrics = CalculateRiskMetrics(optimalAllocation)
        };
    }

    private AssetAllocationMetrics CalculateCurrentMetrics(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions)
    {
        var currentAllocation = CalculateCurrentAllocation(portfolio);
        var marketMetrics = CalculateMarketMetrics(marketPrediction);
        var securityMetrics = CalculateSecurityMetrics(securityPredictions);
        var portfolioMetrics = CalculatePortfolioMetrics(portfolio);

        return new AssetAllocationMetrics
        {
            CurrentAllocation = currentAllocation,
            MarketMetrics = marketMetrics,
            SecurityMetrics = securityMetrics,
            PortfolioMetrics = portfolioMetrics
        };
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

    private Dictionary<string, decimal> CalculateOptimalAllocation(
        AssetAllocationMetrics metrics,
        RiskProfile riskProfile)
    {
        var optimalAllocation = new Dictionary<string, decimal>();
        var totalRisk = metrics.PortfolioMetrics.TotalRisk;
        var riskTolerance = riskProfile.RiskTolerance;

        foreach (var assetClass in _defaultAllocation.Keys)
        {
            var baseWeight = _defaultAllocation[assetClass];
            var riskFactor = _riskFactors[assetClass];
            var expectedReturn = _expectedReturns[assetClass];

            var adjustedWeight = baseWeight * 
                (1 + (riskTolerance - riskFactor) * 0.1m) * 
                (1 + (expectedReturn - totalRisk) * 0.1m);

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

        return optimalAllocation;
    }

    private RebalancingPlan GenerateRebalancingPlan(
        Portfolio portfolio,
        Dictionary<string, decimal> optimalAllocation)
    {
        var currentAllocation = CalculateCurrentAllocation(portfolio);
        var rebalancingPlan = new RebalancingPlan();

        foreach (var assetClass in optimalAllocation.Keys)
        {
            var currentWeight = currentAllocation.TryGetValue(assetClass, out var weight) ? weight : 0;
            var targetWeight = optimalAllocation[assetClass];
            var weightDifference = targetWeight - currentWeight;

            if (Math.Abs(weightDifference) > 0.01m) // Minimum threshold for rebalancing
            {
                rebalancingPlan.Add(new RebalancingAction
                {
                    AssetClass = assetClass,
                    CurrentWeight = currentWeight,
                    TargetWeight = targetWeight,
                    WeightDifference = weightDifference,
                    Type = weightDifference > 0 ? RebalancingActionType.Buy : RebalancingActionType.Sell
                });
            }
        }

        return rebalancingPlan;
    }

    private PerformanceMetrics CalculatePerformanceMetrics(
        Dictionary<string, decimal> allocation)
    {
        var expectedReturn = CalculateExpectedReturn(allocation);
        var volatility = CalculatePortfolioVolatility(allocation);
        var sharpeRatio = CalculateSharpeRatio(expectedReturn, volatility);
        var sortinoRatio = CalculateSortinoRatio(expectedReturn, volatility);

        return new PerformanceMetrics
        {
            ExpectedReturn = expectedReturn,
            Volatility = volatility,
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
            totalReturn += allocation[assetClass] * _expectedReturns[assetClass];
        }
        return totalReturn;
    }

    private decimal CalculatePortfolioVolatility(Dictionary<string, decimal> allocation)
    {
        var totalVolatility = 0m;
        foreach (var assetClass in allocation.Keys)
        {
            totalVolatility += allocation[assetClass] * 
                _volatilityFactors[assetClass] * 
                _riskFactors[assetClass];
        }
        return totalVolatility;
    }

    private decimal CalculateSharpeRatio(decimal returnRate, decimal volatility)
    {
        const decimal riskFreeRate = 0.02m;
        return (returnRate - riskFreeRate) / volatility;
    }

    private decimal CalculateSortinoRatio(decimal returnRate, decimal volatility)
    {
        const decimal targetReturn = 0.03m;
        return (returnRate - targetReturn) / volatility;
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
                _volatilityFactors[assetClass];
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
                _volatilityFactors[assetClass];
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

            var correlation = CalculateCorrelation(asset1, asset2);
            correlationSum += correlation;
            count++;
        }

        return count > 0 ? correlationSum / count : 0m;
    }

    private decimal CalculateCorrelation(string asset1, string asset2)
    {
        var risk1 = _riskFactors[asset1];
        var risk2 = _riskFactors[asset2];
        return 1 - Math.Abs(risk1 - risk2) / (risk1 + risk2);
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

    private string GetAssetClass(string symbol)
    {
        // TODO: Implement actual asset class classification
        if (symbol.EndsWith(".BOND")) return "Bonds";
        if (symbol.EndsWith(".CASH")) return "Cash";
        return "Equities";
    }
}
