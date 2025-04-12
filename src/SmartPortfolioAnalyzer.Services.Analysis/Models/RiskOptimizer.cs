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

public class RiskOptimizer
{
    private const decimal DefaultRiskTolerance = 0.1m; // 10% default risk tolerance
    private const decimal MinimumRiskLevel = 0.01m; // 1% minimum risk level
    private const decimal MaximumRiskLevel = 0.3m; // 30% maximum risk level

    public RiskOptimizationResult OptimizeRisk(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions,
        RiskProfile riskProfile)
    {
        var currentRiskMetrics = CalculateCurrentRiskMetrics(portfolio, marketPrediction, securityPredictions);
        var targetRiskMetrics = CalculateTargetRiskMetrics(currentRiskMetrics, riskProfile);
        var optimizationResult = CalculateOptimization(currentRiskMetrics, targetRiskMetrics);

        return new RiskOptimizationResult
        {
            CurrentRiskMetrics = currentRiskMetrics,
            TargetRiskMetrics = targetRiskMetrics,
            OptimizationResult = optimizationResult,
            Recommendations = GenerateRecommendations(currentRiskMetrics, targetRiskMetrics)
        };
    }

    private RiskMetrics CalculateCurrentRiskMetrics(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions)
    {
        var portfolioRisk = CalculatePortfolioRisk(portfolio);
        var marketRisk = CalculateMarketRisk(marketPrediction);
        var securityRisks = CalculateSecurityRisks(securityPredictions);
        var correlationRisk = CalculateCorrelationRisk(portfolio, securityPredictions);
        var concentrationRisk = CalculateConcentrationRisk(portfolio);

        return new RiskMetrics
        {
            PortfolioRisk = portfolioRisk,
            MarketRisk = marketRisk,
            SecurityRisks = securityRisks,
            CorrelationRisk = correlationRisk,
            ConcentrationRisk = concentrationRisk,
            TotalRisk = CalculateTotalRisk(portfolioRisk, marketRisk, securityRisks)
        };
    }

    private decimal CalculatePortfolioRisk(Portfolio portfolio)
    {
        var returns = portfolio.Positions
            .Select(p => CalculatePositionReturn(p))
            .ToArray();

        var volatility = CalculateVolatility(returns);
        return CalculateRiskLevel(volatility);
    }

    private decimal CalculateMarketRisk(MarketPredictionResult marketPrediction)
    {
        return marketPrediction.Prediction.Volatility * marketPrediction.Prediction.RiskLevel;
    }

    private Dictionary<string, decimal> CalculateSecurityRisks(SecurityPredictionResult[] predictions)
    {
        return predictions.ToDictionary(
            p => p.Symbol,
            p => p.Prediction.Volatility * p.Prediction.RiskLevel
        );
    }

    private decimal CalculateCorrelationRisk(
        Portfolio portfolio,
        SecurityPredictionResult[] predictions)
    {
        var correlationSum = 0m;
        var count = 0;

        foreach (var pair in GetSecurityPairs(portfolio.Positions))
        {
            var symbol1 = pair.Item1.Symbol;
            var symbol2 = pair.Item2.Symbol;

            var prediction1 = predictions.FirstOrDefault(p => p.Symbol == symbol1);
            var prediction2 = predictions.FirstOrDefault(p => p.Symbol == symbol2);

            if (prediction1 != null && prediction2 != null)
            {
                correlationSum += CalculateCorrelation(prediction1, prediction2);
                count++;
            }
        }

        return count > 0 ? correlationSum / count : 0m;
    }

    private decimal CalculateConcentrationRisk(Portfolio portfolio)
    {
        var weights = portfolio.Positions
            .Select(p => p.Weight)
            .OrderByDescending(w => w)
            .ToArray();

        var gini = CalculateGiniCoefficient(weights);
        return gini * portfolio.RiskLevel;
    }

    private RiskMetrics CalculateTargetRiskMetrics(
        RiskMetrics currentMetrics,
        RiskProfile riskProfile)
    {
        var targetRisk = Math.Max(
            Math.Min(riskProfile.RiskTolerance, MaximumRiskLevel),
            MinimumRiskLevel
        );

        var targetPortfolioRisk = targetRisk * currentMetrics.PortfolioRisk;
        var targetMarketRisk = targetRisk * currentMetrics.MarketRisk;
        var targetCorrelationRisk = targetRisk * currentMetrics.CorrelationRisk;
        var targetConcentrationRisk = targetRisk * currentMetrics.ConcentrationRisk;

        return new RiskMetrics
        {
            PortfolioRisk = targetPortfolioRisk,
            MarketRisk = targetMarketRisk,
            SecurityRisks = AdjustSecurityRisks(currentMetrics.SecurityRisks, targetRisk),
            CorrelationRisk = targetCorrelationRisk,
            ConcentrationRisk = targetConcentrationRisk,
            TotalRisk = CalculateTotalRisk(targetPortfolioRisk, targetMarketRisk, currentMetrics.SecurityRisks)
        };
    }

    private Dictionary<string, decimal> AdjustSecurityRisks(
        Dictionary<string, decimal> currentRisks,
        decimal targetRisk)
    {
        var adjustedRisks = new Dictionary<string, decimal>();
        foreach (var pair in currentRisks)
        {
            adjustedRisks[pair.Key] = pair.Value * targetRisk;
        }
        return adjustedRisks;
    }

    private RiskOptimizationResult CalculateOptimization(
        RiskMetrics currentMetrics,
        RiskMetrics targetMetrics)
    {
        var optimization = new RiskOptimizationResult
        {
            RiskReduction = currentMetrics.TotalRisk - targetMetrics.TotalRisk,
            PortfolioAdjustments = CalculatePortfolioAdjustments(currentMetrics, targetMetrics),
            MarketAdjustments = CalculateMarketAdjustments(currentMetrics, targetMetrics),
            SecurityAdjustments = CalculateSecurityAdjustments(currentMetrics, targetMetrics),
            CorrelationAdjustments = CalculateCorrelationAdjustments(currentMetrics, targetMetrics),
            ConcentrationAdjustments = CalculateConcentrationAdjustments(currentMetrics, targetMetrics)
        };

        return optimization;
    }

    private IEnumerable<RiskAdjustment> GenerateRecommendations(
        RiskMetrics currentMetrics,
        RiskMetrics targetMetrics)
    {
        var recommendations = new List<RiskAdjustment>();

        if (currentMetrics.TotalRisk > targetMetrics.TotalRisk)
        {
            recommendations.Add(new RiskAdjustment
            {
                Type = AdjustmentType.ReduceRisk,
                Priority = 1,
                Description = "Overall portfolio risk is too high",
                Recommendation = "Consider reducing exposure to high-risk assets"
            });
        }

        if (currentMetrics.PortfolioRisk > targetMetrics.PortfolioRisk)
        {
            recommendations.Add(new RiskAdjustment
            {
                Type = AdjustmentType.ReduceRisk,
                Priority = 2,
                Description = "Portfolio risk is above target",
                Recommendation = "Consider rebalancing portfolio to target risk levels"
            });
        }

        if (currentMetrics.CorrelationRisk > targetMetrics.CorrelationRisk)
        {
            recommendations.Add(new RiskAdjustment
            {
                Type = AdjustmentType.ReduceRisk,
                Priority = 3,
                Description = "High correlation risk",
                Recommendation = "Consider diversifying holdings to reduce correlation"
            });
        }

        if (currentMetrics.ConcentrationRisk > targetMetrics.ConcentrationRisk)
        {
            recommendations.Add(new RiskAdjustment
            {
                Type = AdjustmentType.ReduceRisk,
                Priority = 4,
                Description = "High concentration risk",
                Recommendation = "Consider spreading investments across more assets"
            });
        }

        return recommendations;
    }

    private decimal CalculatePositionReturn(PortfolioPosition position)
    {
        return (position.CurrentValue - position.InitialValue) / position.InitialValue;
    }

    private decimal CalculateVolatility(decimal[] returns)
    {
        var mean = returns.Average();
        var squaredDeviations = returns.Select(r => (r - mean) * (r - mean));
        return (decimal)Math.Sqrt(squaredDeviations.Average());
    }

    private decimal CalculateRiskLevel(decimal volatility)
    {
        return volatility * 100m; // Convert to percentage
    }

    private decimal CalculateTotalRisk(
        decimal portfolioRisk,
        decimal marketRisk,
        Dictionary<string, decimal> securityRisks)
    {
        var totalSecurityRisk = securityRisks.Values.Sum();
        return portfolioRisk + marketRisk + totalSecurityRisk;
    }

    private IEnumerable<(PortfolioPosition, PortfolioPosition)> GetSecurityPairs(
        IEnumerable<PortfolioPosition> positions)
    {
        var list = positions.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                yield return (list[i], list[j]);
            }
        }
    }

    private decimal CalculateCorrelation(
        SecurityPredictionResult prediction1,
        SecurityPredictionResult prediction2)
    {
        var returns1 = CalculateReturns(prediction1.Prediction.ExpectedReturn);
        var returns2 = CalculateReturns(prediction2.Prediction.ExpectedReturn);
        return CalculatePearsonCorrelation(returns1, returns2);
    }

    private decimal[] CalculateReturns(decimal expectedReturn)
    {
        var returns = new decimal[10]; // Simulate 10 periods
        for (int i = 0; i < returns.Length; i++)
        {
            returns[i] = expectedReturn * (1 + (i - 5) * 0.01m); // Simple linear simulation
        }
        return returns;
    }

    private decimal CalculatePearsonCorrelation(decimal[] x, decimal[] y)
    {
        var n = x.Length;
        var sumX = x.Sum();
        var sumY = y.Sum();
        var sumXY = x.Zip(y, (a, b) => a * b).Sum();
        var sumX2 = x.Select(a => a * a).Sum();
        var sumY2 = y.Select(b => b * b).Sum();

        var numerator = n * sumXY - sumX * sumY;
        var denominator = Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

        return denominator != 0 ? numerator / denominator : 0;
    }

    private decimal CalculateGiniCoefficient(decimal[] weights)
    {
        var n = weights.Length;
        var sum = weights.Sum();
        var sorted = weights.OrderBy(w => w).ToArray();
        var cumulative = 0m;

        var numerator = 0m;
        for (int i = 0; i < n; i++)
        {
            cumulative += sorted[i];
            numerator += (n - i) * sorted[i];
        }

        return 1 - (2 * numerator) / (n * sum);
    }
}
