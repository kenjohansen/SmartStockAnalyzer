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

public class ModelPerformanceMonitor
{
    private readonly Dictionary<PredictionModelType, ModelPerformanceMetrics> _currentMetrics;
    private readonly Dictionary<PredictionModelType, List<ModelPerformanceMetrics>> _historicalMetrics;
    private readonly TimeSpan _monitoringInterval = TimeSpan.FromHours(24);
    private readonly decimal _minimumAccuracyThreshold = 0.6m;
    private readonly decimal _minimumConfidenceThreshold = 0.7m;

    public ModelPerformanceMonitor()
    {
        _currentMetrics = new Dictionary<PredictionModelType, ModelPerformanceMetrics>();
        _historicalMetrics = new Dictionary<PredictionModelType, List<ModelPerformanceMetrics>>();
    }

    public void UpdateMetrics(
        PredictionModelType modelType,
        PredictionPerformanceMetrics metrics,
        PredictionValidationResult validation)
    {
        var currentMetrics = new ModelPerformanceMetrics
        {
            ModelType = modelType,
            Accuracy = metrics.Accuracy,
            Precision = metrics.Precision,
            Recall = metrics.Recall,
            F1Score = metrics.F1Score,
            LastUpdate = DateTime.UtcNow,
            ValidationMetrics = validation,
            Status = CalculateModelStatus(metrics, validation)
        };

        _currentMetrics[modelType] = currentMetrics;

        if (!_historicalMetrics.ContainsKey(modelType))
        {
            _historicalMetrics[modelType] = new List<ModelPerformanceMetrics>();
        }

        _historicalMetrics[modelType].Add(currentMetrics);
        TrimHistoricalMetrics(modelType);
    }

    public ModelPerformanceMetrics GetCurrentMetrics(PredictionModelType modelType)
    {
        return _currentMetrics.TryGetValue(modelType, out var metrics) ? metrics : null;
    }

    public IEnumerable<ModelPerformanceMetrics> GetHistoricalMetrics(PredictionModelType modelType)
    {
        return _historicalMetrics.TryGetValue(modelType, out var metrics) ? metrics : Enumerable.Empty<ModelPerformanceMetrics>();
    }

    public ModelHealthStatus GetModelHealth(PredictionModelType modelType)
    {
        if (!_currentMetrics.TryGetValue(modelType, out var currentMetrics))
        {
            return ModelHealthStatus.Unknown;
        }

        var status = currentMetrics.Status;
        var historicalMetrics = _historicalMetrics[modelType];

        var performanceTrend = CalculatePerformanceTrend(historicalMetrics);
        var validationTrend = CalculateValidationTrend(historicalMetrics);
        var confidenceTrend = CalculateConfidenceTrend(historicalMetrics);

        return new ModelHealthStatus
        {
            CurrentStatus = status,
            PerformanceTrend = performanceTrend,
            ValidationTrend = validationTrend,
            ConfidenceTrend = confidenceTrend,
            LastUpdate = currentMetrics.LastUpdate,
            Recommendations = GenerateHealthRecommendations(status, performanceTrend, validationTrend, confidenceTrend)
        };
    }

    private ModelStatus CalculateModelStatus(
        PredictionPerformanceMetrics metrics,
        PredictionValidationResult validation)
    {
        if (metrics.Accuracy < _minimumAccuracyThreshold ||
            metrics.ConfidenceScore < _minimumConfidenceThreshold ||
            validation.MarketAccuracy < 0.6m ||
            validation.SecurityAccuracy < 0.6m ||
            validation.PortfolioAccuracy < 0.6m)
        {
            return ModelStatus.Critical;
        }

        if (metrics.Accuracy < 0.7m ||
            metrics.ConfidenceScore < 0.8m ||
            validation.MarketAccuracy < 0.7m ||
            validation.SecurityAccuracy < 0.7m ||
            validation.PortfolioAccuracy < 0.7m)
        {
            return ModelStatus.Warning;
        }

        return ModelStatus.Healthy;
    }

    private PerformanceTrend CalculatePerformanceTrend(
        IEnumerable<ModelPerformanceMetrics> metrics)
    {
        var recentMetrics = metrics.OrderByDescending(m => m.LastUpdate).Take(5).ToList();
        if (recentMetrics.Count < 2) return PerformanceTrend.Stable;

        var latest = recentMetrics[0];
        var previous = recentMetrics[1];

        var accuracyChange = (latest.Accuracy - previous.Accuracy) / previous.Accuracy;
        var precisionChange = (latest.Precision - previous.Precision) / previous.Precision;
        var recallChange = (latest.Recall - previous.Recall) / previous.Recall;
        var f1Change = (latest.F1Score - previous.F1Score) / previous.F1Score;

        var avgChange = (accuracyChange + precisionChange + recallChange + f1Change) / 4;

        return avgChange > 0.1m ? PerformanceTrend.Improving :
               avgChange < -0.1m ? PerformanceTrend.Deteriorating :
               PerformanceTrend.Stable;
    }

    private ValidationTrend CalculateValidationTrend(
        IEnumerable<ModelPerformanceMetrics> metrics)
    {
        var recentMetrics = metrics.OrderByDescending(m => m.LastUpdate).Take(5).ToList();
        if (recentMetrics.Count < 2) return ValidationTrend.Stable;

        var latest = recentMetrics[0];
        var previous = recentMetrics[1];

        var marketChange = (latest.ValidationMetrics.MarketAccuracy - previous.ValidationMetrics.MarketAccuracy) / 
                          previous.ValidationMetrics.MarketAccuracy;
        var securityChange = (latest.ValidationMetrics.SecurityAccuracy - previous.ValidationMetrics.SecurityAccuracy) /
                           previous.ValidationMetrics.SecurityAccuracy;
        var portfolioChange = (latest.ValidationMetrics.PortfolioAccuracy - previous.ValidationMetrics.PortfolioAccuracy) /
                            previous.ValidationMetrics.PortfolioAccuracy;

        var avgChange = (marketChange + securityChange + portfolioChange) / 3;

        return avgChange > 0.1m ? ValidationTrend.Improving :
               avgChange < -0.1m ? ValidationTrend.Deteriorating :
               ValidationTrend.Stable;
    }

    private ConfidenceTrend CalculateConfidenceTrend(
        IEnumerable<ModelPerformanceMetrics> metrics)
    {
        var recentMetrics = metrics.OrderByDescending(m => m.LastUpdate).Take(5).ToList();
        if (recentMetrics.Count < 2) return ConfidenceTrend.Stable;

        var latest = recentMetrics[0];
        var previous = recentMetrics[1];

        var confidenceChange = (latest.ConfidenceScore - previous.ConfidenceScore) / previous.ConfidenceScore;

        return confidenceChange > 0.1m ? ConfidenceTrend.Improving :
               confidenceChange < -0.1m ? ConfidenceTrend.Deteriorating :
               ConfidenceTrend.Stable;
    }

    private void TrimHistoricalMetrics(PredictionModelType modelType)
    {
        if (!_historicalMetrics.ContainsKey(modelType)) return;

        var metrics = _historicalMetrics[modelType];
        var threshold = DateTime.UtcNow - _monitoringInterval;
        _historicalMetrics[modelType] = metrics.Where(m => m.LastUpdate >= threshold).ToList();
    }

    private IEnumerable<string> GenerateHealthRecommendations(
        ModelStatus status,
        PerformanceTrend performanceTrend,
        ValidationTrend validationTrend,
        ConfidenceTrend confidenceTrend)
    {
        var recommendations = new List<string>();

        switch (status)
        {
            case ModelStatus.Critical:
                recommendations.Add("Model performance is critically low - immediate attention required");
                recommendations.Add("Consider retraining the model with fresh data");
                recommendations.Add("Validate model assumptions and data quality");
                break;

            case ModelStatus.Warning:
                recommendations.Add("Model performance is below optimal levels");
                recommendations.Add("Monitor closely and consider retraining if performance continues to decline");
                recommendations.Add("Review recent market conditions for potential impact");
                break;

            case ModelStatus.Healthy:
                recommendations.Add("Model is performing well - continue monitoring");
                recommendations.Add("Consider model enhancements for further improvements");
                break;
        }

        if (performanceTrend == PerformanceTrend.Deteriorating)
        {
            recommendations.Add("Performance is declining - investigate recent changes in market conditions");
            recommendations.Add("Consider updating model parameters");
        }

        if (validationTrend == ValidationTrend.Deteriorating)
        {
            recommendations.Add("Validation metrics are declining - review data quality");
            recommendations.Add("Consider adding more recent market data to training set");
        }

        if (confidenceTrend == ConfidenceTrend.Deteriorating)
        {
            recommendations.Add("Model confidence is decreasing - investigate recent predictions");
            recommendations.Add("Consider adjusting model thresholds");
        }

        return recommendations;
    }
}

public class ModelPerformanceMetrics
{
    public PredictionModelType ModelType { get; set; }
    public decimal Accuracy { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1Score { get; set; }
    public decimal ConfidenceScore { get; set; }
    public DateTime LastUpdate { get; set; }
    public PredictionValidationResult ValidationMetrics { get; set; }
    public ModelStatus Status { get; set; }
}

public enum ModelStatus
{
    Unknown,
    Critical,
    Warning,
    Healthy
}

public enum PerformanceTrend
{
    Stable,
    Improving,
    Deteriorating
}

public enum ValidationTrend
{
    Stable,
    Improving,
    Deteriorating
}

public enum ConfidenceTrend
{
    Stable,
    Improving,
    Deteriorating
}

public class ModelHealthStatus
{
    public ModelStatus CurrentStatus { get; set; }
    public PerformanceTrend PerformanceTrend { get; set; }
    public ValidationTrend ValidationTrend { get; set; }
    public ConfidenceTrend ConfidenceTrend { get; set; }
    public DateTime LastUpdate { get; set; }
    public IEnumerable<string> Recommendations { get; set; }
}
