/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartPortfolioAnalyzer.Core.Models;

/// <summary>
/// Interface for portfolio analysis services
/// </summary>
/// <remarks>
/// Provides methods for technical analysis, economic context analysis,
/// and optimization calculations for portfolio management.
/// </remarks>
namespace SmartPortfolioAnalyzer.Services.Analysis.Interfaces;

/// <summary>
/// Defines methods for portfolio analysis
/// </summary>
public interface IAnalysisService
{
    /// <summary>
    /// Calculates technical indicators for a security
    /// </summary>
    /// <param name="symbol">The security symbol</param>
    /// <param name="timeSeries">The price time series</param>
    /// <returns>A collection of technical indicators</returns>
    Task<IEnumerable<TechnicalIndicator>> CalculateTechnicalIndicatorsAsync(
        string symbol,
        IEnumerable<decimal> timeSeries);

    /// <summary>
    /// Calculates optimal position size based on risk parameters
    /// </summary>
    /// <param name="portfolioValue">The total portfolio value</param>
    /// <param name="riskPerTrade">The risk per trade percentage</param>
    /// <param name="stopLoss">The stop loss percentage</param>
    /// <returns>The optimal position size</returns>
    Task<decimal> CalculatePositionSizeAsync(
        decimal portfolioValue,
        decimal riskPerTrade,
        decimal stopLoss);

    /// <summary>
    /// Analyzes market cycle phase
    /// </summary>
    /// <param name="marketData">The market data series</param>
    /// <returns>The current market cycle phase</returns>
    Task<MarketCyclePhase> AnalyzeMarketCycleAsync(
        IEnumerable<decimal> marketData);

    /// <summary>
    /// Calculates optimal portfolio weights
    /// </summary>
    /// <param name="portfolio">The portfolio to optimize</param>
    /// <param name="riskTolerance">The risk tolerance level</param>
    /// <returns>A dictionary of optimized weights</returns>
    Task<Dictionary<string, decimal>> CalculateOptimalWeightsAsync(
        Portfolio portfolio,
        decimal riskTolerance);

    /// <summary>
    /// Generates rebalancing recommendations
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="targetAllocation">The target asset allocation</param>
    /// <returns>A collection of rebalancing recommendations</returns>
    Task<IEnumerable<RebalancingRecommendation>> GenerateRebalancingRecommendationsAsync(
        Portfolio portfolio,
        Dictionary<string, decimal> targetAllocation);

    /// <summary>
    /// Predicts future market conditions
    /// </summary>
    /// <param name="historicalData">The historical market data</param>
    /// <returns>A prediction of future market conditions</returns>
    Task<MarketPrediction> PredictMarketConditionsAsync(
        IEnumerable<decimal> historicalData);
}
