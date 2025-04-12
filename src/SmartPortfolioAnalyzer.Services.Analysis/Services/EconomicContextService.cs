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
/// Provides economic context analysis services
/// </summary>
/// <remarks>
/// Implements analysis of economic indicators, market impact assessment,
/// and economic condition prediction for portfolio management.
/// </remarks>
namespace SmartPortfolioAnalyzer.Services.Analysis.Services;

/// <summary>
/// Implementation of economic context analysis services
/// </summary>
public class EconomicContextService : IEconomicContextService
{
    private readonly Dictionary<EconomicIndicatorType, string> _indicatorSources;
    private readonly Dictionary<EconomicImpact, decimal> _impactWeights;

    /// <summary>
    /// Initializes a new instance of the <see cref="EconomicContextService"/> class.
    /// </summary>
    public EconomicContextService()
    {
        _indicatorSources = new Dictionary<EconomicIndicatorType, string>
        {
            { EconomicIndicatorType.GDP, "Bureau of Economic Analysis" },
            { EconomicIndicatorType.Inflation, "Bureau of Labor Statistics" },
            { EconomicIndicatorType.InterestRate, "Federal Reserve" },
            { EconomicIndicatorType.Unemployment, "Bureau of Labor Statistics" },
            { EconomicIndicatorType.ConsumerConfidence, "Conference Board" }
        };

        _impactWeights = new Dictionary<EconomicImpact, decimal>
        {
            { EconomicImpact.Positive, 0.3m },
            { EconomicImpact.Negative, -0.3m },
            { EconomicImpact.Neutral, 0m }
        };
    }

    /// <summary>
    /// Retrieves current economic indicators
    /// </summary>
    /// <returns>A collection of current economic indicators</returns>
    public async Task<IEnumerable<EconomicIndicator>> GetCurrentIndicatorsAsync()
    {
        var indicators = new List<EconomicIndicator>();
        
        // TODO: Implement actual data retrieval from sources
        // For now, return mock data
        indicators.Add(new EconomicIndicator
        {
            Type = EconomicIndicatorType.GDP,
            Value = 2.5m,
            Date = DateTime.UtcNow,
            Source = _indicatorSources[EconomicIndicatorType.GDP],
            Impact = EconomicImpact.Positive
        });

        indicators.Add(new EconomicIndicator
        {
            Type = EconomicIndicatorType.Inflation,
            Value = 3.2m,
            Date = DateTime.UtcNow,
            Source = _indicatorSources[EconomicIndicatorType.Inflation],
            Impact = EconomicImpact.Negative
        });

        return indicators;
    }

    /// <summary>
    /// Analyzes economic indicators for market impact
    /// </summary>
    /// <param name="indicators">The economic indicators to analyze</param>
    /// <returns>An analysis of market impact</returns>
    public async Task<MarketImpactAnalysis> AnalyzeMarketImpactAsync(
        IEnumerable<EconomicIndicator> indicators)
    {
        var analysis = new MarketImpactAnalysis();
        var indicatorList = indicators.ToList();

        // Calculate overall impact score
        var impactScore = indicatorList.Sum(i => 
            _impactWeights[i.Impact] * CalculateImpactWeight(i.Type));

        // Analyze by indicator type
        var grouped = indicatorList.GroupBy(i => i.Type);
        foreach (var group in grouped)
        {
            analysis.AddIndicatorImpact(group.Key, 
                new IndicatorImpact
                {
                    Type = group.Key,
                    Score = group.Sum(i => _impactWeights[i.Impact]),
                    Trend = CalculateTrend(group)
                });
        }

        analysis.OverallImpact = impactScore;
        analysis.MarketSentiment = CalculateMarketSentiment(impactScore);

        return analysis;
    }

    /// <summary>
    /// Calculates economic sentiment score
    /// </summary>
    /// <param name="indicators">The economic indicators to analyze</param>
    /// <returns>The economic sentiment score</returns>
    public async Task<decimal> CalculateEconomicSentimentAsync(
        IEnumerable<EconomicIndicator> indicators)
    {
        var indicatorList = indicators.ToList();
        if (!indicatorList.Any()) return 0;

        // Calculate weighted sentiment
        var sentiment = indicatorList.Sum(i => 
            i.Value * CalculateSentimentWeight(i.Type) * _impactWeights[i.Impact]);

        // Normalize to -100 to 100 scale
        return Math.Max(-100m, Math.Min(100m, sentiment));
    }

    /// <summary>
    /// Predicts economic conditions
    /// </summary>
    /// <param name="historicalData">Historical economic data</param>
    /// <returns>An economic condition prediction</returns>
    public async Task<EconomicConditionPrediction> PredictEconomicConditionsAsync(
        IEnumerable<EconomicIndicator> historicalData)
    {
        var prediction = new EconomicConditionPrediction();
        var data = historicalData.ToList();

        // Calculate trend for each indicator type
        var grouped = data.GroupBy(i => i.Type);
        foreach (var group in grouped)
        {
            var trend = CalculateTrend(group);
            var volatility = CalculateVolatility(group.Select(i => i.Value));

            prediction.AddPrediction(group.Key, new EconomicTrendPrediction
            {
                Type = group.Key,
                Trend = trend,
                Volatility = volatility,
                Confidence = CalculatePredictionConfidence(trend, volatility)
            });
        }

        return prediction;
    }

    /// <summary>
    /// Gets historical economic data
    /// </summary>
    /// <param name="indicatorType">Type of economic indicator</param>
    /// <param name="startDate">Start date for data</param>
    /// <param name="endDate">End date for data</param>
    /// <returns>Historical economic data</returns>
    public async Task<IEnumerable<EconomicIndicator>> GetHistoricalDataAsync(
        EconomicIndicatorType indicatorType,
        DateTime startDate,
        DateTime endDate)
    {
        // TODO: Implement actual data retrieval from historical sources
        // For now, return mock data
        var indicators = new List<EconomicIndicator>();
        var current = endDate;

        while (current >= startDate)
        {
            indicators.Add(new EconomicIndicator
            {
                Type = indicatorType,
                Value = GenerateMockValue(indicatorType),
                Date = current,
                Source = _indicatorSources[indicatorType],
                Impact = CalculateImpact(indicatorType, GenerateMockValue(indicatorType))
            });

            current = current.AddMonths(-1);
        }

        return indicators;
    }

    /// <summary>
    /// Calculates correlation between economic indicators and market performance
    /// </summary>
    /// <param name="economicData">Economic indicator data</param>
    /// <param name="marketData">Market performance data</param>
    /// <returns>Correlation analysis results</returns>
    public async Task<CorrelationAnalysis> CalculateCorrelationAsync(
        IEnumerable<EconomicIndicator> economicData,
        IEnumerable<decimal> marketData)
    {
        var analysis = new CorrelationAnalysis();
        var economicList = economicData.ToList();
        var marketList = marketData.ToList();

        // Calculate correlation for each indicator type
        var grouped = economicList.GroupBy(e => e.Type);
        foreach (var group in grouped)
        {
            var indicatorValues = group.Select(e => e.Value).ToList();
            var correlation = CalculatePearsonCorrelation(indicatorValues, marketList);

            analysis.AddCorrelation(group.Key, new IndicatorCorrelation
            {
                Type = group.Key,
                Correlation = correlation,
                Significance = CalculateSignificance(correlation)
            });
        }

        return analysis;
    }

    /// <summary>
    /// Gets economic calendar events
    /// </summary>
    /// <param name="startDate">Start date for events</param>
    /// <param name="endDate">End date for events</param>
    /// <returns>Economic calendar events</returns>
    public async Task<IEnumerable<EconomicEvent>> GetEconomicCalendarAsync(
        DateTime startDate,
        DateTime endDate)
    {
        // TODO: Implement actual calendar data retrieval
        // For now, return mock data
        var events = new List<EconomicEvent>();
        var current = startDate;

        while (current <= endDate)
        {
            events.Add(new EconomicEvent
            {
                Date = current,
                Type = EconomicEventType.FOMCMeeting,
                Impact = EconomicImpact.Positive,
                Description = "Federal Open Market Committee Meeting"
            });

            current = current.AddMonths(1);
        }

        return events;
    }

    /// <summary>
    /// Analyzes impact of economic events
    /// </summary>
    /// <param name="events">Economic events to analyze</param>
    /// <param name="marketData">Market data around event times</param>
    /// <returns>Event impact analysis</returns>
    public async Task<EventImpactAnalysis> AnalyzeEventImpactAsync(
        IEnumerable<EconomicEvent> events,
        IEnumerable<decimal> marketData)
    {
        var analysis = new EventImpactAnalysis();
        var eventData = events.ToList();
        var marketList = marketData.ToList();

        foreach (var @event in eventData)
        {
            var impact = CalculateEventImpact(@event, marketList);
            analysis.AddEventImpact(new EventImpact
            {
                Event = @event,
                MarketImpact = impact,
                Significance = CalculateImpactSignificance(impact)
            });
        }

        return analysis;
    }

    // Helper methods
    private decimal CalculateImpactWeight(EconomicIndicatorType type)
    {
        return type switch
        {
            EconomicIndicatorType.GDP => 0.3m,
            EconomicIndicatorType.Inflation => 0.25m,
            EconomicIndicatorType.InterestRate => 0.2m,
            EconomicIndicatorType.Unemployment => 0.15m,
            EconomicIndicatorType.ConsumerConfidence => 0.1m,
            _ => 0.1m
        };
    }

    private decimal CalculateSentimentWeight(EconomicIndicatorType type)
    {
        return CalculateImpactWeight(type);
    }

    private TrendDirection CalculateTrend(IEnumerable<EconomicIndicator> indicators)
    {
        var values = indicators.Select(i => i.Value).ToList();
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

    private decimal CalculateVolatility(IEnumerable<decimal> values)
    {
        var mean = values.Average();
        var squaredDeviations = values.Select(x => Math.Pow((double)(x - mean), 2));
        var variance = (decimal)squaredDeviations.Average();
        return (decimal)Math.Sqrt((double)variance);
    }

    private decimal CalculatePredictionConfidence(TrendDirection trend, decimal volatility)
    {
        var confidence = 0.5m; // Base confidence

        // Adjust confidence based on factors
        if (volatility < 0.02m) confidence += 0.2m; // Low volatility increases confidence
        if (trend != TrendDirection.Unknown) confidence += 0.1m; // Clear trend increases confidence

        return Math.Min(confidence, 1m);
    }

    private decimal GenerateMockValue(EconomicIndicatorType type)
    {
        // Generate realistic mock values based on indicator type
        return type switch
        {
            EconomicIndicatorType.GDP => 2.5m + (decimal)(new Random().NextDouble() - 0.5),
            EconomicIndicatorType.Inflation => 3.2m + (decimal)(new Random().NextDouble() - 0.5),
            EconomicIndicatorType.InterestRate => 5.0m + (decimal)(new Random().NextDouble() - 0.5),
            EconomicIndicatorType.Unemployment => 3.5m + (decimal)(new Random().NextDouble() - 0.5),
            EconomicIndicatorType.ConsumerConfidence => 100m + (decimal)(new Random().NextDouble() - 0.5),
            _ => 0m
        };
    }

    private EconomicImpact CalculateImpact(EconomicIndicatorType type, decimal value)
    {
        return type switch
        {
            EconomicIndicatorType.GDP when value > 2.5m => EconomicImpact.Positive,
            EconomicIndicatorType.GDP when value < 2.0m => EconomicImpact.Negative,
            EconomicIndicatorType.Inflation when value > 4.0m => EconomicImpact.Negative,
            EconomicIndicatorType.Inflation when value < 2.0m => EconomicImpact.Positive,
            _ => EconomicImpact.Neutral
        };
    }

    private MarketSentiment CalculateMarketSentiment(decimal impactScore)
    {
        return impactScore switch
        {
            > 0.5m => MarketSentiment.Bullish,
            < -0.5m => MarketSentiment.Bearish,
            _ => MarketSentiment.Neutral
        };
    }

    private decimal CalculatePearsonCorrelation(
        IEnumerable<decimal> x,
        IEnumerable<decimal> y)
    {
        var xList = x.ToList();
        var yList = y.ToList();
        var n = Math.Min(xList.Count, yList.Count);

        var xMean = xList.Average();
        var yMean = yList.Average();

        var numerator = xList.Zip(yList, (a, b) => (a - xMean) * (b - yMean)).Sum();
        var xStdDev = Math.Sqrt(xList.Sum(x => Math.Pow(x - xMean, 2)));
        var yStdDev = Math.Sqrt(yList.Sum(y => Math.Pow(y - yMean, 2)));

        return n == 0 ? 0 : (decimal)(numerator / (xStdDev * yStdDev));
    }

    private decimal CalculateSignificance(decimal correlation)
    {
        return Math.Abs(correlation) switch
        {
            > 0.7m => 0.95m,
            > 0.5m => 0.85m,
            > 0.3m => 0.75m,
            > 0.1m => 0.65m,
            _ => 0.5m
        };
    }

    private decimal CalculateEventImpact(
        EconomicEvent @event,
        IEnumerable<decimal> marketData)
    {
        var marketList = marketData.ToList();
        var eventIndex = marketList.Count - 1;
        
        // Get data before and after event
        var before = marketList.Skip(Math.Max(0, eventIndex - 5)).Take(5);
        var after = marketList.Skip(eventIndex).Take(5);

        // Calculate impact
        var beforeMean = before.Average();
        var afterMean = after.Average();
        return (afterMean - beforeMean) / beforeMean;
    }

    private decimal CalculateImpactSignificance(decimal impact)
    {
        return Math.Abs(impact) switch
        {
            > 0.05m => 0.95m,
            > 0.03m => 0.85m,
            > 0.01m => 0.75m,
            > 0.005m => 0.65m,
            _ => 0.5m
        };
    }
}
