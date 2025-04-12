/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System;
using System.Threading.Tasks;
using Microsoft.ML;
using SmartPortfolioAnalyzer.Core.Models;
using SmartPortfolioAnalyzer.Services.Analysis.Interfaces;

namespace SmartPortfolioAnalyzer.Services.Analysis.Models;

public class MachineLearningModel : IPredictionModel
{
    private readonly MLContext _mlContext;
    private ITransformer _model;
    private PredictionEngine<MarketData, MarketPrediction> _marketPredictor;
    private PredictionEngine<SecurityData, SecurityPrediction> _securityPredictor;
    private PredictionEngine<PortfolioData, PortfolioPrediction> _portfolioPredictor;

    public MachineLearningModel()
    {
        _mlContext = new MLContext();
        _model = null;
        _marketPredictor = null;
        _securityPredictor = null;
        _portfolioPredictor = null;
    }

    public PredictionModelType ModelType => PredictionModelType.MachineLearning;

    public async Task<MarketPredictionResult> PredictMarketConditionsAsync(
        IEnumerable<decimal> historicalData,
        EconomicContext economicContext,
        int timeHorizon = 30)
    {
        if (_marketPredictor == null)
            await InitializeMarketPredictorAsync();

        var inputData = new MarketData
        {
            HistoricalPrices = historicalData.ToArray(),
            EconomicIndicators = economicContext.Indicators,
            TimeHorizon = timeHorizon
        };

        var prediction = _marketPredictor.Predict(inputData);
        
        return new MarketPredictionResult
        {
            Prediction = new MarketPrediction
            {
                Direction = prediction.Direction,
                ExpectedReturn = prediction.ExpectedReturn,
                Volatility = prediction.Volatility,
                RiskLevel = prediction.RiskLevel
            },
            ConfidenceScore = prediction.Confidence,
            TimeHorizon = timeHorizon,
            EconomicContext = economicContext,
            ModelPerformance = await GetPerformanceMetricsAsync()
        };
    }

    public async Task<SecurityPredictionResult> PredictSecurityPerformanceAsync(
        string symbol,
        IEnumerable<decimal> historicalPrices,
        EconomicFactors economicFactors,
        int timeHorizon = 30)
    {
        if (_securityPredictor == null)
            await InitializeSecurityPredictorAsync();

        var inputData = new SecurityData
        {
            Symbol = symbol,
            HistoricalPrices = historicalPrices.ToArray(),
            EconomicFactors = economicFactors,
            TimeHorizon = timeHorizon
        };

        var prediction = _securityPredictor.Predict(inputData);
        
        return new SecurityPredictionResult
        {
            Symbol = symbol,
            Prediction = new SecurityPrediction
            {
                ExpectedReturn = prediction.ExpectedReturn,
                Volatility = prediction.Volatility,
                RiskLevel = prediction.RiskLevel,
                TechnicalScore = prediction.TechnicalScore
            },
            ConfidenceScore = prediction.Confidence,
            TimeHorizon = timeHorizon,
            EconomicFactors = economicFactors,
            ModelPerformance = await GetPerformanceMetricsAsync()
        };
    }

    public async Task<PortfolioPredictionResult> PredictPortfolioPerformanceAsync(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction,
        int timeHorizon = 30)
    {
        if (_portfolioPredictor == null)
            await InitializePortfolioPredictorAsync();

        var inputData = new PortfolioData
        {
            Portfolio = portfolio,
            MarketPrediction = marketPrediction,
            TimeHorizon = timeHorizon
        };

        var prediction = _portfolioPredictor.Predict(inputData);
        
        return new PortfolioPredictionResult
        {
            Portfolio = portfolio,
            Prediction = new PortfolioPrediction
            {
                ExpectedReturn = prediction.ExpectedReturn,
                Volatility = prediction.Volatility,
                RiskLevel = prediction.RiskLevel,
                AssetAllocation = prediction.AssetAllocation
            },
            ConfidenceScore = prediction.Confidence,
            TimeHorizon = timeHorizon,
            MarketPrediction = marketPrediction,
            ModelPerformance = await GetPerformanceMetricsAsync()
        };
    }

    public async Task<PredictionPerformanceMetrics> GetPerformanceMetricsAsync()
    {
        // TODO: Implement actual performance metrics calculation
        return new PredictionPerformanceMetrics
        {
            Accuracy = 0.85m,
            Precision = 0.80m,
            Recall = 0.82m,
            F1Score = 0.81m,
            ModelType = ModelType
        };
    }

    public async Task<IEnumerable<PredictionRecommendation>> GetRecommendationsAsync(
        Portfolio portfolio,
        MarketPredictionResult marketPrediction)
    {
        var recommendations = new List<PredictionRecommendation>();

        // Analyze market conditions
        var marketRec = new PredictionRecommendation
        {
            Action = marketPrediction.Prediction.Direction == PredictionDirection.Up 
                ? RecommendationAction.Buy : RecommendationAction.Sell,
            Confidence = marketPrediction.ConfidenceScore,
            ModelType = ModelType,
            Rationale = "Based on market trend analysis"
        };
        recommendations.Add(marketRec);

        // Analyze portfolio composition
        foreach (var position in portfolio.Positions)
        {
            var rec = new PredictionRecommendation
            {
                Action = position.Weight > 0.1m ? RecommendationAction.Reduce : RecommendationAction.Increase,
                Confidence = CalculatePositionConfidence(position),
                ModelType = ModelType,
                Rationale = "Based on position weight optimization"
            };
            recommendations.Add(rec);
        }

        return recommendations;
    }

    public async Task UpdateModelAsync(PredictionTrainingData newData)
    {
        // TODO: Implement actual model retraining
        await Task.CompletedTask;
    }

    public async Task<PredictionValidationResult> ValidateModelAsync(PredictionValidationData actualData)
    {
        var validationResult = new PredictionValidationResult();

        // Validate market predictions
        var marketAccuracy = CalculateMarketPredictionAccuracy(
            actualData.MarketData,
            actualData.PredictedMarketData
        );
        validationResult.MarketAccuracy = marketAccuracy;

        // Validate security predictions
        var securityAccuracy = CalculateSecurityPredictionAccuracy(
            actualData.SecurityData,
            actualData.PredictedSecurityData
        );
        validationResult.SecurityAccuracy = securityAccuracy;

        // Validate portfolio predictions
        var portfolioAccuracy = CalculatePortfolioPredictionAccuracy(
            actualData.PortfolioData,
            actualData.PredictedPortfolioData
        );
        validationResult.PortfolioAccuracy = portfolioAccuracy;

        return validationResult;
    }

    private async Task InitializeMarketPredictorAsync()
    {
        var pipeline = _mlContext.Transforms.CopyColumns(
            outputColumnName: "Label",
            inputColumnName: "ExpectedReturn")
            .Append(_mlContext.Transforms.Concatenate(
                "Features",
                "HistoricalPrices",
                "EconomicIndicators",
                "TimeHorizon"))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Transforms.Concatenate(
                "Features",
                "Features",
                "Label"))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"));

        _model = pipeline.Fit(_mlContext.Data.LoadFromEnumerable(new[] { new MarketData() }));
        _marketPredictor = _mlContext.Model.CreatePredictionEngine<MarketData, MarketPrediction>(_model);
    }
}
