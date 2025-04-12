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

public class PortfolioDiversificationAnalyzer
{
    private readonly Dictionary<string, decimal> _diversificationFactors;
    private readonly Dictionary<string, decimal> _correlationFactors;
    private readonly Dictionary<string, decimal> _riskFactors;
    private readonly decimal _minimumDiversificationScore = 0.7m;
    private readonly decimal _maximumConcentrationRisk = 0.2m;

    public PortfolioDiversificationAnalyzer()
    {
        _diversificationFactors = new Dictionary<string, decimal>
        {
            { "Sector", 0.3m },
            { "Region", 0.3m },
            { "MarketCap", 0.2m },
            { "Style", 0.2m }
        };

        _correlationFactors = new Dictionary<string, decimal>
        {
            { "High", 0.8m },
            { "Medium", 0.5m },
            { "Low", 0.2m }
        };

        _riskFactors = new Dictionary<string, decimal>
        {
            { "Equities", 1.2m },
            { "Bonds", 0.8m },
            { "Cash", 0.2m }
        };
    }

    public DiversificationAnalysisResult AnalyzeDiversification(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions)
    {
        var metrics = CalculateDiversificationMetrics(portfolio, marketPrediction, securityPredictions);
        var diversificationScore = CalculateDiversificationScore(metrics);
        var recommendations = GenerateDiversificationRecommendations(metrics, diversificationScore);

        return new DiversificationAnalysisResult
        {
            Metrics = metrics,
            DiversificationScore = diversificationScore,
            Recommendations = recommendations,
            RiskMetrics = CalculateRiskMetrics(metrics)
        };
    }

    private DiversificationMetrics CalculateDiversificationMetrics(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        SecurityPredictionResult[] securityPredictions)
    {
        var sectorDistribution = CalculateSectorDistribution(portfolio);
        var regionDistribution = CalculateRegionDistribution(portfolio);
        var marketCapDistribution = CalculateMarketCapDistribution(portfolio);
        var styleDistribution = CalculateStyleDistribution(portfolio);
        var correlationMetrics = CalculateCorrelationMetrics(portfolio, securityPredictions);
        var concentrationMetrics = CalculateConcentrationMetrics(portfolio);

        return new DiversificationMetrics
        {
            SectorDistribution = sectorDistribution,
            RegionDistribution = regionDistribution,
            MarketCapDistribution = marketCapDistribution,
            StyleDistribution = styleDistribution,
            CorrelationMetrics = correlationMetrics,
            ConcentrationMetrics = concentrationMetrics,
            MarketMetrics = CalculateMarketMetrics(marketPrediction)
        };
    }

    private decimal CalculateDiversificationScore(DiversificationMetrics metrics)
    {
        var score = 0m;
        var factors = _diversificationFactors.Values.Sum();

        // Sector diversification
        score += CalculateDistributionScore(metrics.SectorDistribution) * 
            _diversificationFactors["Sector"] / factors;

        // Region diversification
        score += CalculateDistributionScore(metrics.RegionDistribution) * 
            _diversificationFactors["Region"] / factors;

        // Market cap diversification
        score += CalculateDistributionScore(metrics.MarketCapDistribution) * 
            _diversificationFactors["MarketCap"] / factors;

        // Style diversification
        score += CalculateDistributionScore(metrics.StyleDistribution) * 
            _diversificationFactors["Style"] / factors;

        // Correlation impact
        score *= (1 - metrics.CorrelationMetrics.AverageCorrelation * 0.1m);

        // Concentration impact
        score *= (1 - metrics.ConcentrationMetrics.MaximumConcentration * 0.1m);

        return Math.Max(0, Math.Min(1, score));
    }

    private decimal CalculateDistributionScore(Dictionary<string, decimal> distribution)
    {
        if (distribution.Count < 2) return 0;

        var entropy = 0m;
        foreach (var weight in distribution.Values)
        {
            if (weight > 0)
            {
                entropy -= weight * (decimal)Math.Log(weight);
            }
        }

        return entropy / Math.Log(distribution.Count);
    }

    private Dictionary<string, decimal> CalculateSectorDistribution(Portfolio portfolio)
    {
        var distribution = new Dictionary<string, decimal>();
        var totalValue = portfolio.Positions.Sum(p => p.CurrentValue);

        foreach (var position in portfolio.Positions)
        {
            var sector = GetSector(position.Symbol);
            if (!distribution.ContainsKey(sector))
            {
                distribution[sector] = 0;
            }
            distribution[sector] += position.CurrentValue / totalValue;
        }

        return distribution;
    }

    private Dictionary<string, decimal> CalculateRegionDistribution(Portfolio portfolio)
    {
        var distribution = new Dictionary<string, decimal>();
        var totalValue = portfolio.Positions.Sum(p => p.CurrentValue);

        foreach (var position in portfolio.Positions)
        {
            var region = GetRegion(position.Symbol);
            if (!distribution.ContainsKey(region))
            {
                distribution[region] = 0;
            }
            distribution[region] += position.CurrentValue / totalValue;
        }

        return distribution;
    }

    private Dictionary<string, decimal> CalculateMarketCapDistribution(Portfolio portfolio)
    {
        var distribution = new Dictionary<string, decimal>();
        var totalValue = portfolio.Positions.Sum(p => p.CurrentValue);

        foreach (var position in portfolio.Positions)
        {
            var marketCap = GetMarketCap(position.Symbol);
            if (!distribution.ContainsKey(marketCap))
            {
                distribution[marketCap] = 0;
            }
            distribution[marketCap] += position.CurrentValue / totalValue;
        }

        return distribution;
    }

    private Dictionary<string, decimal> CalculateStyleDistribution(Portfolio portfolio)
    {
        var distribution = new Dictionary<string, decimal>();
        var totalValue = portfolio.Positions.Sum(p => p.CurrentValue);

        foreach (var position in portfolio.Positions)
        {
            var style = GetStyle(position.Symbol);
            if (!distribution.ContainsKey(style))
            {
                distribution[style] = 0;
            }
            distribution[style] += position.CurrentValue / totalValue;
        }

        return distribution;
    }

    private CorrelationMetrics CalculateCorrelationMetrics(
        Portfolio portfolio,
        SecurityPredictionResult[] predictions)
    {
        var correlations = new List<decimal>();
        var count = 0;

        foreach (var pair in GetSecurityPairs(portfolio.Positions))
        {
            var prediction1 = predictions.FirstOrDefault(p => p.Symbol == pair.Item1.Symbol);
            var prediction2 = predictions.FirstOrDefault(p => p.Symbol == pair.Item2.Symbol);

            if (prediction1 != null && prediction2 != null)
            {
                correlations.Add(CalculateCorrelation(prediction1, prediction2));
                count++;
            }
        }

        return new CorrelationMetrics
        {
            AverageCorrelation = count > 0 ? correlations.Average() : 0,
            MaximumCorrelation = count > 0 ? correlations.Max() : 0,
            MinimumCorrelation = count > 0 ? correlations.Min() : 0
        };
    }

    private ConcentrationMetrics CalculateConcentrationMetrics(Portfolio portfolio)
    {
        var weights = portfolio.Positions
            .Select(p => p.CurrentValue / portfolio.TotalValue)
            .OrderByDescending(w => w)
            .ToArray();

        var gini = CalculateGiniCoefficient(weights);
        var herfindahl = CalculateHerfindahlIndex(weights);
        var maximumConcentration = weights.FirstOrDefault();

        return new ConcentrationMetrics
        {
            GiniCoefficient = gini,
            HerfindahlIndex = herfindahl,
            MaximumConcentration = maximumConcentration
        };
    }

    private IEnumerable<RiskRecommendation> GenerateDiversificationRecommendations(
        DiversificationMetrics metrics,
        decimal diversificationScore)
    {
        var recommendations = new List<RiskRecommendation>();

        if (diversificationScore < _minimumDiversificationScore)
        {
            recommendations.Add(new RiskRecommendation
            {
                Priority = 1,
                Type = RecommendationType.ImproveDiversification,
                Description = "Portfolio is insufficiently diversified",
                Recommendation = "Consider adding positions in underrepresented sectors/regions"
            });
        }

        if (metrics.ConcentrationMetrics.MaximumConcentration > _maximumConcentrationRisk)
        {
            recommendations.Add(new RiskRecommendation
            {
                Priority = 2,
                Type = RecommendationType.ReduceConcentration,
                Description = "High concentration risk in portfolio",
                Recommendation = "Consider reducing exposure to largest positions"
            });
        }

        if (metrics.CorrelationMetrics.AverageCorrelation > 0.5m)
        {
            recommendations.Add(new RiskRecommendation
            {
                Priority = 3,
                Type = RecommendationType.ReduceCorrelation,
                Description = "High average correlation between holdings",
                Recommendation = "Consider adding negatively correlated assets"
            });
        }

        return recommendations;
    }

    private RiskMetrics CalculateRiskMetrics(DiversificationMetrics metrics)
    {
        var totalRisk = CalculateTotalRisk(metrics);
        var marketRisk = CalculateMarketRisk(metrics);
        var assetClassRisk = CalculateAssetClassRisk(metrics);
        var correlationRisk = CalculateCorrelationRisk(metrics);
        var concentrationRisk = CalculateConcentrationRisk(metrics);

        return new RiskMetrics
        {
            TotalRisk = totalRisk,
            MarketRisk = marketRisk,
            AssetClassRisk = assetClassRisk,
            CorrelationRisk = correlationRisk,
            ConcentrationRisk = concentrationRisk
        };
    }

    private decimal CalculateTotalRisk(DiversificationMetrics metrics)
    {
        var risk = 0m;
        var factors = _riskFactors.Values.Sum();

        foreach (var assetClass in metrics.MarketMetrics.AssetClassDistribution.Keys)
        {
            var weight = metrics.MarketMetrics.AssetClassDistribution[assetClass];
            var riskFactor = _riskFactors[assetClass];
            risk += weight * riskFactor / factors;
        }

        // Adjust for correlation and concentration
        risk *= (1 + metrics.CorrelationMetrics.AverageCorrelation * 0.1m);
        risk *= (1 + metrics.ConcentrationMetrics.MaximumConcentration * 0.1m);

        return risk;
    }

    private decimal CalculateCorrelation(decimal[] x, decimal[] y)
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

    private decimal CalculateHerfindahlIndex(decimal[] weights)
    {
        return weights.Select(w => w * w).Sum();
    }

    private string GetSector(string symbol)
    {
        // TODO: Implement actual sector classification
        if (symbol.StartsWith("TECH")) return "Technology";
        if (symbol.StartsWith("FIN")) return "Financials";
        if (symbol.StartsWith("HEALTH")) return "Healthcare";
        return "Other";
    }

    private string GetRegion(string symbol)
    {
        // TODO: Implement actual region classification
        if (symbol.EndsWith(".US")) return "United States";
        if (symbol.EndsWith(".EU")) return "Europe";
        if (symbol.EndsWith(".AS")) return "Asia";
        return "Other";
    }

    private string GetMarketCap(string symbol)
    {
        // TODO: Implement actual market cap classification
        if (symbol.Contains("SMALL")) return "Small Cap";
        if (symbol.Contains("MID")) return "Mid Cap";
        return "Large Cap";
    }

    private string GetStyle(string symbol)
    {
        // TODO: Implement actual style classification
        if (symbol.Contains("VALUE")) return "Value";
        if (symbol.Contains("GROWTH")) return "Growth";
        return "Blend";
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
}
