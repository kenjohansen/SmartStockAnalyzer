/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using Microsoft.ML;
using Microsoft.ML.Data;
using System;

namespace SmartPortfolioAnalyzer.Services.Analysis.Models
{
    /// <summary>
    /// Represents market prediction data for training and prediction.
    /// </summary>
    public class MarketPredictionData
    {
        [LoadColumn(0)]
        public float HistoricalReturns { get; set; }

        [LoadColumn(1)]
        public float EconomicIndicators { get; set; }

        [LoadColumn(2)]
        public float MarketSentiment { get; set; }

        [LoadColumn(3)]
        public float TechnicalIndicators { get; set; }

        [LoadColumn(4)]
        public float Volatility { get; set; }

        [LoadColumn(5)]
        public float MacroeconomicFactors { get; set; }

        [LoadColumn(6)]
        public float ExpectedReturn { get; set; }
    }

    /// <summary>
    /// Represents the prediction output from the market prediction model.
    /// </summary>
    public class MarketPredictionResult
    {
        [ColumnName("Score")]
        public float PredictedReturn { get; set; }

        public float ConfidenceScore { get; set; }

        public string PredictionExplanation { get; set; } = string.Empty;

        public MarketPredictionMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// Contains metrics for market prediction performance.
    /// </summary>
    public class MarketPredictionMetrics
    {
        public float R2Score { get; set; }
        public float MeanAbsoluteError { get; set; }
        public float RootMeanSquaredError { get; set; }
        public float ExplainedVariance { get; set; }
    }

    /// <summary>
    /// Configuration settings for the market prediction model.
    /// </summary>
    public class MarketPredictionConfig
    {
        public int PredictionHorizon { get; set; } = 30; // days
        public int TrainingWindowSize { get; set; } = 252; // trading days
        public float ConfidenceThreshold { get; set; } = 0.7f;
        public bool UseFeatureEngineering { get; set; } = true;
        public string[] FeatureColumns = new[]
        {
            "HistoricalReturns",
            "EconomicIndicators",
            "MarketSentiment",
            "TechnicalIndicators",
            "Volatility",
            "MacroeconomicFactors"
        };
    }
}
