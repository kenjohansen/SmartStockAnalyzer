/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System;
using System.Threading.Tasks;
using SmartPortfolioAnalyzer.Core.Models;

/// <summary>
/// Interface for prediction models
/// </summary>
/// <remarks>
/// Provides a common interface for different types of prediction models,
/// including market conditions, security performance, and portfolio behavior.
/// </remarks>
namespace SmartPortfolioAnalyzer.Services.Analysis.Interfaces;

/// <summary>
/// Defines methods for prediction models
/// </summary>
public interface IPredictionModel
{
    /// <summary>
    /// Gets the type of prediction model
    /// </summary>
    PredictionModelType ModelType { get; }

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
    /// Gets performance metrics for the model
    /// </summary>
    /// <returns>Performance metrics for the model</returns>
    Task<PredictionPerformanceMetrics> GetPerformanceMetricsAsync();

    /// <summary>
    /// Gets current recommendations from the model
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="marketPrediction">Current market prediction</param>
    /// <returns>A collection of model recommendations</returns>
    Task<IEnumerable<PredictionRecommendation>> GetRecommendationsAsync(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction);

    /// <summary>
    /// Updates the model with new training data
    /// </summary>
    /// <param name="newData">New training data</param>
    /// <returns>A task representing the model update operation</returns>
    Task UpdateModelAsync(PredictionTrainingData newData);

    /// <summary>
    /// Validates the model's predictions against actual data
    /// </summary>
    /// <param name="actualData">Actual data to validate against</param>
    /// <returns>Validation results</returns>
    Task<PredictionValidationResult> ValidateModelAsync(PredictionValidationData actualData);
}
