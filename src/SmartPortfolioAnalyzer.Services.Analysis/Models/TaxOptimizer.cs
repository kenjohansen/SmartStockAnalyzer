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

public class TaxOptimizer
{
    private readonly Dictionary<string, decimal> _taxRates;
    private readonly Dictionary<string, decimal> _holdingPeriods;
    private readonly Dictionary<string, decimal> _washSalePeriods;
    private readonly decimal _defaultTaxRate = 0.15m; // 15% default capital gains tax rate
    private readonly decimal _shortTermTaxRate = 0.25m; // 25% short-term capital gains tax rate
    private readonly decimal _longTermTaxRate = 0.15m; // 15% long-term capital gains tax rate

    public TaxOptimizer()
    {
        _taxRates = new Dictionary<string, decimal>
        {
            { "ShortTerm", _shortTermTaxRate },
            { "LongTerm", _longTermTaxRate },
            { "Dividends", 0.15m },
            { "Interest", 0.25m }
        };

        _holdingPeriods = new Dictionary<string, decimal>
        {
            { "ShortTerm", 1 }, // Less than 1 year
            { "LongTerm", 12 } // More than 1 year
        };

        _washSalePeriods = new Dictionary<string, decimal>
        {
            { "Default", 30 }, // 30-day wash sale period
            { "Extended", 60 } // Extended wash sale period
        };
    }

    public TaxOptimizationResult OptimizeTax(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions,
        TaxProfile taxProfile)
    {
        var currentMetrics = CalculateCurrentMetrics(portfolio, marketPrediction, securityPredictions);
        var taxEfficiency = CalculateTaxEfficiency(currentMetrics, taxProfile);
        var optimizationPlan = GenerateOptimizationPlan(portfolio, taxEfficiency);

        return new TaxOptimizationResult
        {
            CurrentMetrics = currentMetrics,
            TaxEfficiency = taxEfficiency,
            OptimizationPlan = optimizationPlan,
            TaxImpactMetrics = CalculateTaxImpactMetrics(optimizationPlan)
        };
    }

    private TaxMetrics CalculateCurrentMetrics(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions)
    {
        var currentAllocation = CalculateCurrentAllocation(portfolio);
        var marketMetrics = CalculateMarketMetrics(marketPrediction);
        var securityMetrics = CalculateSecurityMetrics(securityPredictions);
        var portfolioMetrics = CalculatePortfolioMetrics(portfolio);

        return new TaxMetrics
        {
            CurrentAllocation = currentAllocation,
            MarketMetrics = marketMetrics,
            SecurityMetrics = securityMetrics,
            PortfolioMetrics = portfolioMetrics,
            TaxEfficiency = CalculateTaxEfficiency(portfolio)
        };
    }

    private TaxEfficiency CalculateTaxEfficiency(
        TaxMetrics metrics,
        TaxProfile taxProfile)
    {
        var shortTermGains = CalculateShortTermGains(metrics);
        var longTermGains = CalculateLongTermGains(metrics);
        var dividendIncome = CalculateDividendIncome(metrics);
        var interestIncome = CalculateInterestIncome(metrics);

        var taxEfficiency = new TaxEfficiency
        {
            ShortTermGains = shortTermGains,
            LongTermGains = longTermGains,
            DividendIncome = dividendIncome,
            InterestIncome = interestIncome,
            TotalTaxableIncome = shortTermGains + longTermGains + dividendIncome + interestIncome,
            TaxRate = CalculateEffectiveTaxRate(metrics, taxProfile)
        };

        return taxEfficiency;
    }

    private decimal CalculateEffectiveTaxRate(
        TaxMetrics metrics,
        TaxProfile taxProfile)
    {
        var totalIncome = metrics.PortfolioMetrics.TotalValue * 
            (metrics.PortfolioMetrics.ExpectedReturn - 1);

        var shortTermGains = totalIncome * 
            metrics.PortfolioMetrics.ShortTermAllocation;
        var longTermGains = totalIncome * 
            metrics.PortfolioMetrics.LongTermAllocation;
        var dividendIncome = totalIncome * 
            metrics.PortfolioMetrics.DividendAllocation;
        var interestIncome = totalIncome * 
            metrics.PortfolioMetrics.InterestAllocation;

        var totalTax = (shortTermGains * _taxRates["ShortTerm"] +
                       longTermGains * _taxRates["LongTerm"] +
                       dividendIncome * _taxRates["Dividends"] +
                       interestIncome * _taxRates["Interest"]);

        return totalTax / totalIncome;
    }

    private TaxOptimizationPlan GenerateOptimizationPlan(
        Portfolio portfolio,
        TaxEfficiency taxEfficiency)
    {
        var plan = new TaxOptimizationPlan();
        var currentAllocation = CalculateCurrentAllocation(portfolio);
        var targetAllocation = CalculateTargetAllocation(taxEfficiency);

        foreach (var assetClass in targetAllocation.Keys)
        {
            var currentWeight = currentAllocation.TryGetValue(assetClass, out var weight) ? weight : 0;
            var targetWeight = targetAllocation[assetClass];
            var weightDifference = targetWeight - currentWeight;

            if (Math.Abs(weightDifference) > 0.01m) // Minimum threshold
            {
                plan.Add(new TaxOptimizationAction
                {
                    AssetClass = assetClass,
                    CurrentWeight = currentWeight,
                    TargetWeight = targetWeight,
                    WeightDifference = weightDifference,
                    Type = weightDifference > 0 ? TaxOptimizationActionType.Buy : TaxOptimizationActionType.Sell,
                    TaxImpact = CalculateTaxImpact(assetClass, weightDifference)
                });
            }
        }

        return plan;
    }

    private Dictionary<string, decimal> CalculateTargetAllocation(TaxEfficiency efficiency)
    {
        var allocation = new Dictionary<string, decimal>();
        var totalTax = efficiency.TotalTaxableIncome * efficiency.TaxRate;
        var adjustedWeight = 1m - efficiency.TaxRate;

        foreach (var assetClass in efficiency.ShortTermGains.Keys)
        {
            var baseWeight = CalculateBaseWeight(assetClass, efficiency);
            var taxFactor = CalculateTaxFactor(assetClass, efficiency);
            var adjustedWeight = baseWeight * taxFactor;

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

    private decimal CalculateBaseWeight(string assetClass, TaxEfficiency efficiency)
    {
        var baseWeight = 0.1m; // Default base weight
        var taxRate = _taxRates.TryGetValue(assetClass, out var rate) ? rate : _defaultTaxRate;

        return baseWeight * (1 - taxRate);
    }

    private decimal CalculateTaxFactor(string assetClass, TaxEfficiency efficiency)
    {
        var taxRate = _taxRates.TryGetValue(assetClass, out var rate) ? rate : _defaultTaxRate;
        var holdingPeriod = _holdingPeriods["LongTerm"];
        var washSalePeriod = _washSalePeriods["Default"];

        return (1 - taxRate) * 
            (1 + holdingPeriod * 0.01m) * 
            (1 - washSalePeriod * 0.001m);
    }

    private decimal CalculateTaxImpact(string assetClass, decimal weightDifference)
    {
        var taxRate = _taxRates.TryGetValue(assetClass, out var rate) ? rate : _defaultTaxRate;
        return Math.Abs(weightDifference) * taxRate;
    }

    private TaxImpactMetrics CalculateTaxImpactMetrics(TaxOptimizationPlan plan)
    {
        var totalTaxImpact = 0m;
        var shortTermTaxImpact = 0m;
        var longTermTaxImpact = 0m;
        var totalTaxSavings = 0m;
        var taxEfficiencyScore = 0m;

        foreach (var action in plan.Actions)
        {
            totalTaxImpact += action.TaxImpact;
            if (action.Type == TaxOptimizationActionType.Sell)
            {
                if (action.HoldingPeriod < _holdingPeriods["LongTerm"])
                {
                    shortTermTaxImpact += action.TaxImpact;
                }
                else
                {
                    longTermTaxImpact += action.TaxImpact;
                }
            }
        }

        // Calculate tax savings based on optimization
        var optimizedTaxImpact = CalculateOptimizedTaxImpact(plan);
        totalTaxSavings = totalTaxImpact - optimizedTaxImpact;

        // Calculate tax efficiency score
        taxEfficiencyScore = 1 - (totalTaxImpact / plan.TotalValue);

        return new TaxImpactMetrics
        {
            TotalTaxImpact = totalTaxImpact,
            ShortTermTaxImpact = shortTermTaxImpact,
            LongTermTaxImpact = longTermTaxImpact,
            TotalTaxSavings = totalTaxSavings,
            TaxEfficiencyScore = taxEfficiencyScore
        };
    }

    private decimal CalculateOptimizedTaxImpact(TaxOptimizationPlan plan)
    {
        var optimizedImpact = 0m;
        foreach (var action in plan.Actions)
        {
            if (action.Type == TaxOptimizationActionType.Sell)
            {
                var taxRate = _taxRates["LongTerm"];
                optimizedImpact += Math.Abs(action.WeightDifference) * taxRate;
            }
        }
        return optimizedImpact;
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
