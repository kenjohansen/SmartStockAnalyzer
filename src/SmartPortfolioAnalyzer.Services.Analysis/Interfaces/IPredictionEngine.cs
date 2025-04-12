/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System;
using System.Threading.Tasks;
using SmartPortfolioAnalyzer.Core.Models;

/// <summary>
/// Interface for market prediction services
/// </summary>
/// <remarks>
/// Provides methods for predicting market conditions, security performance,
/// and portfolio behavior using various prediction models.
/// </remarks>
namespace SmartPortfolioAnalyzer.Services.Analysis.Interfaces;

/// <summary>
/// Defines methods for market prediction services
/// </summary>
public interface IPredictionEngine
{
    /// <summary>
    /// Predicts future market conditions
    /// </summary>
    /// <param name="historicalData">Historical market data</param>
    /// <param name="economicContext">Current economic context</param>
    /// <param name="timeHorizon">Prediction time horizon in days</param>
    /// <returns>A market prediction result</returns>
    Task<MarketPredictionResult> PredictMarketConditionsAsync(
        IEnumerable<decimal> historicalData,
        EconomicContext economicContext,
        int timeHorizon = 30);

    /// <summary>
    /// Predicts security performance
    /// </summary>
    /// <param name="symbol">The security symbol</param>
    /// <param name="historicalPrices">Historical price data</param>
    /// <param name="economicFactors">Relevant economic factors</param>
    /// <param name="timeHorizon">Prediction time horizon in days</param>
    /// <returns>A security performance prediction</returns>
    Task<SecurityPredictionResult> PredictSecurityPerformanceAsync(
        string symbol,
        IEnumerable<decimal> historicalPrices,
        EconomicFactors economicFactors,
        int timeHorizon = 30);

    /// <summary>
    /// Predicts portfolio performance
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="marketPrediction">Current market prediction</param>
    /// <param name="timeHorizon">Prediction time horizon in days</param>
    /// <returns>A portfolio performance prediction</returns>
    Task<PortfolioPredictionResult> PredictPortfolioPerformanceAsync(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        int timeHorizon = 30);

    /// <summary>
    /// Gets prediction model performance metrics
    /// </summary>
    /// <param name="modelType">Type of prediction model</param>
    /// <returns>Performance metrics for the model</returns>
    Task<PredictionPerformanceMetrics> GetModelPerformanceAsync(
        PredictionModelType modelType);

    /// <summary>
    /// Gets current prediction recommendations
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="marketPrediction">Current market prediction</param>
    /// <returns>A collection of prediction recommendations</returns>
    Task<IEnumerable<PredictionRecommendation>> GetRecommendationsAsync(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction);

    /// <summary>
    /// Updates prediction model with new data
    /// </summary>
    /// <param name="modelType">Type of prediction model</param>
    /// <param name="newData">New training data</param>
    /// <returns>A task representing the model update operation</returns>
    Task UpdateModelAsync(
        PredictionModelType modelType,
        PredictionTrainingData newData);
}
