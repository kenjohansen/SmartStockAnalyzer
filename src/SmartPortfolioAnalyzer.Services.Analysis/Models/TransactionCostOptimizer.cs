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

public class TransactionCostOptimizer
{
    private readonly Dictionary<string, decimal> _defaultCostFactors;
    private readonly Dictionary<string, decimal> _slippageFactors;
    private readonly Dictionary<string, decimal> _marketImpactFactors;
    private readonly decimal _minimumTransactionThreshold = 1000m;
    private readonly decimal _maximumTransactionCostRate = 0.01m; // 1% maximum transaction cost rate

    public TransactionCostOptimizer()
    {
        _defaultCostFactors = new Dictionary<string, decimal>
        {
            { "Equities", 0.001m },
            { "Bonds", 0.0005m },
            { "ETFs", 0.0005m },
            { "Options", 0.002m }
        };

        _slippageFactors = new Dictionary<string, decimal>
        {
            { "HighVolume", 0.0005m },
            { "MediumVolume", 0.001m },
            { "LowVolume", 0.002m }
        };

        _marketImpactFactors = new Dictionary<string, decimal>
        {
            { "HighImpact", 0.001m },
            { "MediumImpact", 0.0005m },
            { "LowImpact", 0.0002m }
        };
    }

    public TransactionCostOptimizationResult OptimizeTransactionCosts(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions,
        TransactionCostProfile costProfile)
    {
        var currentMetrics = CalculateCurrentMetrics(portfolio, marketPrediction, securityPredictions);
        var costAnalysis = AnalyzeTransactionCosts(currentMetrics, costProfile);
        var optimizationPlan = GenerateOptimizationPlan(portfolio, costAnalysis);

        return new TransactionCostOptimizationResult
        {
            CurrentMetrics = currentMetrics,
            CostAnalysis = costAnalysis,
            OptimizationPlan = optimizationPlan,
            CostMetrics = CalculateCostMetrics(optimizationPlan)
        };
    }

    private TransactionCostMetrics CalculateCurrentMetrics(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions)
    {
        var currentAllocation = CalculateCurrentAllocation(portfolio);
        var marketMetrics = CalculateMarketMetrics(marketPrediction);
        var securityMetrics = CalculateSecurityMetrics(securityPredictions);
        var portfolioMetrics = CalculatePortfolioMetrics(portfolio);

        return new TransactionCostMetrics
        {
            CurrentAllocation = currentAllocation,
            MarketMetrics = marketMetrics,
            SecurityMetrics = securityMetrics,
            PortfolioMetrics = portfolioMetrics,
            TransactionCostRate = CalculateTransactionCostRate(portfolio)
        };
    }

    private TransactionCostAnalysis AnalyzeTransactionCosts(
        TransactionCostMetrics metrics,
        TransactionCostProfile costProfile)
    {
        var transactionCosts = CalculateTransactionCosts(metrics, costProfile);
        var slippageCosts = CalculateSlippageCosts(metrics);
        var marketImpactCosts = CalculateMarketImpactCosts(metrics);
        var totalCosts = CalculateTotalTransactionCosts(transactionCosts, slippageCosts, marketImpactCosts);

        return new TransactionCostAnalysis
        {
            TransactionCosts = transactionCosts,
            SlippageCosts = slippageCosts,
            MarketImpactCosts = marketImpactCosts,
            TotalCosts = totalCosts,
            CostEfficiencyScore = CalculateCostEfficiencyScore(totalCosts)
        };
    }

    private TransactionCostOptimizationPlan GenerateOptimizationPlan(
        Portfolio portfolio,
        TransactionCostAnalysis costAnalysis)
    {
        var plan = new TransactionCostOptimizationPlan();
        var currentAllocation = CalculateCurrentAllocation(portfolio);
        var targetAllocation = CalculateTargetAllocation(costAnalysis);

        foreach (var assetClass in targetAllocation.Keys)
        {
            var currentWeight = currentAllocation.TryGetValue(assetClass, out var weight) ? weight : 0;
            var targetWeight = targetAllocation[assetClass];
            var weightDifference = targetWeight - currentWeight;

            if (Math.Abs(weightDifference) > 0.01m) // Minimum threshold
            {
                plan.Add(new TransactionCostOptimizationAction
                {
                    AssetClass = assetClass,
                    CurrentWeight = currentWeight,
                    TargetWeight = targetWeight,
                    WeightDifference = weightDifference,
                    Type = weightDifference > 0 ? TransactionCostOptimizationActionType.Buy : TransactionCostOptimizationActionType.Sell,
                    CostImpact = CalculateCostImpact(assetClass, weightDifference, costAnalysis)
                });
            }
        }

        return plan;
    }

    private Dictionary<string, decimal> CalculateTargetAllocation(TransactionCostAnalysis analysis)
    {
        var allocation = new Dictionary<string, decimal>();
        var totalCost = analysis.TotalCosts;
        var adjustedWeight = 1m - totalCost;

        foreach (var assetClass in analysis.TransactionCosts.Keys)
        {
            var baseWeight = CalculateBaseWeight(assetClass, analysis);
            var costFactor = CalculateCostFactor(assetClass, analysis);
            var adjustedWeight = baseWeight * costFactor;

            allocation[assetClass] = Math.Max(
                Math.Min(adjustedWeight, 1),
                0
            );
        }

        // Normalize weights to sum to 1
        var totalWeight = allocation.Values.Sum();
        foreach (var assetClass in allocation.Keys)
        {
            allocation[assetClass] /= totalWeight;
        }

        return allocation;
    }

    private decimal CalculateBaseWeight(string assetClass, TransactionCostAnalysis analysis)
    {
        var baseWeight = 0.1m; // Default base weight
        var costRate = _defaultCostFactors.TryGetValue(assetClass, out var rate) ? rate : _defaultCostFactors["Equities"];

        return baseWeight * (1 - costRate);
    }

    private decimal CalculateCostFactor(string assetClass, TransactionCostAnalysis analysis)
    {
        var costRate = _defaultCostFactors.TryGetValue(assetClass, out var rate) ? rate : _defaultCostFactors["Equities"];
        var slippage = _slippageFactors["LowVolume"];
        var marketImpact = _marketImpactFactors["LowImpact"];

        return (1 - costRate) * 
            (1 - slippage) * 
            (1 - marketImpact);
    }

    private decimal CalculateCostImpact(
        string assetClass,
        decimal weightDifference,
        TransactionCostAnalysis analysis)
    {
        var costRate = _defaultCostFactors.TryGetValue(assetClass, out var rate) ? rate : _defaultCostFactors["Equities"];
        var slippage = _slippageFactors["LowVolume"];
        var marketImpact = _marketImpactFactors["LowImpact"];

        return Math.Abs(weightDifference) * 
            (costRate + slippage + marketImpact);
    }

    private TransactionCostMetrics CalculateCostMetrics(TransactionCostOptimizationPlan plan)
    {
        var totalCost = 0m;
        var transactionCosts = 0m;
        var slippageCosts = 0m;
        var marketImpactCosts = 0m;
        var costEfficiencyScore = 0m;

        foreach (var action in plan.Actions)
        {
            totalCost += action.CostImpact;
            if (action.Type == TransactionCostOptimizationActionType.Buy)
            {
                transactionCosts += action.CostImpact * _defaultCostFactors["Equities"];
                slippageCosts += action.CostImpact * _slippageFactors["LowVolume"];
                marketImpactCosts += action.CostImpact * _marketImpactFactors["LowImpact"];
            }
        }

        // Calculate cost efficiency score
        costEfficiencyScore = 1 - (totalCost / plan.TotalValue);

        return new TransactionCostMetrics
        {
            TotalCost = totalCost,
            TransactionCosts = transactionCosts,
            SlippageCosts = slippageCosts,
            MarketImpactCosts = marketImpactCosts,
            CostEfficiencyScore = costEfficiencyScore
        };
    }

    private decimal CalculateTransactionCosts(
        TransactionCostMetrics metrics,
        TransactionCostProfile costProfile)
    {
        var totalCost = 0m;
        foreach (var position in metrics.PortfolioMetrics.Positions)
        {
            var costRate = _defaultCostFactors[GetAssetClass(position.Symbol)];
            var transactionCost = position.CurrentValue * costRate;
            totalCost += transactionCost;
        }
        return totalCost;
    }

    private decimal CalculateSlippageCosts(TransactionCostMetrics metrics)
    {
        var totalSlippage = 0m;
        foreach (var position in metrics.PortfolioMetrics.Positions)
        {
            var slippageRate = _slippageFactors[GetVolumeCategory(position.Symbol)];
            var slippageCost = position.CurrentValue * slippageRate;
            totalSlippage += slippageCost;
        }
        return totalSlippage;
    }

    private decimal CalculateMarketImpactCosts(TransactionCostMetrics metrics)
    {
        var totalImpact = 0m;
        foreach (var position in metrics.PortfolioMetrics.Positions)
        {
            var impactRate = _marketImpactFactors[GetImpactCategory(position.Symbol)];
            var impactCost = position.CurrentValue * impactRate;
            totalImpact += impactCost;
        }
        return totalImpact;
    }

    private decimal CalculateTotalTransactionCosts(
        decimal transactionCosts,
        decimal slippageCosts,
        decimal marketImpactCosts)
    {
        return transactionCosts + slippageCosts + marketImpactCosts;
    }

    private decimal CalculateCostEfficiencyScore(decimal totalCosts)
    {
        return 1 - (totalCosts / _maximumTransactionCostRate);
    }

    private decimal CalculateTransactionCostRate(Portfolio portfolio)
    {
        var totalCost = 0m;
        var totalValue = portfolio.TotalValue;

        foreach (var position in portfolio.Positions)
        {
            var costRate = _defaultCostFactors[GetAssetClass(position.Symbol)];
            var transactionCost = position.CurrentValue * costRate;
            totalCost += transactionCost;
        }

        return totalCost / totalValue;
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
        if (symbol.EndsWith(".ETF")) return "ETFs";
        if (symbol.EndsWith(".OPT")) return "Options";
        return "Equities";
    }

    private string GetVolumeCategory(string symbol)
    {
        // TODO: Implement actual volume classification
        if (symbol.Contains("HIGHVOL")) return "HighVolume";
        if (symbol.Contains("MIDVOL")) return "MediumVolume";
        return "LowVolume";
    }

    private string GetImpactCategory(string symbol)
    {
        // TODO: Implement actual impact classification
        if (symbol.Contains("HIGHIMP")) return "HighImpact";
        if (symbol.Contains("MIDIMP")) return "MediumImpact";
        return "LowImpact";
    }
}
