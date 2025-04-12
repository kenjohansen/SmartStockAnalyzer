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
/// Provides market prediction services
/// </summary>
/// <remarks>
/// Implements prediction models for market conditions, security performance,
/// and portfolio behavior using machine learning and statistical methods.
/// </remarks>
namespace SmartPortfolioAnalyzer.Services.Analysis.Services;

/// <summary>
/// Implementation of market prediction services
/// </summary>
public class PredictionEngine : IPredictionEngine
{
    private readonly Dictionary<PredictionModelType, IPredictionModel> _models;
    private readonly Dictionary<string, decimal> _modelWeights;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictionEngine"/> class.
    /// </summary>
    public PredictionEngine()
    {
        _models = new Dictionary<PredictionModelType, IPredictionModel>
        {
            { PredictionModelType.MachineLearning, new MachineLearningModel() },
            { PredictionModelType.Statistical, new StatisticalModel() },
            { PredictionModelType.TrendFollowing, new TrendFollowingModel() }
        };

        _modelWeights = new Dictionary<string, decimal>
        {
            { "MachineLearning", 0.4m },
            { "Statistical", 0.3m },
            { "TrendFollowing", 0.3m }
        };
    }

    /// <summary>
    /// Predicts future market conditions
    /// </summary>
    /// <param name="historicalData">Historical market data</param>
    /// <param name="economicContext">Current economic context</param>
    /// <param name="timeHorizon">Prediction time horizon in days</param>
    /// <returns>A market prediction result</returns>
    public async Task<MarketPredictionResult> PredictMarketConditionsAsync(
        IEnumerable<decimal> historicalData,
        EconomicContext economicContext,
        int timeHorizon = 30)
    {
        var predictions = new List<MarketPredictionResult>();
        
        // Get predictions from all models
        foreach (var model in _models)
        {
            var prediction = await model.Value.PredictMarketConditionsAsync(
                historicalData,
                economicContext,
                timeHorizon
            );
            predictions.Add(prediction);
        }

        // Combine predictions using weighted average
        var combinedPrediction = CombinePredictions(predictions, _modelWeights);
        
        // Calculate confidence score
        var confidence = CalculatePredictionConfidence(predictions);

        return new MarketPredictionResult
        {
            Prediction = combinedPrediction,
            ConfidenceScore = confidence,
            TimeHorizon = timeHorizon,
            EconomicContext = economicContext,
            ModelPerformance = await GetModelPerformanceAsync(PredictionModelType.MachineLearning)
        };
    }

    /// <summary>
    /// Predicts security performance
    /// </summary>
    /// <param name="symbol">The security symbol</param>
    /// <param name="historicalPrices">Historical price data</param>
    /// <param name="economicFactors">Relevant economic factors</param>
    /// <param name="timeHorizon">Prediction time horizon in days</param>
    /// <returns>A security performance prediction</returns>
    public async Task<SecurityPredictionResult> PredictSecurityPerformanceAsync(
        string symbol,
        IEnumerable<decimal> historicalPrices,
        EconomicFactors economicFactors,
        int timeHorizon = 30)
    {
        var predictions = new List<SecurityPredictionResult>();

        // Get predictions from all models
        foreach (var model in _models)
        {
            var prediction = await model.Value.PredictSecurityPerformanceAsync(
                symbol,
                historicalPrices,
                economicFactors,
                timeHorizon
            );
            predictions.Add(prediction);
        }

        // Combine predictions using weighted average
        var combinedPrediction = CombineSecurityPredictions(predictions, _modelWeights);
        
        // Calculate confidence score
        var confidence = CalculateSecurityPredictionConfidence(predictions);

        return new SecurityPredictionResult
        {
            Symbol = symbol,
            Prediction = combinedPrediction,
            ConfidenceScore = confidence,
            TimeHorizon = timeHorizon,
            EconomicFactors = economicFactors,
            ModelPerformance = await GetModelPerformanceAsync(PredictionModelType.MachineLearning)
        };
    }

    /// <summary>
    /// Predicts portfolio performance
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="marketPrediction">Current market prediction</param>
    /// <param name="timeHorizon">Prediction time horizon in days</param>
    /// <returns>A portfolio performance prediction</returns>
    public async Task<PortfolioPredictionResult> PredictPortfolioPerformanceAsync(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        int timeHorizon = 30)
    {
        var predictions = new List<PortfolioPredictionResult>();

        // Get predictions from all models
        foreach (var model in _models)
        {
            var prediction = await model.Value.PredictPortfolioPerformanceAsync(
                portfolio,
                marketPrediction,
                timeHorizon
            );
            predictions.Add(prediction);
        }

        // Combine predictions using weighted average
        var combinedPrediction = CombinePortfolioPredictions(predictions, _modelWeights);
        
        // Calculate confidence score
        var confidence = CalculatePortfolioPredictionConfidence(predictions);

        return new PortfolioPredictionResult
        {
            Portfolio = portfolio,
            Prediction = combinedPrediction,
            ConfidenceScore = confidence,
            TimeHorizon = timeHorizon,
            MarketPrediction = marketPrediction,
            ModelPerformance = await GetModelPerformanceAsync(PredictionModelType.MachineLearning)
        };
    }

    /// <summary>
    /// Gets prediction model performance metrics
    /// </summary>
    /// <param name="modelType">Type of prediction model</param>
    /// <returns>Performance metrics for the model</returns>
    public async Task<PredictionPerformanceMetrics> GetModelPerformanceAsync(
        PredictionModelType modelType)
    {
        if (!_models.TryGetValue(modelType, out var model))
            return new PredictionPerformanceMetrics();

        return await model.GetPerformanceMetricsAsync();
    }

    /// <summary>
    /// Gets current prediction recommendations
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="marketPrediction">Current market prediction</param>
    /// <returns>A collection of prediction recommendations</returns>
    public async Task<IEnumerable<PredictionRecommendation>> GetRecommendationsAsync(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction)
    {
        var recommendations = new List<PredictionRecommendation>();

        // Get recommendations from each model
        foreach (var model in _models)
        {
            var recs = await model.Value.GetRecommendationsAsync(portfolio, marketPrediction);
            recommendations.AddRange(recs);
        }

        // Combine recommendations using weighted voting
        var combinedRecommendations = CombineRecommendations(recommendations, _modelWeights);
        return combinedRecommendations;
    }

    /// <summary>
    /// Updates prediction model with new data
    /// </summary>
    /// <param name="modelType">Type of prediction model</param>
    /// <param name="newData">New training data</param>
    /// <returns>A task representing the model update operation</returns>
    public async Task UpdateModelAsync(
        PredictionModelType modelType,
        PredictionTrainingData newData)
    {
        if (!_models.TryGetValue(modelType, out var model))
            return;

        await model.UpdateModelAsync(newData);
    }

    // Helper methods for combining predictions
    private MarketPrediction CombinePredictions(
        IEnumerable<MarketPredictionResult> predictions,
        Dictionary<string, decimal> weights)
    {
        var weightedPredictions = predictions
            .Select(p => new { Prediction = p.Prediction, Weight = weights[p.ModelPerformance.ModelType.ToString()] });

        var combined = new MarketPrediction
        {
            Direction = CalculateWeightedDirection(weightedPredictions),
            ExpectedReturn = CalculateWeightedReturn(weightedPredictions),
            Volatility = CalculateWeightedVolatility(weightedPredictions),
            RiskLevel = CalculateCombinedRiskLevel(weightedPredictions)
        };

        return combined;
    }

    private SecurityPrediction CombineSecurityPredictions(
        IEnumerable<SecurityPredictionResult> predictions,
        Dictionary<string, decimal> weights)
    {
        var weightedPredictions = predictions
            .Select(p => new { Prediction = p.Prediction, Weight = weights[p.ModelPerformance.ModelType.ToString()] });

        var combined = new SecurityPrediction
        {
            ExpectedReturn = CalculateWeightedReturn(weightedPredictions),
            Volatility = CalculateWeightedVolatility(weightedPredictions),
            RiskLevel = CalculateCombinedRiskLevel(weightedPredictions),
            TechnicalScore = CalculateWeightedTechnicalScore(weightedPredictions)
        };

        return combined;
    }

    private PortfolioPrediction CombinePortfolioPredictions(
        IEnumerable<PortfolioPredictionResult> predictions,
        Dictionary<string, decimal> weights)
    {
        var weightedPredictions = predictions
            .Select(p => new { Prediction = p.Prediction, Weight = weights[p.ModelPerformance.ModelType.ToString()] });

        var combined = new PortfolioPrediction
        {
            ExpectedReturn = CalculateWeightedReturn(weightedPredictions),
            Volatility = CalculateWeightedVolatility(weightedPredictions),
            RiskLevel = CalculateCombinedRiskLevel(weightedPredictions),
            AssetAllocation = CalculateCombinedAssetAllocation(weightedPredictions)
        };

        return combined;
    }

    // Helper methods for calculating confidence scores
    private decimal CalculatePredictionConfidence(
        IEnumerable<MarketPredictionResult> predictions)
    {
        var consistency = CalculatePredictionConsistency(predictions);
        var historicalAccuracy = predictions.Average(p => p.ModelPerformance.Accuracy);
        return (consistency + historicalAccuracy) / 2;
    }

    private decimal CalculateSecurityPredictionConfidence(
        IEnumerable<SecurityPredictionResult> predictions)
    {
        var consistency = CalculatePredictionConsistency(predictions);
        var historicalAccuracy = predictions.Average(p => p.ModelPerformance.Accuracy);
        var technicalScore = predictions.Average(p => p.Prediction.TechnicalScore);
        return (consistency + historicalAccuracy + technicalScore) / 3;
    }

    private decimal CalculatePortfolioPredictionConfidence(
        IEnumerable<PortfolioPredictionResult> predictions)
    {
        var consistency = CalculatePredictionConsistency(predictions);
        var historicalAccuracy = predictions.Average(p => p.ModelPerformance.Accuracy);
        var diversification = CalculateDiversificationScore(predictions);
        return (consistency + historicalAccuracy + diversification) / 3;
    }

    // Helper methods for combining recommendations
    private IEnumerable<PredictionRecommendation> CombineRecommendations(
        IEnumerable<PredictionRecommendation> recommendations,
        Dictionary<string, decimal> weights)
    {
        var grouped = recommendations
            .GroupBy(r => r.Action)
            .Select(g => new PredictionRecommendation
            {
                Action = g.Key,
                Confidence = g.Average(r => r.Confidence * weights[r.ModelType.ToString()]),
                ModelType = PredictionModelType.Combined
            });

        return grouped.OrderByDescending(r => r.Confidence);
    }

    // Helper methods for various calculations
    private PredictionDirection CalculateWeightedDirection(
        IEnumerable<(MarketPrediction Prediction, decimal Weight)> predictions)
    {
        var upScore = predictions
            .Where(p => p.Prediction.Direction == PredictionDirection.Up)
            .Sum(p => p.Weight);

        var downScore = predictions
            .Where(p => p.Prediction.Direction == PredictionDirection.Down)
            .Sum(p => p.Weight);

        return upScore > downScore ? PredictionDirection.Up : PredictionDirection.Down;
    }

    private decimal CalculateWeightedReturn(
        IEnumerable<(dynamic Prediction, decimal Weight)> predictions)
    {
        return predictions
            .Select(p => p.Prediction.ExpectedReturn * p.Weight)
            .Sum();
    }

    private decimal CalculateWeightedVolatility(
        IEnumerable<(dynamic Prediction, decimal Weight)> predictions)
    {
        return predictions
            .Select(p => p.Prediction.Volatility * p.Weight)
            .Sum();
    }

    private RiskLevel CalculateCombinedRiskLevel(
        IEnumerable<(dynamic Prediction, decimal Weight)> predictions)
    {
        var weightedRisk = predictions
            .Select(p => (int)p.Prediction.RiskLevel * p.Weight)
            .Sum();

        return (RiskLevel)(weightedRisk / predictions.Count());
    }

    private decimal CalculateWeightedTechnicalScore(
        IEnumerable<(SecurityPrediction Prediction, decimal Weight)> predictions)
    {
        return predictions
            .Select(p => p.Prediction.TechnicalScore * p.Weight)
            .Sum();
    }

    private Dictionary<string, decimal> CalculateCombinedAssetAllocation(
        IEnumerable<(PortfolioPrediction Prediction, decimal Weight)> predictions)
    {
        var allocation = new Dictionary<string, decimal>();

        foreach (var (prediction, weight) in predictions)
        {
            foreach (var (asset, weight) in prediction.Prediction.AssetAllocation)
            {
                if (!allocation.ContainsKey(asset))
                    allocation[asset] = 0;
                allocation[asset] += weight * weight;
            }
        }

        var total = allocation.Values.Sum();
        foreach (var asset in allocation.Keys)
        {
            allocation[asset] /= total;
        }

        return allocation;
    }

    private decimal CalculatePredictionConsistency(
        IEnumerable<dynamic> predictions)
    {
        var directions = predictions
            .Select(p => p.Prediction.Direction)
            .Distinct()
            .Count();

        return directions == 1 ? 1m : directions == 2 ? 0.5m : 0m;
    }

    private decimal CalculateDiversificationScore(
        IEnumerable<PortfolioPredictionResult> predictions)
    {
        var allocations = predictions
            .SelectMany(p => p.Prediction.AssetAllocation)
            .GroupBy(a => a.Key)
            .Select(g => g.First())
            .ToDictionary(a => a.Key, a => a.Value);

        var concentration = allocations.Values.Max();
        return 1 - concentration;
    }
}
