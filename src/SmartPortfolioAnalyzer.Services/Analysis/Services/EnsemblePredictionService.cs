/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using Microsoft.ML;
using SmartPortfolioAnalyzer.Services.Analysis.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartPortfolioAnalyzer.Services.Analysis.Services
{
    public interface IEnsemblePredictionService
    {
        Task<EnsemblePredictionResult> PredictEnsemblePortfolioPerformanceAsync(Portfolio portfolio);
        Task<EnsembleBacktestResult> BacktestEnsemblePredictionAsync(
            string portfolioId,
            DateTime startDate,
            DateTime endDate);
    }

    public class EnsemblePredictionService : IEnsemblePredictionService
    {
        private readonly IMarketPredictionService _marketService;
        private readonly ISecurityPredictionService _securityService;
        private readonly IPortfolioPredictionService _portfolioService;
        private readonly MLContext _mlContext;
        private readonly EnsemblePredictionConfig _config;
        private ITransformer _ensembleModel;

        public EnsemblePredictionService(
            IMarketPredictionService marketService,
            ISecurityPredictionService securityService,
            IPortfolioPredictionService portfolioService,
            MLContext mlContext,
            EnsemblePredictionConfig config)
        {
            _marketService = marketService;
            _securityService = securityService;
            _portfolioService = portfolioService;
            _mlContext = mlContext;
            _config = config;
        }

        /// <summary>
        /// Builds the ensemble prediction pipeline.
        /// </summary>
        /// <returns>The ML.NET ensemble pipeline</returns>
        private IEstimator<ITransformer> BuildEnsemblePipeline()
        {
            // Start with basic feature concatenation
            var pipeline = _mlContext.Transforms.Concatenate("Features", _config.FeatureColumns)
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"));
            
            return pipeline;
        }

        /// <summary>
        /// Trains the ensemble model using historical data.
        /// </summary>
        /// <param name="historicalData">Historical ensemble data</param>
        /// <returns>Training result including model and metrics</returns>
        public async Task<EnsembleTrainingResult> TrainEnsembleModelAsync(IEnumerable<EnsemblePredictionData> historicalData)
        {
            // Load the data collection into MLContext
            var trainingData = _mlContext.Data.LoadFromEnumerable(historicalData);
            
            // Build our ensemble pipeline
            var pipeline = BuildEnsemblePipeline();
            
            // Train the ensemble model
            _ensembleModel = pipeline.Fit(trainingData);
            
            // Get predictions on the training data
            var predictions = _ensembleModel.Transform(trainingData);
            
            // Evaluate the ensemble model
            var metrics = _mlContext.Regression.Evaluate(predictions);
            
            return new EnsembleTrainingResult
            {
                Model = _ensembleModel,
                TrainingMetrics = new EnsemblePredictionMetrics
                {
                    R2Score = metrics.R2Score,
                    MeanAbsoluteError = metrics.MeanAbsoluteError,
                    RootMeanSquaredError = metrics.RootMeanSquaredError,
                    ExplainedVariance = metrics.ExplainedVariance
                }
            };
        }

        /// <summary>
        /// Predicts portfolio performance using ensemble of models.
        /// </summary>
        /// <param name="portfolio">Portfolio to predict</param>
        /// <returns>Ensemble prediction result</returns>
        public async Task<EnsemblePredictionResult> PredictEnsemblePortfolioPerformanceAsync(Portfolio portfolio)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            // Get individual model predictions
            var marketPrediction = await _marketService.PredictMarketConditionsAsync();
            var securityPredictions = new Dictionary<string, SecurityPredictionResult>();
            
            foreach (var position in portfolio.Positions)
            {
                var prediction = await _securityService.PredictSecurityPerformanceAsync(
                    position.Symbol, portfolio);
                securityPredictions[position.Symbol] = prediction;
            }

            var portfolioPrediction = await _portfolioService.PredictPortfolioPerformanceAsync(portfolio);

            // Prepare ensemble data
            var ensembleData = PrepareEnsembleData(
                marketPrediction,
                securityPredictions,
                portfolioPrediction,
                portfolio);

            // Create a single-item collection for prediction
            var predictionData = _mlContext.Data.LoadFromEnumerable(new[] { ensembleData });
            
            // Get ensemble prediction
            var prediction = _ensembleModel.Transform(predictionData);
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<EnsemblePredictionData, EnsemblePredictionPrediction>(_ensembleModel);
            var result = predictionEngine.Predict(ensembleData);

            // Calculate ensemble metrics
            var ensembleMetrics = CalculateEnsembleMetrics(
                marketPrediction,
                securityPredictions.Values,
                portfolioPrediction,
                result.Score);

            return new EnsemblePredictionResult
            {
                PortfolioId = portfolio.Id,
                IndividualPredictions = new IndividualPredictions
                {
                    MarketPrediction = marketPrediction,
                    SecurityPredictions = securityPredictions,
                    PortfolioPrediction = portfolioPrediction
                },
                EnsemblePrediction = new EnsemblePrediction
                {
                    ExpectedReturn = result.Score,
                    ConfidenceScore = CalculateConfidenceScore(result.Score),
                    RiskLevel = ensembleMetrics.RiskLevel,
                    DiversificationScore = ensembleMetrics.DiversificationScore,
                    SectorExposure = ensembleMetrics.SectorExposure,
                    StyleExposure = ensembleMetrics.StyleExposure,
                    TechnicalScore = ensembleMetrics.TechnicalScore
                },
                TimeHorizon = _config.PredictionHorizon,
                Metrics = new EnsemblePredictionMetrics
                {
                    R2Score = 0.0f, // Not applicable for single prediction
                    MeanAbsoluteError = 0.0f,
                    RootMeanSquaredError = 0.0f,
                    ExplainedVariance = 0.0f
                }
            };
        }

        /// <summary>
        /// Prepares ensemble data by combining individual model predictions.
        /// </summary>
        /// <param name="marketPrediction">Market prediction result</param>
        /// <param name="securityPredictions">Security predictions</param>
        /// <param name="portfolioPrediction">Portfolio prediction result</param>
        /// <param name="portfolio">Portfolio context</param>
        /// <returns>Prepared ensemble data</returns>
        private EnsemblePredictionData PrepareEnsembleData(
            MarketPredictionResult marketPrediction,
            Dictionary<string, SecurityPredictionResult> securityPredictions,
            PortfolioPredictionResult portfolioPrediction,
            Portfolio portfolio)
        {
            // Calculate weighted averages of individual predictions
            var marketWeight = _config.MarketWeight;
            var securityWeight = _config.SecurityWeight;
            var portfolioWeight = _config.PortfolioWeight;

            var totalWeight = marketWeight + securityWeight + portfolioWeight;

            var weightedMarket = marketPrediction.PredictedReturn * (marketWeight / totalWeight);
            var weightedPortfolio = portfolioPrediction.Prediction.ExpectedReturn * (portfolioWeight / totalWeight);
            var weightedSecurity = securityPredictions.Values
                .Average(p => p.Prediction.ExpectedReturn * (securityWeight / totalWeight));

            // Calculate ensemble features
            var ensembleFeatures = new EnsembleFeatures
            {
                MarketFeatures = new[]
                {
                    marketPrediction.PredictedReturn,
                    marketPrediction.ConfidenceScore,
                    marketPrediction.Metrics.R2Score
                },
                SecurityFeatures = securityPredictions.Values
                    .Select(p => new[]
                    {
                        p.Prediction.ExpectedReturn,
                        p.ConfidenceScore,
                        p.Prediction.Volatility
                    })
                    .ToArray(),
                PortfolioFeatures = new[]
                {
                    portfolioPrediction.Prediction.ExpectedReturn,
                    portfolioPrediction.ConfidenceScore,
                    portfolioPrediction.Prediction.RiskLevel
                }
            };

            return new EnsemblePredictionData
            {
                MarketFeatures = ensembleFeatures.MarketFeatures,
                SecurityFeatures = ensembleFeatures.SecurityFeatures,
                PortfolioFeatures = ensembleFeatures.PortfolioFeatures,
                PortfolioValue = portfolio.TotalValue,
                SectorExposure = portfolioPrediction.Prediction.SectorExposure,
                StyleExposure = portfolioPrediction.Prediction.StyleExposure,
                RiskLevel = portfolioPrediction.Prediction.RiskLevel,
                DiversificationScore = portfolioPrediction.Prediction.DiversificationScore
            };
        }

        /// <summary>
        /// Calculates ensemble metrics by combining individual model metrics.
        /// </summary>
        /// <param name="marketPrediction">Market prediction result</param>
        /// <param name="securityPredictions">Security predictions</param>
        /// <param name="portfolioPrediction">Portfolio prediction result</param>
        /// <param name="ensembleScore">Ensemble prediction score</param>
        /// <returns>Ensemble metrics</returns>
        private EnsembleMetrics CalculateEnsembleMetrics(
            MarketPredictionResult marketPrediction,
            IEnumerable<SecurityPredictionResult> securityPredictions,
            PortfolioPredictionResult portfolioPrediction,
            decimal ensembleScore)
        {
            // Calculate weighted averages of individual metrics
            var marketWeight = _config.MarketWeight;
            var securityWeight = _config.SecurityWeight;
            var portfolioWeight = _config.PortfolioWeight;

            var totalWeight = marketWeight + securityWeight + portfolioWeight;

            // Calculate ensemble risk level
            var marketRisk = CalculateRiskLevel(marketPrediction.PredictedReturn);
            var portfolioRisk = portfolioPrediction.Prediction.RiskLevel;
            var securityRisk = securityPredictions
                .Average(p => p.Prediction.RiskLevel);

            var ensembleRisk = (marketRisk * (marketWeight / totalWeight)) +
                              (portfolioRisk * (portfolioWeight / totalWeight)) +
                              (securityRisk * (securityWeight / totalWeight));

            // Calculate ensemble diversification
            var portfolioDiversification = portfolioPrediction.Prediction.DiversificationScore;
            var securityDiversification = securityPredictions
                .Average(p => p.Prediction.DiversificationScore ?? 0);

            var ensembleDiversification = (portfolioDiversification * (portfolioWeight / totalWeight)) +
                                        (securityDiversification * (securityWeight / totalWeight));

            // Calculate ensemble technical score
            var portfolioTech = portfolioPrediction.Prediction.TechnicalScore;
            var securityTech = securityPredictions
                .Average(p => p.Prediction.TechnicalScore ?? 0);

            var ensembleTech = (portfolioTech * (portfolioWeight / totalWeight)) +
                             (securityTech * (securityWeight / totalWeight));

            return new EnsembleMetrics
            {
                RiskLevel = ensembleRisk,
                DiversificationScore = ensembleDiversification,
                SectorExposure = portfolioPrediction.Prediction.SectorExposure,
                StyleExposure = portfolioPrediction.Prediction.StyleExposure,
                TechnicalScore = ensembleTech
            };
        }

        /// <summary>
        /// Backtests the ensemble prediction model over a specified period.
        /// </summary>
        /// <param name="portfolioId">Portfolio ID to backtest</param>
        /// <param name="startDate">Start date for backtesting</param>
        /// <param name="endDate">End date for backtesting</param>
        /// <returns>Backtest results including performance metrics</returns>
        public async Task<EnsembleBacktestResult> BacktestEnsemblePredictionAsync(
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
            var predictions = new List<EnsemblePredictionPerformance>();
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

                // Prepare individual model predictions
                var portfolio = windowData.Last();
                var marketPrediction = await _marketService.PredictMarketConditionsAsync();
                var portfolioPrediction = await _portfolioService.PredictPortfolioPerformanceAsync(portfolio);
                
                var securityPredictions = new Dictionary<string, SecurityPredictionResult>();
                foreach (var position in portfolio.Positions)
                {
                    var prediction = await _securityService.PredictSecurityPerformanceAsync(
                        position.Symbol, portfolio);
                    securityPredictions[position.Symbol] = prediction;
                }

                // Prepare ensemble data
                var ensembleData = PrepareEnsembleData(
                    marketPrediction,
                    securityPredictions,
                    portfolioPrediction,
                    portfolio);

                var predictionDataEnumerable = _mlContext.Data.LoadFromEnumerable(new[] { ensembleData });
                
                // Get ensemble prediction
                var prediction = _ensembleModel.Transform(predictionDataEnumerable);
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<EnsemblePredictionData, EnsemblePredictionPrediction>(_ensembleModel);
                var result = predictionEngine.Predict(ensembleData);

                // Get actual return for next period
                var nextPeriodData = historicalData
                    .Where(d => d.Date > currentDate && d.Date <= currentDate.AddDays(_config.PredictionHorizon))
                    .OrderBy(d => d.Date)
                    .ToList();

                if (nextPeriodData.Count < 2)
                    break;

                var actualReturn = CalculatePortfolioReturn(nextPeriodData);
                
                // Store results
                predictions.Add(new EnsemblePredictionPerformance
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
            var metrics = MonitorEnsemblePerformance(predictions, actualReturns);

            return new EnsembleBacktestResult
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
        /// Monitors the performance of the ensemble prediction model.
        /// </summary>
        /// <param name="predictions">List of predictions to evaluate</param>
        /// <param name="actualReturns">Actual returns for comparison</param>
        /// <returns>Performance metrics</returns>
        private EnsemblePredictionMetrics MonitorEnsemblePerformance(
            IEnumerable<EnsemblePredictionPerformance> predictions,
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

            return new EnsemblePredictionMetrics
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
        /// Calculates the confidence score for an ensemble prediction.
        /// </summary>
        /// <param name="score">Prediction score</param>
        /// <returns>Confidence score between 0 and 1</returns>
        private float CalculateConfidenceScore(decimal score)
        {
            // Weighted combination of individual model confidence scores
            var marketWeight = _config.MarketWeight;
            var securityWeight = _config.SecurityWeight;
            var portfolioWeight = _config.PortfolioWeight;

            var totalWeight = marketWeight + securityWeight + portfolioWeight;

            return (float)(
                (score * (marketWeight / totalWeight)) +
                (score * (securityWeight / totalWeight)) +
                (score * (portfolioWeight / totalWeight)));
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
    }
}
