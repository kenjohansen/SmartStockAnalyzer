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

public enum RebalancingStrategyType
{
    /// <summary>
    /// Rebalance at fixed time intervals
    /// </summary>
    TimeBased,

    /// <summary>
    /// Rebalance when allocation deviates from target
    /// </summary>
    ThresholdBased,

    /// <summary>
    /// Rebalance based on market conditions
    /// </summary>
    MarketConditionBased,

    /// <summary>
    /// Rebalance based on volatility
    /// </summary>
    VolatilityBased,

    /// <summary>
    /// Rebalance based on risk metrics
    /// </summary>
    RiskBased
}

public class RebalancingStrategy
{
    private readonly Dictionary<RebalancingStrategyType, RebalancingParameters> _defaultParameters;
    private readonly TimeSpan _defaultTimePeriod = TimeSpan.FromDays(30);
    private readonly decimal _defaultThreshold = 0.05m; // 5% default threshold
    private readonly decimal _defaultVolatilityThreshold = 0.15m; // 15% default volatility threshold

    public RebalancingStrategy()
    {
        _defaultParameters = new Dictionary<RebalancingStrategyType, RebalancingParameters>
        {
            { RebalancingStrategyType.TimeBased, new RebalancingParameters
                {
                    TimePeriod = _defaultTimePeriod,
                    MinimumRebalanceAmount = 1000m,
                    MaximumRebalanceAmount = 100000m
                }
            },
            { RebalancingStrategyType.ThresholdBased, new RebalancingParameters
                {
                    Threshold = _defaultThreshold,
                    MinimumRebalanceAmount = 1000m,
                    MaximumRebalanceAmount = 100000m
                }
            },
            { RebalancingStrategyType.MarketConditionBased, new RebalancingParameters
                {
                    MarketVolatilityThreshold = _defaultVolatilityThreshold,
                    MinimumRebalanceAmount = 1000m,
                    MaximumRebalanceAmount = 100000m
                }
            },
            { RebalancingStrategyType.VolatilityBased, new RebalancingParameters
                {
                    VolatilityThreshold = _defaultVolatilityThreshold,
                    MinimumRebalanceAmount = 1000m,
                    MaximumRebalanceAmount = 100000m
                }
            },
            { RebalancingStrategyType.RiskBased, new RebalancingParameters
                {
                    RiskThreshold = 0.1m,
                    MinimumRebalanceAmount = 1000m,
                    MaximumRebalanceAmount = 100000m
                }
            }
        };
    }

    public RebalancingPlan GenerateRebalancingPlan(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions,
        RiskProfile riskProfile,
        RebalancingStrategyType strategyType = RebalancingStrategyType.ThresholdBased)
    {
        var currentMetrics = CalculateCurrentMetrics(portfolio, marketPrediction, securityPredictions);
        var targetAllocation = CalculateTargetAllocation(currentMetrics, riskProfile);
        var rebalancingPlan = GeneratePlan(portfolio, targetAllocation, strategyType);

        return new RebalancingPlan
        {
            CurrentMetrics = currentMetrics,
            TargetAllocation = targetAllocation,
            Actions = rebalancingPlan,
            StrategyType = strategyType,
            PerformanceImpact = CalculatePerformanceImpact(rebalancingPlan)
        };
    }

    private PortfolioMetrics CalculateCurrentMetrics(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions)
    {
        var allocation = CalculateCurrentAllocation(portfolio);
        var marketMetrics = CalculateMarketMetrics(marketPrediction);
        var securityMetrics = CalculateSecurityMetrics(securityPredictions);
        var portfolioMetrics = CalculatePortfolioMetrics(portfolio);

        return new PortfolioMetrics
        {
            CurrentAllocation = allocation,
            MarketMetrics = marketMetrics,
            SecurityMetrics = securityMetrics,
            PortfolioMetrics = portfolioMetrics
        };
    }

    private Dictionary<string, decimal> CalculateTargetAllocation(
        PortfolioMetrics metrics,
        RiskProfile riskProfile)
    {
        var optimalAllocation = new Dictionary<string, decimal>();
        var totalValue = metrics.PortfolioMetrics.TotalValue;
        var riskTolerance = riskProfile.RiskTolerance;

        // Calculate target allocation based on risk profile and market conditions
        var equityWeight = CalculateEquityWeight(riskTolerance, metrics.MarketMetrics.Volatility);
        var bondWeight = CalculateBondWeight(riskTolerance, metrics.MarketMetrics.Volatility);
        var cashWeight = CalculateCashWeight(equityWeight, bondWeight);

        optimalAllocation["Equities"] = equityWeight;
        optimalAllocation["Bonds"] = bondWeight;
        optimalAllocation["Cash"] = cashWeight;

        return optimalAllocation;
    }

    private RebalancingPlan GeneratePlan(
        Portfolio portfolio,
        Dictionary<string, decimal> targetAllocation,
        RebalancingStrategyType strategyType)
    {
        var currentAllocation = CalculateCurrentAllocation(portfolio);
        var plan = new RebalancingPlan
        {
            StrategyType = strategyType,
            Actions = new List<RebalancingAction>()
        };

        switch (strategyType)
        {
            case RebalancingStrategyType.TimeBased:
                GenerateTimeBasedPlan(portfolio, targetAllocation, plan);
                break;
            case RebalancingStrategyType.ThresholdBased:
                GenerateThresholdBasedPlan(portfolio, targetAllocation, plan);
                break;
            case RebalancingStrategyType.MarketConditionBased:
                GenerateMarketConditionBasedPlan(portfolio, targetAllocation, plan);
                break;
            case RebalancingStrategyType.VolatilityBased:
                GenerateVolatilityBasedPlan(portfolio, targetAllocation, plan);
                break;
            case RebalancingStrategyType.RiskBased:
                GenerateRiskBasedPlan(portfolio, targetAllocation, plan);
                break;
        }

        return plan;
    }

    private void GenerateTimeBasedPlan(
        Portfolio portfolio,
        Dictionary<string, decimal> targetAllocation,
        RebalancingPlan plan)
    {
        var parameters = _defaultParameters[RebalancingStrategyType.TimeBased];
        var currentAllocation = CalculateCurrentAllocation(portfolio);

        foreach (var assetClass in targetAllocation.Keys)
        {
            var currentWeight = currentAllocation.TryGetValue(assetClass, out var weight) ? weight : 0;
            var targetWeight = targetAllocation[assetClass];
            var weightDifference = targetWeight - currentWeight;

            if (Math.Abs(weightDifference) > 0.01m) // Minimum threshold
            {
                var action = new RebalancingAction
                {
                    AssetClass = assetClass,
                    CurrentWeight = currentWeight,
                    TargetWeight = targetWeight,
                    WeightDifference = weightDifference,
                    Type = weightDifference > 0 ? RebalancingActionType.Buy : RebalancingActionType.Sell,
                    Amount = CalculateRebalanceAmount(
                        portfolio.TotalValue,
                        weightDifference,
                        parameters.MinimumRebalanceAmount,
                        parameters.MaximumRebalanceAmount
                    )
                };

                plan.Actions.Add(action);
            }
        }
    }

    private void GenerateThresholdBasedPlan(
        Portfolio portfolio,
        Dictionary<string, decimal> targetAllocation,
        RebalancingPlan plan)
    {
        var parameters = _defaultParameters[RebalancingStrategyType.ThresholdBased];
        var currentAllocation = CalculateCurrentAllocation(portfolio);

        foreach (var assetClass in targetAllocation.Keys)
        {
            var currentWeight = currentAllocation.TryGetValue(assetClass, out var weight) ? weight : 0;
            var targetWeight = targetAllocation[assetClass];
            var weightDifference = targetWeight - currentWeight;

            if (Math.Abs(weightDifference) > parameters.Threshold)
            {
                var action = new RebalancingAction
                {
                    AssetClass = assetClass,
                    CurrentWeight = currentWeight,
                    TargetWeight = targetWeight,
                    WeightDifference = weightDifference,
                    Type = weightDifference > 0 ? RebalancingActionType.Buy : RebalancingActionType.Sell,
                    Amount = CalculateRebalanceAmount(
                        portfolio.TotalValue,
                        weightDifference,
                        parameters.MinimumRebalanceAmount,
                        parameters.MaximumRebalanceAmount
                    )
                };

                plan.Actions.Add(action);
            }
        }
    }

    private void GenerateMarketConditionBasedPlan(
        Portfolio portfolio,
        Dictionary<string, decimal> targetAllocation,
        RebalancingPlan plan)
    {
        var parameters = _defaultParameters[RebalancingStrategyType.MarketConditionBased];
        var currentAllocation = CalculateCurrentAllocation(portfolio);

        foreach (var assetClass in targetAllocation.Keys)
        {
            var currentWeight = currentAllocation.TryGetValue(assetClass, out var weight) ? weight : 0;
            var targetWeight = targetAllocation[assetClass];
            var weightDifference = targetWeight - currentWeight;

            // Adjust threshold based on market volatility
            var adjustedThreshold = parameters.Threshold * 
                (1 + portfolio.Volatility * 0.1m);

            if (Math.Abs(weightDifference) > adjustedThreshold)
            {
                var action = new RebalancingAction
                {
                    AssetClass = assetClass,
                    CurrentWeight = currentWeight,
                    TargetWeight = targetWeight,
                    WeightDifference = weightDifference,
                    Type = weightDifference > 0 ? RebalancingActionType.Buy : RebalancingActionType.Sell,
                    Amount = CalculateRebalanceAmount(
                        portfolio.TotalValue,
                        weightDifference,
                        parameters.MinimumRebalanceAmount,
                        parameters.MaximumRebalanceAmount
                    )
                };

                plan.Actions.Add(action);
            }
        }
    }

    private void GenerateVolatilityBasedPlan(
        Portfolio portfolio,
        Dictionary<string, decimal> targetAllocation,
        RebalancingPlan plan)
    {
        var parameters = _defaultParameters[RebalancingStrategyType.VolatilityBased];
        var currentAllocation = CalculateCurrentAllocation(portfolio);

        foreach (var assetClass in targetAllocation.Keys)
        {
            var currentWeight = currentAllocation.TryGetValue(assetClass, out var weight) ? weight : 0;
            var targetWeight = targetAllocation[assetClass];
            var weightDifference = targetWeight - currentWeight;

            // Adjust threshold based on portfolio volatility
            var adjustedThreshold = parameters.Threshold * 
                (1 + portfolio.Volatility * 0.1m);

            if (Math.Abs(weightDifference) > adjustedThreshold)
            {
                var action = new RebalancingAction
                {
                    AssetClass = assetClass,
                    CurrentWeight = currentWeight,
                    TargetWeight = targetWeight,
                    WeightDifference = weightDifference,
                    Type = weightDifference > 0 ? RebalancingActionType.Buy : RebalancingActionType.Sell,
                    Amount = CalculateRebalanceAmount(
                        portfolio.TotalValue,
                        weightDifference,
                        parameters.MinimumRebalanceAmount,
                        parameters.MaximumRebalanceAmount
                    )
                };

                plan.Actions.Add(action);
            }
        }
    }

    private void GenerateRiskBasedPlan(
        Portfolio portfolio,
        Dictionary<string, decimal> targetAllocation,
        RebalancingPlan plan)
    {
        var parameters = _defaultParameters[RebalancingStrategyType.RiskBased];
        var currentAllocation = CalculateCurrentAllocation(portfolio);

        foreach (var assetClass in targetAllocation.Keys)
        {
            var currentWeight = currentAllocation.TryGetValue(assetClass, out var weight) ? weight : 0;
            var targetWeight = targetAllocation[assetClass];
            var weightDifference = targetWeight - currentWeight;

            // Adjust threshold based on risk metrics
            var adjustedThreshold = parameters.Threshold * 
                (1 + portfolio.RiskLevel * 0.1m);

            if (Math.Abs(weightDifference) > adjustedThreshold)
            {
                var action = new RebalancingAction
                {
                    AssetClass = assetClass,
                    CurrentWeight = currentWeight,
                    TargetWeight = targetWeight,
                    WeightDifference = weightDifference,
                    Type = weightDifference > 0 ? RebalancingActionType.Buy : RebalancingActionType.Sell,
                    Amount = CalculateRebalanceAmount(
                        portfolio.TotalValue,
                        weightDifference,
                        parameters.MinimumRebalanceAmount,
                        parameters.MaximumRebalanceAmount
                    )
                };

                plan.Actions.Add(action);
            }
        }
    }

    private decimal CalculateRebalanceAmount(
        decimal portfolioValue,
        decimal weightDifference,
        decimal minAmount,
        decimal maxAmount)
    {
        var amount = portfolioValue * Math.Abs(weightDifference);
        return Math.Max(minAmount, Math.Min(amount, maxAmount));
    }

    private PerformanceImpact CalculatePerformanceImpact(RebalancingPlan plan)
    {
        var totalImpact = 0m;
        var transactionCosts = 0m;
        var marketImpact = 0m;
        var volatilityImpact = 0m;

        foreach (var action in plan.Actions)
        {
            var impact = CalculateActionImpact(action);
            totalImpact += impact;
            transactionCosts += CalculateTransactionCosts(action);
            marketImpact += CalculateMarketImpact(action);
            volatilityImpact += CalculateVolatilityImpact(action);
        }

        return new PerformanceImpact
        {
            TotalImpact = totalImpact,
            TransactionCosts = transactionCosts,
            MarketImpact = marketImpact,
            VolatilityImpact = volatilityImpact,
            RiskImpact = CalculateRiskImpact(plan)
        };
    }

    private decimal CalculateActionImpact(RebalancingAction action)
    {
        var impact = action.Amount * 
            (action.Type == RebalancingActionType.Buy ? 0.001m : -0.001m); // 0.1% impact per action
        return impact;
    }

    private decimal CalculateTransactionCosts(RebalancingAction action)
    {
        const decimal costRate = 0.001m; // 0.1% transaction cost
        return action.Amount * costRate;
    }

    private decimal CalculateMarketImpact(RebalancingAction action)
    {
        var impact = action.Amount * 
            (action.Type == RebalancingActionType.Buy ? 0.0005m : -0.0005m); // 0.05% market impact
        return impact;
    }

    private decimal CalculateVolatilityImpact(RebalancingAction action)
    {
        var impact = action.Amount * 
            (action.Type == RebalancingActionType.Buy ? 0.0002m : -0.0002m); // 0.02% volatility impact
        return impact;
    }

    private decimal CalculateRiskImpact(RebalancingPlan plan)
    {
        var totalImpact = 0m;
        foreach (var action in plan.Actions)
        {
            var impact = action.Amount * 
                (action.Type == RebalancingActionType.Buy ? 0.0003m : -0.0003m); // 0.03% risk impact
            totalImpact += impact;
        }
        return totalImpact;
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

    private decimal CalculateEquityWeight(decimal riskTolerance, decimal marketVolatility)
    {
        return Math.Max(
            Math.Min(
                riskTolerance * (1 + marketVolatility * 0.1m),
                0.9m
            ),
            0.1m
        );
    }

    private decimal CalculateBondWeight(decimal riskTolerance, decimal marketVolatility)
    {
        return Math.Max(
            Math.Min(
                (1 - riskTolerance) * (1 - marketVolatility * 0.1m),
                0.8m
            ),
            0.1m
        );
    }

    private decimal CalculateCashWeight(decimal equityWeight, decimal bondWeight)
    {
        return Math.Max(0m, 1 - equityWeight - bondWeight);
    }

    private string GetAssetClass(string symbol)
    {
        // TODO: Implement actual asset class classification
        if (symbol.EndsWith(".BOND")) return "Bonds";
        if (symbol.EndsWith(".CASH")) return "Cash";
        return "Equities";
    }
}
