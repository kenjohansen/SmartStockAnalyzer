/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using Microsoft.ML;
using Microsoft.ML.Data;
using SmartPortfolioAnalyzer.Services.Analysis.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartPortfolioAnalyzer.Services.Analysis.Services
{
    public interface IPortfolioPredictionService
    {
        Task<PortfolioPredictionTrainingResult> TrainModelAsync(IEnumerable<PortfolioPredictionData> historicalData);
        Task<PortfolioPredictionResult> PredictPortfolioPerformanceAsync(Portfolio portfolio);
        Task<PortfolioPredictionBacktestResult> BacktestPortfolioPredictionAsync(
            string portfolioId,
            DateTime startDate,
            DateTime endDate);
    }

    public class PortfolioPredictionService : IPortfolioPredictionService
    {
        private readonly MLContext _mlContext;
        private readonly PortfolioPredictionConfig _config;
        private ITransformer _model;

        public PortfolioPredictionService(MLContext mlContext, PortfolioPredictionConfig config)
        {
            _mlContext = mlContext;
            _config = config;
        }

        /// <summary>
        /// Builds the training pipeline for portfolio prediction.
        /// </summary>
        /// <returns>The ML.NET training pipeline</returns>
        private IEstimator<ITransformer> BuildTrainingPipeline()
        {
            // Start with basic feature concatenation
            var pipeline = _mlContext.Transforms.Concatenate("Features", _config.FeatureColumns)
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"));
            
            return pipeline;
        }

        /// <summary>
        /// Trains a new portfolio prediction model using historical data.
        /// </summary>
        /// <param name="historicalData">Historical portfolio data for training</param>
        /// <returns>Training statistics and model metrics</returns>
        public async Task<PortfolioPredictionTrainingResult> TrainModelAsync(IEnumerable<PortfolioPredictionData> historicalData)
        {
            // Load the data collection into MLContext
            var trainingData = _mlContext.Data.LoadFromEnumerable(historicalData);
            
            // Build our simple pipeline
            var pipeline = BuildTrainingPipeline();
            
            // Train the model
            _model = pipeline.Fit(trainingData);
            
            // Get predictions on the training data
            var predictions = _model.Transform(trainingData);
            
            // Evaluate the model
            var metrics = _mlContext.Regression.Evaluate(predictions);
            
            return new PortfolioPredictionTrainingResult
            {
                Model = _model,
                TrainingMetrics = new PortfolioPredictionMetrics
                {
                    R2Score = metrics.R2Score,
                    MeanAbsoluteError = metrics.MeanAbsoluteError,
                    RootMeanSquaredError = metrics.RootMeanSquaredError,
                    ExplainedVariance = metrics.ExplainedVariance
                }
            };
        }

        /// <summary>
        /// Predicts portfolio performance.
        /// </summary>
        /// <param name="portfolio">Portfolio to predict</param>
        /// <returns>Portfolio prediction result</returns>
        public async Task<PortfolioPredictionResult> PredictPortfolioPerformanceAsync(Portfolio portfolio)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            // Prepare portfolio data
            var portfolioData = PreparePortfolioData(portfolio);
            
            // Create a single-item collection for prediction
            var predictionData = _mlContext.Data.LoadFromEnumerable(new[] { portfolioData });
            
            // Get prediction using the trained model
            var prediction = _model.Transform(predictionData);
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<PortfolioPredictionData, PortfolioPredictionPrediction>(_model);
            var result = predictionEngine.Predict(portfolioData);
            
            // Calculate portfolio metrics
            var portfolioMetrics = CalculatePortfolioMetrics(portfolio, result.Score);
            
            return new PortfolioPredictionResult
            {
                PortfolioId = portfolio.Id,
                Prediction = new PortfolioPrediction
                {
                    ExpectedReturn = result.Score,
                    RiskLevel = portfolioMetrics.RiskLevel,
                    DiversificationScore = portfolioMetrics.DiversificationScore,
                    SectorExposure = portfolioMetrics.SectorExposure,
                    StyleExposure = portfolioMetrics.StyleExposure,
                    TechnicalScore = portfolioMetrics.TechnicalScore
                },
                ConfidenceScore = CalculateConfidenceScore(result.Score),
                PredictionExplanation = GenerateExplanation(result.Score, portfolio),
                TimeHorizon = _config.PredictionHorizon,
                Metrics = new PortfolioPredictionMetrics
                {
                    R2Score = 0.0f, // Not applicable for single prediction
                    MeanAbsoluteError = 0.0f,
                    RootMeanSquaredError = 0.0f,
                    ExplainedVariance = 0.0f
                }
            };
        }

        /// <summary>
        /// Backtests the portfolio prediction model over a specified period.
        /// </summary>
        /// <param name="portfolioId">Portfolio ID to backtest</param>
        /// <param name="startDate">Start date for backtesting</param>
        /// <param name="endDate">End date for backtesting</param>
        /// <returns>Backtest results including performance metrics</returns>
        public async Task<PortfolioPredictionBacktestResult> BacktestPortfolioPredictionAsync(
            string portfolioId,
            DateTime startDate,
            DateTime endDate)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(portfolioId))
                throw new ArgumentException("Portfolio ID cannot be null or empty", nameof(portfolioId));

            if (startDate >= endDate)
                throw new ArgumentException("Start date must be before end date");

            // Get historical portfolio data for the period
            var historicalData = await GetHistoricalPortfolioDataAsync(portfolioId, startDate, endDate);
            if (historicalData == null || !historicalData.Any())
                throw new ArgumentException("No historical data available for the specified period");

            // Prepare predictions
            var predictions = new List<PortfolioPredictionPerformance>();
            var actualReturns = new List<decimal>();

            // Create sliding window of data
            var windowSize = _config.PredictionHorizon;
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                // Get data for current window
                var windowData = historicalData
                    .Where(d => d.Date <= currentDate && d.Date >= currentDate.AddDays(-windowSize))
                    .OrderBy(d => d.Date)
                    .ToList();

                if (windowData.Count < windowSize)
                    break;

                // Prepare prediction data
                var predictionData = PreparePortfolioData(windowData.Last());
                var predictionDataEnumerable = _mlContext.Data.LoadFromEnumerable(new[] { predictionData });
                
                // Get prediction
                var prediction = _model.Transform(predictionDataEnumerable);
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<PortfolioPredictionData, PortfolioPredictionPrediction>(_model);
                var result = predictionEngine.Predict(predictionData);

                // Get actual return for next period
                var nextPeriodData = historicalData
                    .Where(d => d.Date > currentDate && d.Date <= currentDate.AddDays(_config.PredictionHorizon))
                    .OrderBy(d => d.Date)
                    .ToList();

                if (nextPeriodData.Count < 2)
                    break;

                var actualReturn = CalculatePortfolioReturn(nextPeriodData);
                
                // Store results
                predictions.Add(new PortfolioPredictionPerformance
                {
                    PortfolioId = portfolioId,
                    PredictionDate = currentDate,
                    PredictedReturn = result.Score,
                    ActualReturn = actualReturn,
                    Error = actualReturn - result.Score,
                    AbsoluteError = Math.Abs(actualReturn - result.Score),
                    SquaredError = (actualReturn - result.Score) * (actualReturn - result.Score)
                });

                actualReturns.Add(actualReturn);

                // Move to next date
                currentDate = currentDate.AddDays(1);
            }

            // Calculate performance metrics
            var metrics = MonitorPortfolioPerformance(predictions, actualReturns);

            return new PortfolioPredictionBacktestResult
            {
                PortfolioId = portfolioId,
                StartDate = startDate,
                EndDate = endDate,
                Predictions = predictions,
                PerformanceMetrics = metrics,
                ModelConfiguration = _config
            };
        }

        /// <summary>
        /// Prepares portfolio data for prediction.
        /// </summary>
        /// <param name="portfolio">Portfolio to prepare</param>
        /// <returns>Prepared portfolio data</returns>
        private PortfolioPredictionData PreparePortfolioData(Portfolio portfolio)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            // Calculate portfolio metrics
            var totalValue = portfolio.Positions.Sum(p => p.Quantity * p.CurrentPrice);
            var sectorExposure = CalculateSectorExposure(portfolio);
            var styleExposure = CalculateStyleExposure(portfolio);
            var technicalMetrics = CalculateTechnicalMetrics(portfolio);
            var riskMetrics = CalculateRiskMetrics(portfolio);

            return new PortfolioPredictionData
            {
                TotalValue = (float)totalValue,
                SectorExposure = sectorExposure,
                StyleExposure = styleExposure,
                TechnicalMetrics = technicalMetrics,
                RiskMetrics = riskMetrics,
                MarketSentiment = GetMarketSentiment(),
                EconomicContext = GetEconomicContext()
            };
        }

        /// <summary>
        /// Calculates sector exposure metrics for a portfolio.
        /// </summary>
        /// <param name="portfolio">Portfolio to analyze</param>
        /// <returns>Sector exposure metrics</returns>
        private decimal[] CalculateSectorExposure(Portfolio portfolio)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            // TODO: Implement actual sector exposure calculation
            return new decimal[] { 0.25m, 0.25m, 0.25m, 0.25m }; // Equal exposure for now
        }

        /// <summary>
        /// Calculates style exposure metrics for a portfolio.
        /// </summary>
        /// <param name="portfolio">Portfolio to analyze</param>
        /// <returns>Style exposure metrics</returns>
        private decimal[] CalculateStyleExposure(Portfolio portfolio)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            // TODO: Implement actual style exposure calculation
            return new decimal[] { 0.5m, 0.3m, 0.2m }; // Growth, Value, Income
        }

        /// <summary>
        /// Calculates technical metrics for a portfolio.
        /// </summary>
        /// <param name="portfolio">Portfolio to analyze</param>
        /// <returns>Technical metrics</returns>
        private decimal CalculateTechnicalMetrics(Portfolio portfolio)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            // TODO: Implement actual technical metrics calculation
            return portfolio.Positions.Average(p => p.TrendScore ?? 0); // Simplified example
        }

        /// <summary>
        /// Calculates risk metrics for a portfolio.
        /// </summary>
        /// <param name="portfolio">Portfolio to analyze</param>
        /// <returns>Risk metrics</returns>
        private decimal CalculateRiskMetrics(Portfolio portfolio)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            // Calculate portfolio volatility
            var returns = portfolio.Positions
                .Select(p => CalculateReturns(p.PriceHistory))
                .ToList();

            return CalculatePortfolioVolatility(returns);
        }

        /// <summary>
        /// Calculates portfolio volatility from individual security returns.
        /// </summary>
        /// <param name="securityReturns">List of security returns</param>
        /// <returns>Portfolio volatility</returns>
        private decimal CalculatePortfolioVolatility(List<decimal> securityReturns)
        {
            if (securityReturns == null || securityReturns.Count < 2)
                throw new ArgumentException("At least two returns required to calculate portfolio volatility");

            // Calculate weighted average volatility
            var totalWeight = securityReturns.Count;
            var weightedVolatility = securityReturns
                .Select(r => r / totalWeight)
                .Sum();

            return weightedVolatility;
        }

        /// <summary>
        /// Calculates portfolio metrics based on prediction.
        /// </summary>
        /// <param name="portfolio">Portfolio context</param>
        /// <param name="predictedReturn">Predicted portfolio return</param>
        /// <returns>Portfolio metrics</returns>
        private PortfolioMetrics CalculatePortfolioMetrics(Portfolio portfolio, decimal predictedReturn)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            var riskLevel = CalculateRiskLevel(portfolio);
            var diversificationScore = CalculateDiversificationScore(portfolio);
            var sectorExposure = CalculateSectorExposure(portfolio);
            var styleExposure = CalculateStyleExposure(portfolio);
            var technicalScore = CalculateTechnicalScore(portfolio, predictedReturn);

            return new PortfolioMetrics
            {
                RiskLevel = riskLevel,
                DiversificationScore = diversificationScore,
                SectorExposure = sectorExposure,
                StyleExposure = styleExposure,
                TechnicalScore = technicalScore
            };
        }

        /// <summary>
        /// Calculates the risk level of a portfolio.
        /// </summary>
        /// <param name="portfolio">Portfolio to analyze</param>
        /// <returns>Risk level</returns>
        private decimal CalculateRiskLevel(Portfolio portfolio)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            var volatility = CalculateRiskMetrics(portfolio);
            if (volatility < 0.05m) return 1; // Low risk
            if (volatility < 0.1m) return 2; // Medium risk
            return 3; // High risk
        }

        /// <summary>
        /// Calculates the diversification score of a portfolio.
        /// </summary>
        /// <param name="portfolio">Portfolio to analyze</param>
        /// <returns>Diversification score</returns>
        private decimal CalculateDiversificationScore(Portfolio portfolio)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            var uniqueSectors = portfolio.Positions
                .Select(p => p.Sector)
                .Distinct()
                .Count();

            var uniqueStyles = portfolio.Positions
                .Select(p => p.Style)
                .Distinct()
                .Count();

            return (uniqueSectors * 0.6m) + (uniqueStyles * 0.4m);
        }

        /// <summary>
        /// Calculates the technical score for a portfolio.
        /// </summary>
        /// <param name="portfolio">Portfolio context</param>
        /// <param name="predictedReturn">Predicted return</param>
        /// <returns>Technical score</returns>
        private decimal CalculateTechnicalScore(Portfolio portfolio, decimal predictedReturn)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            var trendScore = portfolio.Positions
                .Average(p => p.TrendScore ?? 0);

            var momentumScore = portfolio.Positions
                .Average(p => p.MomentumScore ?? 0);

            return (trendScore * 0.6m) + (momentumScore * 0.4m);
        }

        /// <summary>
        /// Calculates the confidence score for a prediction.
        /// </summary>
        /// <param name="score">Prediction score</param>
        /// <returns>Confidence score between 0 and 1</returns>
        private float CalculateConfidenceScore(decimal score)
        {
            // TODO: Implement actual confidence score calculation
            return (float)Math.Abs(score) / 100; // Simplified example
        }

        /// <summary>
        /// Generates an explanation for the prediction.
        /// </summary>
        /// <param name="score">Prediction score</param>
        /// <param name="portfolio">Portfolio context</param>
        /// <returns>Explanation of the prediction</returns>
        private string GenerateExplanation(decimal score, Portfolio portfolio)
        {
            // TODO: Implement actual explanation generation
            return $"The predicted return of {score}% for the portfolio is based on current market conditions and portfolio composition.";
        }

        /// <summary>
        /// Gets current market sentiment.
        /// </summary>
        /// <returns>Market sentiment score</returns>
        private decimal GetMarketSentiment()
        {
            // TODO: Implement actual market sentiment analysis
            return 0.5m; // Neutral sentiment for now
        }

        /// <summary>
        /// Gets current economic context.
        /// </summary>
        /// <returns>Economic context</returns>
        private EconomicContext GetEconomicContext()
        {
            // TODO: Implement actual economic context retrieval
            return new EconomicContext
            {
                MacroeconomicIndex = 1.0m,
                InterestRates = 0.05m,
                InflationRate = 0.02m
            };
        }

        /// <summary>
        /// Monitors the performance of the portfolio prediction model.
        /// </summary>
        /// <param name="predictions">List of predictions to evaluate</param>
        /// <param name="actualReturns">Actual returns for comparison</param>
        /// <returns>Performance metrics</returns>
        private PortfolioPredictionMetrics MonitorPortfolioPerformance(
            IEnumerable<PortfolioPredictionPerformance> predictions,
            IEnumerable<decimal> actualReturns)
        {
            var perfList = predictions.ToList();
            var actualList = actualReturns.ToList();

            if (perfList.Count != actualList.Count)
                throw new ArgumentException("Prediction and actual return counts must match");

            var totalError = 0m;
            var totalSquaredError = 0m;
            var totalAbsoluteError = 0m;
            var totalPercentageError = 0m;
            var totalReturn = 0m;
            var totalVariance = 0m;
            var totalDownsideVariance = 0m;
            var maxDrawdown = 0m;
            var peak = 0m;

            for (int i = 0; i < perfList.Count; i++)
            {
                var pred = perfList[i];
                var actual = actualList[i];

                var error = actual - pred.PredictedReturn;
                var absError = Math.Abs(error);
                var percentageError = absError / Math.Abs(actual);

                totalError += error;
                totalSquaredError += error * error;
                totalAbsoluteError += absError;
                totalPercentageError += percentageError;
                totalReturn += actual;

                // Calculate downside variance for Sortino ratio
                if (error < 0)
                    totalDownsideVariance += error * error;

                // Calculate drawdown
                var currentReturn = totalReturn / (i + 1);
                if (currentReturn > peak)
                    peak = currentReturn;
                var drawdown = (peak - currentReturn) / peak;
                if (drawdown > maxDrawdown)
                    maxDrawdown = drawdown;
            }

            var n = perfList.Count;
            var meanReturn = totalReturn / n;
            var meanError = totalError / n;
            var meanSquaredError = totalSquaredError / n;
            var meanAbsoluteError = totalAbsoluteError / n;
            var meanPercentageError = totalPercentageError / n;
            var variance = totalSquaredError / (n - 1);
            var downsideVariance = totalDownsideVariance / (n - 1);

            var r2Score = 1 - (totalSquaredError / (variance * n));
            var sharpeRatio = meanReturn / Math.Sqrt(variance);
            var sortinoRatio = meanReturn / Math.Sqrt(downsideVariance);

            return new PortfolioPredictionMetrics
            {
                MeanAbsoluteError = meanAbsoluteError,
                RootMeanSquaredError = (decimal)Math.Sqrt((double)meanSquaredError),
                MeanAbsolutePercentageError = meanPercentageError * 100,
                R2Score = r2Score,
                SharpeRatio = sharpeRatio,
                SortinoRatio = sortinoRatio,
                MaximumDrawdown = maxDrawdown,
                StartDate = perfList.First().PredictionDate,
                EndDate = perfList.Last().PredictionDate
            };
        }

        /// <summary>
        /// Gets historical portfolio data for a specified period.
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Historical portfolio data</returns>
        private async Task<List<HistoricalPortfolioData>> GetHistoricalPortfolioDataAsync(
            string portfolioId,
            DateTime startDate,
            DateTime endDate)
        {
            // TODO: Implement actual historical portfolio data retrieval
            return new List<HistoricalPortfolioData>();
        }

        /// <summary>
        /// Calculates portfolio return for a period.
        /// </summary>
        /// <param name="portfolioData">Portfolio data for the period</param>
        /// <returns>Portfolio return</returns>
        private decimal CalculatePortfolioReturn(List<HistoricalPortfolioData> portfolioData)
        {
            if (portfolioData == null || portfolioData.Count < 2)
                throw new ArgumentException("At least two portfolio data points required to calculate return");

            var startValue = portfolioData.First().TotalValue;
            var endValue = portfolioData.Last().TotalValue;
            
            return (endValue - startValue) / startValue;
        }

        /// <summary>
        /// Gets historical portfolio data.
        /// </summary>
        /// <param name="portfolioId">Portfolio ID</param>
        /// <param name="date">Date of the data point</param>
        /// <returns>Historical portfolio data</returns>
        private class HistoricalPortfolioData
        {
            public string PortfolioId { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public decimal TotalValue { get; set; }
            public List<PortfolioPosition> Positions { get; set; } = new();
            public decimal[] SectorExposure { get; set; } = Array.Empty<decimal>();
            public decimal[] StyleExposure { get; set; } = Array.Empty<decimal>();
            public decimal RiskLevel { get; set; }
            public decimal DiversificationScore { get; set; }
            public decimal TechnicalScore { get; set; }
        }

        /// <summary>
        /// Represents a portfolio position in historical data.
        /// </summary>
        private class PortfolioPosition
        {
            public string Symbol { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal CurrentPrice { get; set; }
            public string Sector { get; set; } = string.Empty;
            public string Style { get; set; } = string.Empty;
            public decimal? TrendScore { get; set; }
            public decimal? MomentumScore { get; set; }
            public List<decimal> PriceHistory { get; set; } = new();
        }

        /// <summary>
        /// Represents portfolio prediction performance.
        /// </summary>
        private class PortfolioPredictionPerformance
        {
            public string PortfolioId { get; set; } = string.Empty;
            public DateTime PredictionDate { get; set; }
            public decimal PredictedReturn { get; set; }
            public decimal ActualReturn { get; set; }
            public decimal Error { get; set; }
            public decimal AbsoluteError { get; set; }
            public decimal SquaredError { get; set; }
        }

        /// <summary>
        /// Represents portfolio prediction metrics.
        /// </summary>
        private class PortfolioPredictionMetrics
        {
            public decimal MeanAbsoluteError { get; set; }
            public decimal RootMeanSquaredError { get; set; }
            public decimal MeanAbsolutePercentageError { get; set; }
            public decimal R2Score { get; set; }
            public decimal SharpeRatio { get; set; }
            public decimal SortinoRatio { get; set; }
            public decimal MaximumDrawdown { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }
    }
}
