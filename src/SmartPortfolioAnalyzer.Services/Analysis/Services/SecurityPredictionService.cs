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
    public interface ISecurityPredictionService
    {
        Task<SecurityPredictionTrainingResult> TrainModelAsync(IEnumerable<SecurityPredictionData> historicalData);
        Task<SecurityPredictionResult> PredictSecurityPerformanceAsync(string symbol, Portfolio portfolio);
        Task<SecurityPredictionBacktestResult> BacktestSecurityPredictionAsync(string symbol, DateTime startDate, DateTime endDate);
    }

    public class SecurityPredictionService : ISecurityPredictionService
    {
        private readonly MLContext _mlContext;
        private readonly SecurityPredictionConfig _config;
        private ITransformer _model;

        public SecurityPredictionService(MLContext mlContext, SecurityPredictionConfig config)
        {
            _mlContext = mlContext;
            _config = config;
        }

        /// <summary>
        /// Builds the training pipeline for security prediction.
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
        /// Trains a new security prediction model using historical data.
        /// </summary>
        /// <param name="historicalData">Historical security data for training</param>
        /// <returns>Training statistics and model metrics</returns>
        public async Task<SecurityPredictionTrainingResult> TrainModelAsync(IEnumerable<SecurityPredictionData> historicalData)
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
            
            return new SecurityPredictionTrainingResult
            {
                Model = _model,
                TrainingMetrics = new SecurityPredictionMetrics
                {
                    R2Score = metrics.R2Score,
                    MeanAbsoluteError = metrics.MeanAbsoluteError,
                    RootMeanSquaredError = metrics.RootMeanSquaredError,
                    ExplainedVariance = metrics.ExplainedVariance
                }
            };
        }

        /// <summary>
        /// Predicts security performance for a given symbol and portfolio.
        /// </summary>
        /// <param name="symbol">Security symbol to predict</param>
        /// <param name="portfolio">Portfolio context</param>
        /// <returns>Security prediction result</returns>
        public async Task<SecurityPredictionResult> PredictSecurityPerformanceAsync(string symbol, Portfolio portfolio)
        {
            // Prepare security data from portfolio
            var securityData = PrepareSecurityData(symbol, portfolio);
            
            // Create a single-item collection for prediction
            var predictionData = _mlContext.Data.LoadFromEnumerable(new[] { securityData });
            
            // Get prediction using the trained model
            var prediction = _model.Transform(predictionData);
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<SecurityPredictionData, SecurityPredictionPrediction>(_model);
            var result = predictionEngine.Predict(securityData);
            
            // Calculate confidence score and generate explanation
            var confidenceScore = CalculateConfidenceScore(result.Score);
            var explanation = GenerateExplanation(result.Score, symbol, portfolio);
            
            return new SecurityPredictionResult
            {
                Symbol = symbol,
                Prediction = new SecurityPrediction
                {
                    ExpectedReturn = result.Score,
                    Volatility = securityData.Volatility,
                    RiskLevel = CalculateRiskLevel(securityData.Volatility),
                    TechnicalScore = CalculateTechnicalScore(securityData, portfolio)
                },
                ConfidenceScore = confidenceScore,
                PredictionExplanation = explanation,
                TimeHorizon = _config.PredictionHorizon,
                Metrics = new SecurityPredictionMetrics
                {
                    R2Score = 0.0f, // Not applicable for single prediction
                    MeanAbsoluteError = 0.0f,
                    RootMeanSquaredError = 0.0f,
                    ExplainedVariance = 0.0f
                }
            };
        }

        /// <summary>
        /// Prepares security data from portfolio for prediction.
        /// </summary>
        /// <param name="symbol">Security symbol</param>
        /// <param name="portfolio">Portfolio context</param>
        /// <returns>Prepared security data</returns>
        private SecurityPredictionData PrepareSecurityData(string symbol, Portfolio portfolio)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            // Find the security position in the portfolio
            var position = portfolio.Positions.FirstOrDefault(p => p.Symbol == symbol);
            if (position == null)
                throw new ArgumentException($"Security {symbol} not found in portfolio", nameof(symbol));

            // Calculate historical returns
            var historicalPrices = position.PriceHistory;
            if (historicalPrices == null || historicalPrices.Count < 2)
                throw new ArgumentException("Insufficient historical data for prediction", nameof(portfolio));

            var returns = CalculateReturns(historicalPrices);
            var volatility = CalculateVolatility(returns);
            var trend = CalculateTrend(returns);

            // Calculate economic indicators
            var economicContext = GetEconomicContext();
            var economicIndicators = CalculateEconomicIndicators(economicContext);

            // Calculate technical indicators
            var technicalIndicators = CalculateTechnicalIndicators(historicalPrices);

            return new SecurityPredictionData
            {
                HistoricalReturns = (float)returns.Last(),
                EconomicIndicators = (float)economicIndicators,
                MarketSentiment = (float)GetMarketSentiment(symbol),
                TechnicalIndicators = (float)technicalIndicators,
                Volatility = (float)volatility,
                MacroeconomicFactors = (float)economicContext.MacroeconomicIndex,
                ExpectedReturn = (float)trend
            };
        }

        private decimal CalculateReturns(List<decimal> prices)
        {
            if (prices == null || prices.Count < 2)
                throw new ArgumentException("At least two prices required to calculate returns");

            var returns = new List<decimal>();
            for (int i = 1; i < prices.Count; i++)
            {
                var returnRate = (prices[i] - prices[i - 1]) / prices[i - 1];
                returns.Add(returnRate);
            }

            return returns.Last();
        }

        private decimal CalculateVolatility(List<decimal> returns)
        {
            if (returns == null || returns.Count < 2)
                throw new ArgumentException("At least two returns required to calculate volatility");

            var mean = returns.Average();
            var sumOfSquares = returns.Select(x => (x - mean) * (x - mean)).Sum();
            return (decimal)Math.Sqrt((double)(sumOfSquares / (returns.Count - 1)));
        }

        private decimal CalculateTrend(List<decimal> returns)
        {
            if (returns == null || returns.Count < 2)
                throw new ArgumentException("At least two returns required to calculate trend");

            var trend = returns.Last() - returns.First();
            return trend;
        }

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

        private decimal CalculateEconomicIndicators(EconomicContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Simple weighted average of economic factors
            return (context.MacroeconomicIndex * 0.5m) + 
                   (context.InterestRates * 0.3m) + 
                   (context.InflationRate * 0.2m);
        }

        private decimal CalculateTechnicalIndicators(List<decimal> prices)
        {
            if (prices == null || prices.Count < 2)
                throw new ArgumentException("At least two prices required for technical indicators");

            var movingAverage = CalculateMovingAverage(prices);
            var rsi = CalculateRSI(prices);
            
            return (movingAverage * 0.6m) + (rsi * 0.4m);
        }

        private decimal CalculateMovingAverage(List<decimal> prices)
        {
            if (prices == null || prices.Count < 20) // Using 20-day MA
                throw new ArgumentException("At least 20 prices required for moving average");

            return prices.TakeLast(20).Average();
        }

        private decimal CalculateRSI(List<decimal> prices)
        {
            if (prices == null || prices.Count < 14) // Using 14-day RSI
                throw new ArgumentException("At least 14 prices required for RSI");

            var gains = new List<decimal>();
            var losses = new List<decimal>();

            for (int i = 1; i < prices.Count; i++)
            {
                var change = prices[i] - prices[i - 1];
                if (change > 0)
                    gains.Add(change);
                else
                    losses.Add(-change);
            }

            var averageGain = gains.TakeLast(14).Average();
            var averageLoss = losses.TakeLast(14).Average();

            if (averageLoss == 0)
                return 100m;

            var rs = averageGain / averageLoss;
            return 100m - (100m / (1m + rs));
        }

        private decimal GetMarketSentiment(string symbol)
        {
            // TODO: Implement actual market sentiment analysis
            return 0.5m; // Neutral sentiment for now
        }

        /// <summary>
        /// Calculates the confidence score for a prediction.
        /// </summary>
        /// <param name="score">Prediction score</param>
        /// <returns>Confidence score between 0 and 1</returns>
        private float CalculateConfidenceScore(float score)
        {
            // TODO: Implement confidence score calculation
            return Math.Abs(score) / 100; // Simplified example
        }

        /// <summary>
        /// Generates an explanation for the prediction.
        /// </summary>
        /// <param name="score">Prediction score</param>
        /// <param name="symbol">Security symbol</param>
        /// <param name="portfolio">Portfolio context</param>
        /// <returns>Explanation of the prediction</returns>
        private string GenerateExplanation(float score, string symbol, Portfolio portfolio)
        {
            // TODO: Implement explanation generation
            return $"The predicted return of {score}% for {symbol} is based on current market conditions and portfolio composition.";
        }

        /// <summary>
        /// Calculates the risk level based on volatility.
        /// </summary>
        /// <param name="volatility">Security volatility</param>
        /// <returns>Risk level</returns>
        private decimal CalculateRiskLevel(decimal volatility)
        {
            if (volatility < 0.05m) return 1; // Low risk
            if (volatility < 0.1m) return 2; // Medium risk
            return 3; // High risk
        }

        /// <summary>
        /// Calculates the technical score for a security.
        /// </summary>
        /// <param name="data">Security prediction data</param>
        /// <param name="portfolio">Portfolio context</param>
        /// <returns>Technical score</returns>
        private decimal CalculateTechnicalScore(SecurityPredictionData data, Portfolio portfolio)
        {
            // TODO: Implement technical score calculation
            return data.Trend / data.Volatility;
        }

        private class SecurityPredictionPerformance
        {
            public decimal ActualReturn { get; set; }
            public decimal PredictedReturn { get; set; }
            public decimal Error { get; set; }
            public decimal AbsoluteError { get; set; }
            public decimal SquaredError { get; set; }
            public DateTime PredictionDate { get; set; }
            public string Symbol { get; set; } = string.Empty;
        }

        private class SecurityPredictionMetrics
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

        /// <summary>
        /// Monitors the performance of the security prediction model.
        /// </summary>
        /// <param name="predictions">List of predictions to evaluate</param>
        /// <param name="actualReturns">Actual returns for comparison</param>
        /// <returns>Performance metrics</returns>
        private SecurityPredictionMetrics MonitorModelPerformance(
            IEnumerable<SecurityPredictionPerformance> predictions,
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

            return new SecurityPredictionMetrics
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
        /// Backtests the security prediction model over a specified period.
        /// </summary>
        /// <param name="symbol">Security symbol to backtest</param>
        /// <param name="startDate">Start date for backtesting</param>
        /// <param name="endDate">End date for backtesting</param>
        /// <returns>Backtest results including performance metrics</returns>
        public async Task<SecurityPredictionBacktestResult> BacktestSecurityPredictionAsync(
            string symbol,
            DateTime startDate,
            DateTime endDate)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

            if (startDate >= endDate)
                throw new ArgumentException("Start date must be before end date");

            // Get historical data for the period
            var historicalData = await GetHistoricalDataAsync(symbol, startDate, endDate);
            if (historicalData == null || !historicalData.Any())
                throw new ArgumentException("No historical data available for the specified period");

            // Prepare predictions
            var predictions = new List<SecurityPredictionPerformance>();
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
                var predictionData = PrepareSecurityData(symbol, windowData);
                var predictionDataEnumerable = _mlContext.Data.LoadFromEnumerable(new[] { predictionData });
                
                // Get prediction
                var prediction = _model.Transform(predictionDataEnumerable);
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<SecurityPredictionData, SecurityPredictionPrediction>(_model);
                var result = predictionEngine.Predict(predictionData);

                // Get actual return for next period
                var nextPeriodData = historicalData
                    .Where(d => d.Date > currentDate && d.Date <= currentDate.AddDays(_config.PredictionHorizon))
                    .OrderBy(d => d.Date)
                    .ToList();

                if (nextPeriodData.Count < 2)
                    break;

                var actualReturn = CalculateReturns(nextPeriodData.Select(d => d.Price).ToList());
                
                // Store results
                predictions.Add(new SecurityPredictionPerformance
                {
                    Symbol = symbol,
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
            var metrics = MonitorModelPerformance(predictions, actualReturns);

            return new SecurityPredictionBacktestResult
            {
                Symbol = symbol,
                StartDate = startDate,
                EndDate = endDate,
                Predictions = predictions,
                PerformanceMetrics = metrics,
                ModelConfiguration = _config
            };
        }

        /// <summary>
        /// Gets historical data for a security.
        /// </summary>
        /// <param name="symbol">Security symbol</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Historical data for the specified period</returns>
        private async Task<List<HistoricalPriceData>> GetHistoricalDataAsync(
            string symbol,
            DateTime startDate,
            DateTime endDate)
        {
            // TODO: Implement actual historical data retrieval
            return new List<HistoricalPriceData>();
        }

        /// <summary>
        /// Gets historical price data for a security.
        /// </summary>
        /// <param name="symbol">Security symbol</param>
        /// <param name="date">Date of the data point</param>
        /// <returns>Historical price data</returns>
        private class HistoricalPriceData
        {
            public DateTime Date { get; set; }
            public decimal Price { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Volume { get; set; }
        }
    }
}
