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

/// <summary>
/// Provides portfolio analysis services
/// </summary>
/// <remarks>
/// Implements technical analysis, economic context analysis,
/// and optimization calculations for portfolio management.
/// </remarks>
namespace SmartPortfolioAnalyzer.Services.Analysis.Services;

/// <summary>
/// Implementation of portfolio analysis services
/// </summary>
public class AnalysisService : IAnalysisService
{
    /// <summary>
    /// Calculates technical indicators for a security
    /// </summary>
    /// <param name="symbol">The security symbol</param>
    /// <param name="timeSeries">The price time series</param>
    /// <returns>A collection of technical indicators</returns>
    public async Task<IEnumerable<TechnicalIndicator>> CalculateTechnicalIndicatorsAsync(
        string symbol,
        IEnumerable<decimal> timeSeries)
    {
        var indicators = new List<TechnicalIndicator>();
        
        // Calculate moving averages
        var sma = CalculateSimpleMovingAverage(timeSeries, 20);
        var ema = CalculateExponentialMovingAverage(timeSeries, 20);
        
        // Calculate RSI
        var rsi = CalculateRSI(timeSeries, 14);
        
        // Calculate MACD
        var macd = CalculateMACD(timeSeries);
        
        indicators.AddRange(new[]
        {
            new TechnicalIndicator { Name = "SMA", Value = sma, Symbol = symbol },
            new TechnicalIndicator { Name = "EMA", Value = ema, Symbol = symbol },
            new TechnicalIndicator { Name = "RSI", Value = rsi, Symbol = symbol },
            new TechnicalIndicator { Name = "MACD", Value = macd, Symbol = symbol }
        });

        return indicators;
    }

    /// <summary>
    /// Calculates optimal position size based on risk parameters
    /// </summary>
    /// <param name="portfolioValue">The total portfolio value</param>
    /// <param name="riskPerTrade">The risk per trade percentage</param>
    /// <param name="stopLoss">The stop loss percentage</param>
    /// <returns>The optimal position size</returns>
    public async Task<decimal> CalculatePositionSizeAsync(
        decimal portfolioValue,
        decimal riskPerTrade,
        decimal stopLoss)
    {
        // Calculate position size using Kelly Criterion
        var positionSize = portfolioValue * riskPerTrade / stopLoss;
        return Math.Min(positionSize, portfolioValue * 0.1m); // Max 10% of portfolio
    }

    /// <summary>
    /// Analyzes market cycle phase
    /// </summary>
    /// <param name="marketData">The market data series</param>
    /// <returns>The current market cycle phase</returns>
    public async Task<MarketCyclePhase> AnalyzeMarketCycleAsync(
        IEnumerable<decimal> marketData)
    {
        var data = marketData.ToList();
        if (data.Count < 20) return MarketCyclePhase.Unknown;

        var recentTrend = CalculateTrend(data.TakeLast(20));
        var longTrend = CalculateTrend(data);

        return recentTrend switch
        {
            TrendDirection.Up when longTrend == TrendDirection.Up => MarketCyclePhase.Bullish,
            TrendDirection.Down when longTrend == TrendDirection.Down => MarketCyclePhase.Bearish,
            TrendDirection.Up when longTrend == TrendDirection.Down => MarketCyclePhase.Recovery,
            TrendDirection.Down when longTrend == TrendDirection.Up => MarketCyclePhase.Correction,
            _ => MarketCyclePhase.Unknown
        };
    }

    /// <summary>
    /// Calculates optimal portfolio weights
    /// </summary>
    /// <param name="portfolio">The portfolio to optimize</param>
    /// <param name="riskTolerance">The risk tolerance level</param>
    /// <returns>A dictionary of optimized weights</returns>
    public async Task<Dictionary<string, decimal>> CalculateOptimalWeightsAsync(
        Portfolio portfolio,
        decimal riskTolerance)
    {
        var totalValue = portfolio.Positions.Sum(p => p.MarketValue);
        var weights = new Dictionary<string, decimal>();

        foreach (var position in portfolio.Positions)
        {
            // Calculate risk-adjusted weight
            var volatility = CalculatePositionVolatility(position);
            var weight = CalculateRiskAdjustedWeight(position, volatility, riskTolerance);
            
            weights[position.Symbol] = weight;
        }

        // Normalize weights to sum to 1
        var totalWeight = weights.Values.Sum();
        foreach (var symbol in weights.Keys)
        {
            weights[symbol] /= totalWeight;
        }

        return weights;
    }

    /// <summary>
    /// Generates rebalancing recommendations
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="targetAllocation">The target asset allocation</param>
    /// <returns>A collection of rebalancing recommendations</returns>
    public async Task<IEnumerable<RebalancingRecommendation>> GenerateRebalancingRecommendationsAsync(
        Portfolio portfolio,
        Dictionary<string, decimal> targetAllocation)
    {
        var recommendations = new List<RebalancingRecommendation>();
        var currentWeights = CalculateCurrentWeights(portfolio);

        foreach (var target in targetAllocation)
        {
            if (currentWeights.TryGetValue(target.Key, out var currentWeight))
            {
                var deviation = Math.Abs(target.Value - currentWeight);
                var threshold = 0.05m; // 5% deviation threshold

                if (deviation > threshold)
                {
                    recommendations.Add(new RebalancingRecommendation
                    {
                        Symbol = target.Key,
                        CurrentWeight = currentWeight,
                        TargetWeight = target.Value,
                        Deviation = deviation,
                        Action = target.Value > currentWeight ? RebalanceAction.Buy : RebalanceAction.Sell
                    });
                }
            }
        }

        return recommendations;
    }

    /// <summary>
    /// Predicts future market conditions
    /// </summary>
    /// <param name="historicalData">The historical market data</param>
    /// <returns>A prediction of future market conditions</returns>
    public async Task<MarketPrediction> PredictMarketConditionsAsync(
        IEnumerable<decimal> historicalData)
    {
        var data = historicalData.ToList();
        if (data.Count < 100) return new MarketPrediction { Confidence = 0.3m };

        var trend = CalculateTrend(data);
        var volatility = CalculateVolatility(data);
        var momentum = CalculateMomentum(data);

        return new MarketPrediction
        {
            Trend = trend,
            Volatility = volatility,
            Momentum = momentum,
            Confidence = CalculatePredictionConfidence(trend, volatility, momentum)
        };
    }

    // Helper methods
    private decimal CalculateSimpleMovingAverage(IEnumerable<decimal> data, int period)
    {
        var values = data.ToList();
        if (values.Count < period) return 0;
        return values.TakeLast(period).Average();
    }

    private decimal CalculateExponentialMovingAverage(IEnumerable<decimal> data, int period)
    {
        var values = data.ToList();
        if (values.Count < period) return 0;
        var multiplier = 2m / (period + 1);
        var ema = values[0];

        for (int i = 1; i < values.Count; i++)
        {
            ema = (values[i] - ema) * multiplier + ema;
        }

        return ema;
    }

    private decimal CalculateRSI(IEnumerable<decimal> data, int period)
    {
        var values = data.ToList();
        if (values.Count < period) return 0;

        var gains = 0m;
        var losses = 0m;
        var previous = values[0];

        for (int i = 1; i < period; i++)
        {
            var change = values[i] - previous;
            if (change > 0)
                gains += change;
            else
                losses -= change;
            previous = values[i];
        }

        var rs = gains / losses;
        return 100 - (100 / (1 + rs));
    }

    private (decimal, decimal) CalculateMACD(IEnumerable<decimal> data)
    {
        var values = data.ToList();
        if (values.Count < 26) return (0, 0);

        var ema12 = CalculateExponentialMovingAverage(values, 12);
        var ema26 = CalculateExponentialMovingAverage(values, 26);
        var macd = ema12 - ema26;
        var signal = CalculateExponentialMovingAverage(new[] { macd }, 9);

        return (macd, signal);
    }

    private TrendDirection CalculateTrend(IEnumerable<decimal> data)
    {
        var values = data.ToList();
        if (values.Count < 2) return TrendDirection.Unknown;

        var start = values.First();
        var end = values.Last();
        var change = (end - start) / start;

        return change switch
        {
            > 0.01m => TrendDirection.Up,
            < -0.01m => TrendDirection.Down,
            _ => TrendDirection.Sideways
        };
    }

    private decimal CalculateVolatility(IEnumerable<decimal> data)
    {
        var values = data.ToList();
        if (values.Count < 2) return 0;

        var mean = values.Average();
        var squaredDeviations = values.Select(x => Math.Pow((double)(x - mean), 2));
        var variance = (decimal)squaredDeviations.Average();
        return (decimal)Math.Sqrt((double)variance);
    }

    private decimal CalculateMomentum(IEnumerable<decimal> data)
    {
        var values = data.ToList();
        if (values.Count < 14) return 0;

        var current = values.Last();
        var previous = values[values.Count - 14];
        return (current - previous) / previous;
    }

    private decimal CalculatePredictionConfidence(
        TrendDirection trend,
        decimal volatility,
        decimal momentum)
    {
        var confidence = 0.5m; // Base confidence

        // Adjust confidence based on factors
        if (volatility < 0.02m) confidence += 0.2m; // Low volatility increases confidence
        if (Math.Abs(momentum) > 0.05m) confidence += 0.1m; // Strong momentum increases confidence
        if (trend != TrendDirection.Unknown) confidence += 0.1m; // Clear trend increases confidence

        return Math.Min(confidence, 1m);
    }

    private Dictionary<string, decimal> CalculateCurrentWeights(Portfolio portfolio)
    {
        var totalValue = portfolio.Positions.Sum(p => p.MarketValue);
        return portfolio.Positions.ToDictionary(
            p => p.Symbol,
            p => p.MarketValue / totalValue
        );
    }

    private decimal CalculatePositionVolatility(PortfolioPosition position)
    {
        // TODO: Implement actual volatility calculation using historical data
        return 0.05m; // Default volatility
    }

    private decimal CalculateRiskAdjustedWeight(
        PortfolioPosition position,
        decimal volatility,
        decimal riskTolerance)
    {
        // Calculate weight based on risk-adjusted formula
        return riskTolerance / (volatility * Math.Sqrt(252));
    }
}
