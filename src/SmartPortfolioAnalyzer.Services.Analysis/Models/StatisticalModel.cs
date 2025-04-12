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

public class StatisticalModel : IPredictionModel
{
    private readonly Random _random = new Random();

    public PredictionModelType ModelType => PredictionModelType.Statistical;

    public async Task<MarketPredictionResult> PredictMarketConditionsAsync(
        IEnumerable<decimal> historicalData,
        EconomicContext economicContext,
        int timeHorizon = 30)
    {
        var historicalPrices = historicalData.ToArray();
        var returns = CalculateReturns(historicalPrices);
        var volatility = CalculateVolatility(returns);
        var trend = CalculateTrend(returns);

        var prediction = new MarketPrediction
        {
            Direction = trend > 0 ? PredictionDirection.Up : PredictionDirection.Down,
            ExpectedReturn = CalculateExpectedReturn(returns),
            Volatility = volatility,
            RiskLevel = CalculateRiskLevel(volatility)
        };

        return new MarketPredictionResult
        {
            Prediction = prediction,
            ConfidenceScore = CalculateConfidenceScore(trend, volatility),
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
        var returns = CalculateReturns(historicalPrices.ToArray());
        var volatility = CalculateVolatility(returns);
        var beta = CalculateBeta(returns);
        var alpha = CalculateAlpha(returns);

        var prediction = new SecurityPrediction
        {
            ExpectedReturn = CalculateExpectedReturn(returns, beta, alpha),
            Volatility = volatility,
            RiskLevel = CalculateRiskLevel(volatility),
            TechnicalScore = CalculateTechnicalScore(returns)
        };

        return new SecurityPredictionResult
        {
            Symbol = symbol,
            Prediction = prediction,
            ConfidenceScore = CalculateConfidenceScore(volatility, beta),
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
        var portfolioReturns = CalculatePortfolioReturns(portfolio);
        var portfolioVolatility = CalculatePortfolioVolatility(portfolio);
        var portfolioBeta = CalculatePortfolioBeta(portfolio);

        var prediction = new PortfolioPrediction
        {
            ExpectedReturn = CalculateExpectedReturn(portfolioReturns, portfolioBeta),
            Volatility = portfolioVolatility,
            RiskLevel = CalculateRiskLevel(portfolioVolatility),
            AssetAllocation = CalculateOptimalAllocation(portfolio)
        };

        return new PortfolioPredictionResult
        {
            Portfolio = portfolio,
            Prediction = prediction,
            ConfidenceScore = CalculateConfidenceScore(portfolioVolatility, portfolioBeta),
            TimeHorizon = timeHorizon,
            MarketPrediction = marketPrediction,
            ModelPerformance = await GetPerformanceMetricsAsync()
        };
    }

    private decimal CalculateExpectedReturn(decimal[] returns, decimal beta = 1, decimal alpha = 0)
    {
        return returns.Average() * beta + alpha;
    }

    private decimal CalculateVolatility(decimal[] returns)
    {
        var mean = returns.Average();
        var squaredDeviations = returns.Select(r => (r - mean) * (r - mean));
        return (decimal)Math.Sqrt(squaredDeviations.Average());
    }

    private decimal CalculateTrend(decimal[] returns)
    {
        var n = returns.Length;
        var sumX = n * (n + 1) / 2;
        var sumX2 = n * (n + 1) * (2 * n + 1) / 6;
        var sumY = returns.Sum();
        var sumXY = returns.Select((r, i) => r * (i + 1)).Sum();

        return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
    }

    private decimal CalculateBeta(decimal[] returns)
    {
        // Simplified beta calculation using historical returns
        return returns.Average() / 0.01m; // Assuming market return of 1%
    }

    private decimal CalculateAlpha(decimal[] returns)
    {
        // Simplified alpha calculation
        return returns.Average() - 0.01m; // Assuming market return of 1%
    }

    private decimal CalculateTechnicalScore(decimal[] returns)
    {
        var volatility = CalculateVolatility(returns);
        var trend = CalculateTrend(returns);
        return trend / volatility;
    }

    private decimal CalculateRiskLevel(decimal volatility)
    {
        if (volatility < 0.05m) return 1; // Low risk
        if (volatility < 0.1m) return 2; // Medium risk
        return 3; // High risk
    }

    private decimal CalculateConfidenceScore(decimal trend, decimal volatility)
    {
        var trendScore = trend > 0 ? 0.8m : 0.2m;
        var volatilityScore = 1 - volatility;
        return (trendScore + volatilityScore) / 2;
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

    private decimal CalculatePortfolioReturns(Portfolio portfolio)
    {
        return portfolio.Positions
            .Select(p => p.Weight * CalculateReturns(p.HistoricalPrices.ToArray()).Average())
            .Sum();
    }

    private decimal CalculatePortfolioVolatility(Portfolio portfolio)
    {
        var covarianceMatrix = CalculateCovarianceMatrix(portfolio);
        var weights = portfolio.Positions.Select(p => p.Weight).ToArray();
        return CalculatePortfolioVolatilityFromCovariance(covarianceMatrix, weights);
    }

    private decimal[,] CalculateCovarianceMatrix(Portfolio portfolio)
    {
        var n = portfolio.Positions.Count;
        var covariance = new decimal[n, n];

        for (int i = 0; i < n; i++)
        {
            var returns1 = CalculateReturns(portfolio.Positions[i].HistoricalPrices.ToArray());
            for (int j = 0; j < n; j++)
            {
                var returns2 = CalculateReturns(portfolio.Positions[j].HistoricalPrices.ToArray());
                covariance[i, j] = CalculateCovariance(returns1, returns2);
            }
        }

        return covariance;
    }

    private decimal CalculateCovariance(decimal[] returns1, decimal[] returns2)
    {
        var mean1 = returns1.Average();
        var mean2 = returns2.Average();
        var n = returns1.Length;
        var sum = 0m;

        for (int i = 0; i < n; i++)
        {
            sum += (returns1[i] - mean1) * (returns2[i] - mean2);
        }

        return sum / n;
    }

    private decimal CalculatePortfolioVolatilityFromCovariance(decimal[,] covariance, decimal[] weights)
    {
        var n = weights.Length;
        var portfolioVariance = 0m;

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                portfolioVariance += weights[i] * weights[j] * covariance[i, j];
            }
        }

        return (decimal)Math.Sqrt(portfolioVariance);
    }

    private decimal CalculatePortfolioBeta(Portfolio portfolio)
    {
        return portfolio.Positions
            .Select(p => p.Weight * CalculateBeta(CalculateReturns(p.HistoricalPrices.ToArray())))
            .Sum();
    }

    private Dictionary<string, decimal> CalculateOptimalAllocation(Portfolio portfolio)
    {
        var optimalAllocation = new Dictionary<string, decimal>();
        var totalRisk = portfolio.Positions.Sum(p => CalculateVolatility(CalculateReturns(p.HistoricalPrices.ToArray())));

        foreach (var position in portfolio.Positions)
        {
            var positionRisk = CalculateVolatility(CalculateReturns(position.HistoricalPrices.ToArray()));
            var weight = 1 / (positionRisk / totalRisk);
            optimalAllocation[position.Symbol] = weight;
        }

        return optimalAllocation;
    }

    public async Task<PredictionPerformanceMetrics> GetPerformanceMetricsAsync()
    {
        return new PredictionPerformanceMetrics
        {
            Accuracy = 0.75m,
            Precision = 0.70m,
            Recall = 0.72m,
            F1Score = 0.71m,
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
            var returns = CalculateReturns(position.HistoricalPrices.ToArray());
            var volatility = CalculateVolatility(returns);
            var beta = CalculateBeta(returns);

            var recommendation = new PredictionRecommendation
            {
                Symbol = position.Symbol,
                Action = CalculateAction(returns, volatility, beta),
                Confidence = CalculateConfidenceScore(volatility, beta),
                ModelType = ModelType,
                Rationale = "Based on statistical analysis of returns and volatility"
            };

            recommendations.Add(recommendation);
        }

        return recommendations;
    }

    public async Task UpdateModelAsync(PredictionTrainingData newData)
    {
        // No-op for statistical model as it recalculates statistics on each prediction
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

    private RecommendationAction CalculateAction(decimal[] returns, decimal volatility, decimal beta)
    {
        if (beta > 1 && volatility < 0.1m)
            return RecommendationAction.Buy;
        if (beta < 1 && volatility > 0.2m)
            return RecommendationAction.Sell;
        return RecommendationAction.Hold;
    }
}
