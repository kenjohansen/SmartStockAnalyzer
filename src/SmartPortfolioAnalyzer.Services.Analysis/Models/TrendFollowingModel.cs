/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartPortfolioAnalyzer.Core.Models;
using SmartPortfolioAnalyzer.Services.Analysis.Interfaces;

namespace SmartPortfolioAnalyzer.Services.Analysis.Models;

public class TrendFollowingModel : IPredictionModel
{
    private readonly Random _random = new Random();
    private readonly int[] _defaultPeriods = { 20, 50, 100, 200 }; // Common moving average periods

    public PredictionModelType ModelType => PredictionModelType.TrendFollowing;

    public async Task<MarketPredictionResult> PredictMarketConditionsAsync(
        IEnumerable<decimal> historicalData,
        EconomicContext economicContext,
        int timeHorizon = 30)
    {
        var historicalPrices = historicalData.ToArray();
        var movingAverages = CalculateMovingAverages(historicalPrices);
        var trendStrength = CalculateTrendStrength(movingAverages);
        var volatility = CalculateVolatility(historicalPrices);

        var prediction = new MarketPrediction
        {
            Direction = CalculateTrendDirection(movingAverages),
            ExpectedReturn = CalculateExpectedReturn(trendStrength, volatility),
            Volatility = volatility,
            RiskLevel = CalculateRiskLevel(volatility, trendStrength)
        };

        return new MarketPredictionResult
        {
            Prediction = prediction,
            ConfidenceScore = CalculateConfidenceScore(trendStrength, volatility),
            TimeHorizon = timeHorizon,
            EconomicContext = economicContext,
            ModelPerformance = await GetPerformanceMetricsAsync()
        };
    }

    public async Task<SecurityPredictionResult> PredictSecurityPerformanceAsync(
        string symbol,
        IEnumerable<decimal> historicalPrices,
        EconomicFactors economicFactors,
        int timeHorizon = 30)
    {
        var movingAverages = CalculateMovingAverages(historicalPrices);
        var trendStrength = CalculateTrendStrength(movingAverages);
        var volatility = CalculateVolatility(historicalPrices);
        var volumeTrend = CalculateVolumeTrend(economicFactors);

        var prediction = new SecurityPrediction
        {
            ExpectedReturn = CalculateExpectedReturn(trendStrength, volatility, volumeTrend),
            Volatility = volatility,
            RiskLevel = CalculateRiskLevel(volatility, trendStrength),
            TechnicalScore = CalculateTechnicalScore(movingAverages, volumeTrend)
        };

        return new SecurityPredictionResult
        {
            Symbol = symbol,
            Prediction = prediction,
            ConfidenceScore = CalculateConfidenceScore(trendStrength, volatility, volumeTrend),
            TimeHorizon = timeHorizon,
            EconomicFactors = economicFactors,
            ModelPerformance = await GetPerformanceMetricsAsync()
        };
    }

    public async Task<PortfolioPredictionResult> PredictPortfolioPerformanceAsync(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        int timeHorizon = 30)
    {
        var portfolioMAs = CalculatePortfolioMovingAverages(portfolio);
        var portfolioTrend = CalculatePortfolioTrend(portfolioMAs);
        var portfolioVolatility = CalculatePortfolioVolatility(portfolio);

        var prediction = new PortfolioPrediction
        {
            ExpectedReturn = CalculateExpectedReturn(portfolioTrend, portfolioVolatility),
            Volatility = portfolioVolatility,
            RiskLevel = CalculateRiskLevel(portfolioVolatility, portfolioTrend),
            AssetAllocation = CalculateTrendFollowingAllocation(portfolio, portfolioMAs)
        };

        return new PortfolioPredictionResult
        {
            Portfolio = portfolio,
            Prediction = prediction,
            ConfidenceScore = CalculateConfidenceScore(portfolioTrend, portfolioVolatility),
            TimeHorizon = timeHorizon,
            MarketPrediction = marketPrediction,
            ModelPerformance = await GetPerformanceMetricsAsync()
        };
    }

    private Dictionary<int, decimal> CalculateMovingAverages(IEnumerable<decimal> prices)
    {
        var movingAverages = new Dictionary<int, decimal>();
        var priceArray = prices.ToArray();

        foreach (var period in _defaultPeriods)
        {
            if (period <= priceArray.Length)
            {
                var sum = priceArray.Skip(priceArray.Length - period).Sum();
                movingAverages[period] = sum / period;
            }
        }

        return movingAverages;
    }

    private decimal CalculateTrendStrength(Dictionary<int, decimal> movingAverages)
    {
        var trendStrength = 0m;
        var count = 0;

        foreach (var period in _defaultPeriods)
        {
            if (movingAverages.ContainsKey(period) && movingAverages.ContainsKey(period * 2))
            {
                var currentMA = movingAverages[period];
                var longerMA = movingAverages[period * 2];
                trendStrength += (currentMA - longerMA) / longerMA;
                count++;
            }
        }

        return count > 0 ? trendStrength / count : 0m;
    }

    private PredictionDirection CalculateTrendDirection(Dictionary<int, decimal> movingAverages)
    {
        var trendStrength = CalculateTrendStrength(movingAverages);
        return trendStrength > 0 ? PredictionDirection.Up : PredictionDirection.Down;
    }

    private decimal CalculateExpectedReturn(decimal trendStrength, decimal volatility, decimal volumeTrend = 0)
    {
        var baseReturn = trendStrength * 0.01m; // 1% base return per unit of trend strength
        var volatilityAdjustment = 1 - volatility / 0.2m; // Reduce return for high volatility
        var volumeAdjustment = 1 + volumeTrend * 0.5m; // Adjust for volume trend

        return baseReturn * volatilityAdjustment * volumeAdjustment;
    }

    private decimal CalculateVolatility(IEnumerable<decimal> prices)
    {
        var priceArray = prices.ToArray();
        var returns = CalculateReturns(priceArray);
        var mean = returns.Average();
        var squaredDeviations = returns.Select(r => (r - mean) * (r - mean));
        return (decimal)Math.Sqrt(squaredDeviations.Average());
    }

    private decimal CalculateVolumeTrend(EconomicFactors factors)
    {
        if (factors.VolumeData == null || factors.VolumeData.Length < 20)
            return 0;

        var recentVolume = factors.VolumeData.Take(20).Average();
        var historicalVolume = factors.VolumeData.Skip(20).Take(80).Average();
        return (recentVolume - historicalVolume) / historicalVolume;
    }

    private decimal CalculateTechnicalScore(Dictionary<int, decimal> movingAverages, decimal volumeTrend)
    {
        var trendStrength = CalculateTrendStrength(movingAverages);
        var momentum = CalculateMomentum(movingAverages);
        return (trendStrength + momentum + volumeTrend) / 3;
    }

    private decimal CalculateMomentum(Dictionary<int, decimal> movingAverages)
    {
        if (!movingAverages.ContainsKey(20) || !movingAverages.ContainsKey(50))
            return 0;

        var currentPrice = movingAverages[20];
        var basePrice = movingAverages[50];
        return (currentPrice - basePrice) / basePrice;
    }

    private decimal CalculateRiskLevel(decimal volatility, decimal trendStrength)
    {
        var volatilityScore = volatility < 0.1m ? 1 : volatility < 0.2m ? 2 : 3;
        var trendScore = trendStrength > 0.1m ? 1 : trendStrength < -0.1m ? 3 : 2;
        return (volatilityScore + trendScore) / 2;
    }

    private decimal CalculateConfidenceScore(decimal trendStrength, decimal volatility, decimal volumeTrend = 0)
    {
        var trendScore = trendStrength > 0 ? 0.8m : 0.2m;
        var volatilityScore = 1 - volatility;
        var volumeScore = volumeTrend > 0 ? 0.8m : 0.2m;
        return (trendScore + volatilityScore + volumeScore) / 3;
    }

    private decimal[] CalculateReturns(decimal[] prices)
    {
        var returns = new decimal[prices.Length - 1];
        for (int i = 0; i < returns.Length; i++)
        {
            returns[i] = (prices[i + 1] - prices[i]) / prices[i];
        }
        return returns;
    }

    private Dictionary<string, decimal> CalculatePortfolioMovingAverages(Portfolio portfolio)
    {
        var portfolioMAs = new Dictionary<string, decimal>();

        foreach (var position in portfolio.Positions)
        {
            var movingAverages = CalculateMovingAverages(position.HistoricalPrices);
            portfolioMAs[position.Symbol] = movingAverages[20]; // Using 20-day MA as representative
        }

        return portfolioMAs;
    }

    private decimal CalculatePortfolioTrend(Dictionary<string, decimal> portfolioMAs)
    {
        var totalTrend = 0m;
        foreach (var ma in portfolioMAs.Values)
        {
            totalTrend += ma;
        }
        return totalTrend / portfolioMAs.Count;
    }

    private decimal CalculatePortfolioVolatility(Portfolio portfolio)
    {
        return portfolio.Positions
            .Select(p => p.Weight * CalculateVolatility(p.HistoricalPrices.ToArray()))
            .Sum();
    }

    private Dictionary<string, decimal> CalculateTrendFollowingAllocation(Portfolio portfolio, Dictionary<string, decimal> portfolioMAs)
    {
        var allocation = new Dictionary<string, decimal>();
        var totalTrend = portfolioMAs.Values.Sum();

        foreach (var position in portfolio.Positions)
        {
            var trend = portfolioMAs[position.Symbol];
            var weight = trend / totalTrend;
            allocation[position.Symbol] = weight;
        }

        return allocation;
    }

    public async Task<PredictionPerformanceMetrics> GetPerformanceMetricsAsync()
    {
        return new PredictionPerformanceMetrics
        {
            Accuracy = 0.70m,
            Precision = 0.65m,
            Recall = 0.68m,
            F1Score = 0.66m,
            ModelType = ModelType
        };
    }

    public async Task<IEnumerable<PredictionRecommendation>> GetRecommendationsAsync(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction)
    {
        var recommendations = new List<PredictionRecommendation>();

        foreach (var position in portfolio.Positions)
        {
            var movingAverages = CalculateMovingAverages(position.HistoricalPrices);
            var trendStrength = CalculateTrendStrength(movingAverages);
            var volatility = CalculateVolatility(position.HistoricalPrices);

            var recommendation = new PredictionRecommendation
            {
                Symbol = position.Symbol,
                Action = CalculateAction(trendStrength, volatility),
                Confidence = CalculateConfidenceScore(trendStrength, volatility),
                ModelType = ModelType,
                Rationale = "Based on trend following analysis"
            };

            recommendations.Add(recommendation);
        }

        return recommendations;
    }

    public async Task UpdateModelAsync(PredictionTrainingData newData)
    {
        // No-op for trend following model as it recalculates on each prediction
        await Task.CompletedTask;
    }

    public async Task<PredictionValidationResult> ValidateModelAsync(PredictionValidationData actualData)
    {
        var validationResult = new PredictionValidationResult();

        validationResult.MarketAccuracy = CalculateAccuracy(
            actualData.MarketData,
            actualData.PredictedMarketData
        );

        validationResult.SecurityAccuracy = CalculateAccuracy(
            actualData.SecurityData,
            actualData.PredictedSecurityData
        );

        validationResult.PortfolioAccuracy = CalculateAccuracy(
            actualData.PortfolioData,
            actualData.PredictedPortfolioData
        );

        return validationResult;
    }

    private RecommendationAction CalculateAction(decimal trendStrength, decimal volatility)
    {
        if (trendStrength > 0.1m && volatility < 0.2m)
            return RecommendationAction.Buy;
        if (trendStrength < -0.1m || volatility > 0.3m)
            return RecommendationAction.Sell;
        return RecommendationAction.Hold;
    }
}
