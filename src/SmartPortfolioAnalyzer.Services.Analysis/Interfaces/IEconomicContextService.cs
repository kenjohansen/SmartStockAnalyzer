/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System;
using System.Threading.Tasks;
using SmartPortfolioAnalyzer.Core.Models;

/// <summary>
/// Interface for economic context analysis services
/// </summary>
/// <remarks>
/// Provides methods for analyzing economic indicators and their impact on portfolio decisions.
/// </remarks>
namespace SmartPortfolioAnalyzer.Services.Analysis.Interfaces;

/// <summary>
/// Defines methods for economic context analysis
/// </summary>
public interface IEconomicContextService
{
    /// <summary>
    /// Retrieves current economic indicators
    /// </summary>
    /// <returns>A collection of current economic indicators</returns>
    Task<IEnumerable<EconomicIndicator>> GetCurrentIndicatorsAsync();

    /// <summary>
    /// Analyzes economic indicators for market impact
    /// </summary>
    /// <param name="indicators">The economic indicators to analyze</param>
    /// <returns>An analysis of market impact</returns>
    Task<MarketImpactAnalysis> AnalyzeMarketImpactAsync(
        IEnumerable<EconomicIndicator> indicators);

    /// <summary>
    /// Calculates economic sentiment score
    /// </summary>
    /// <param name="indicators">The economic indicators to analyze</param>
    /// <returns>The economic sentiment score</returns>
    Task<decimal> CalculateEconomicSentimentAsync(
        IEnumerable<EconomicIndicator> indicators);

    /// <summary>
    /// Predicts economic conditions
    /// </summary>
    /// <param name="historicalData">Historical economic data</param>
    /// <returns>An economic condition prediction</returns>
    Task<EconomicConditionPrediction> PredictEconomicConditionsAsync(
        IEnumerable<EconomicIndicator> historicalData);

    /// <summary>
    /// Gets historical economic data
    /// </summary>
    /// <param name="indicatorType">Type of economic indicator</param>
    /// <param name="startDate">Start date for data</param>
    /// <param name="endDate">End date for data</param>
    /// <returns>Historical economic data</returns>
    Task<IEnumerable<EconomicIndicator>> GetHistoricalDataAsync(
        EconomicIndicatorType indicatorType,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Calculates correlation between economic indicators and market performance
    /// </summary>
    /// <param name="economicData">Economic indicator data</param>
    /// <param name="marketData">Market performance data</param>
    /// <returns>Correlation analysis results</returns>
    Task<CorrelationAnalysis> CalculateCorrelationAsync(
        IEnumerable<EconomicIndicator> economicData,
        IEnumerable<decimal> marketData);

    /// <summary>
    /// Gets economic calendar events
    /// </summary>
    /// <param name="startDate">Start date for events</param>
    /// <param name="endDate">End date for events</param>
    /// <returns>Economic calendar events</returns>
    Task<IEnumerable<EconomicEvent>> GetEconomicCalendarAsync(
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Analyzes impact of economic events
    /// </summary>
    /// <param name="events">Economic events to analyze</param>
    /// <param name="marketData">Market data around event times</param>
    /// <returns>Event impact analysis</returns>
    Task<EventImpactAnalysis> AnalyzeEventImpactAsync(
        IEnumerable<EconomicEvent> events,
        IEnumerable<decimal> marketData);
}
