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

public enum ModelWeightingStrategy
{
    /// <summary>
    /// Equal weights for all models
    /// </summary>
    Equal,

    /// <summary>
    /// Weights based on historical performance
    /// </summary>
    PerformanceBased,

    /// <summary>
    /// Weights based on market conditions
    /// </summary>
    MarketConditionBased,

    /// <summary>
    /// Weights based on volatility
    /// </summary>
    VolatilityBased,

    /// <summary>
    /// Weights based on confidence scores
    /// </summary>
    ConfidenceBased
}

public class ModelWeightCalculator
{
    private readonly Dictionary<PredictionModelType, decimal> _defaultWeights;

    public ModelWeightCalculator()
    {
        _defaultWeights = new Dictionary<PredictionModelType, decimal>
        {
            { PredictionModelType.MachineLearning, 0.4m },
            { PredictionModelType.Statistical, 0.3m },
            { PredictionModelType.TrendFollowing, 0.3m }
        };
    }

    public Dictionary<PredictionModelType, decimal> CalculateWeights(
        IEnumerable<PredictionPerformanceMetrics> modelMetrics,
        EconomicContext economicContext,
        ModelWeightingStrategy strategy = ModelWeightingStrategy.PerformanceBased)
    {
        return strategy switch
        {
            ModelWeightingStrategy.Equal => CalculateEqualWeights(),
            ModelWeightingStrategy.PerformanceBased => CalculatePerformanceBasedWeights(modelMetrics),
            ModelWeightingStrategy.MarketConditionBased => CalculateMarketConditionBasedWeights(economicContext),
            ModelWeightingStrategy.VolatilityBased => CalculateVolatilityBasedWeights(economicContext),
            ModelWeightingStrategy.ConfidenceBased => CalculateConfidenceBasedWeights(modelMetrics),
            _ => CalculateEqualWeights()
        };
    }

    private Dictionary<PredictionModelType, decimal> CalculateEqualWeights()
    {
        var weights = new Dictionary<PredictionModelType, decimal>();
        var models = Enum.GetValues<PredictionModelType>();
        var weight = 1m / models.Length;

        foreach (var model in models)
        {
            weights[model] = weight;
        }

        return weights;
    }

    private Dictionary<PredictionModelType, decimal> CalculatePerformanceBasedWeights(
        IEnumerable<PredictionPerformanceMetrics> modelMetrics)
    {
        var metrics = modelMetrics.ToList();
        var totalScore = metrics.Sum(m => m.F1Score);

        return metrics.ToDictionary(
            m => m.ModelType,
            m => m.F1Score / totalScore
        );
    }

    private Dictionary<PredictionModelType, decimal> CalculateMarketConditionBasedWeights(
        EconomicContext economicContext)
    {
        var weights = new Dictionary<PredictionModelType, decimal>();
        var volatility = economicContext.Volatility;
        var trend = economicContext.TrendStrength;

        // Adjust weights based on market conditions
        var mlWeight = 0.4m;
        var statWeight = 0.3m;
        var trendWeight = 0.3m;

        if (volatility > 0.2m)
        {
            mlWeight *= 0.8m; // Reduce ML weight in high volatility
            statWeight *= 1.2m; // Increase statistical weight
        }

        if (trend > 0.1m)
        {
            trendWeight *= 1.2m; // Increase trend following weight in trending markets
        }

        // Normalize weights
        var totalWeight = mlWeight + statWeight + trendWeight;
        weights[PredictionModelType.MachineLearning] = mlWeight / totalWeight;
        weights[PredictionModelType.Statistical] = statWeight / totalWeight;
        weights[PredictionModelType.TrendFollowing] = trendWeight / totalWeight;

        return weights;
    }

    private Dictionary<PredictionModelType, decimal> CalculateVolatilityBasedWeights(
        EconomicContext economicContext)
    {
        var weights = new Dictionary<PredictionModelType, decimal>();
        var volatility = economicContext.Volatility;

        // Adjust weights based on volatility
        var mlWeight = 0.4m;
        var statWeight = 0.3m;
        var trendWeight = 0.3m;

        if (volatility < 0.1m)
        {
            // Low volatility - favor trend following
            trendWeight *= 1.2m;
            statWeight *= 0.9m;
        }
        else if (volatility > 0.2m)
        {
            // High volatility - favor statistical models
            statWeight *= 1.2m;
            trendWeight *= 0.8m;
        }

        // Normalize weights
        var totalWeight = mlWeight + statWeight + trendWeight;
        weights[PredictionModelType.MachineLearning] = mlWeight / totalWeight;
        weights[PredictionModelType.Statistical] = statWeight / totalWeight;
        weights[PredictionModelType.TrendFollowing] = trendWeight / totalWeight;

        return weights;
    }

    private Dictionary<PredictionModelType, decimal> CalculateConfidenceBasedWeights(
        IEnumerable<PredictionPerformanceMetrics> modelMetrics)
    {
        var metrics = modelMetrics.ToList();
        var totalConfidence = metrics.Sum(m => m.ConfidenceScore);

        return metrics.ToDictionary(
            m => m.ModelType,
            m => m.ConfidenceScore / totalConfidence
        );
    }
}
