/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using Microsoft.ML;
using SmartPortfolioAnalyzer.Services.Analysis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartPortfolioAnalyzer.Services.Analysis.Services
{
    public interface IFeatureEngineeringService
    {
        Task<T> TransformDataAsync<T>(IEnumerable<T> data) where T : class;
        Task<T> TransformSingleAsync<T>(T data) where T : class;
    }

    public class FeatureEngineeringService : IFeatureEngineeringService
    {
        private readonly MLContext _mlContext;
        private readonly FeatureEngineeringConfig _config;

        public FeatureEngineeringService(MLContext mlContext, FeatureEngineeringConfig config)
        {
            _mlContext = mlContext;
            _config = config;
        }

        /// <summary>
        /// Transforms a collection of data points using MVP feature engineering techniques.
        /// </summary>
        /// <typeparam name="T">Type of data to transform</typeparam>
        /// <param name="data">Data collection to transform</param>
        /// <returns>Transformed data collection</returns>
        public async Task<T> TransformDataAsync<T>(IEnumerable<T> data) where T : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var transformedData = data.ToList();

            // Add technical features
            transformedData = await AddTechnicalFeaturesAsync(transformedData);
            
            // Add economic features
            transformedData = await AddEconomicFeaturesAsync(transformedData);

            return transformedData;
        }

        /// <summary>
        /// Transforms a single data point using MVP feature engineering techniques.
        /// </summary>
        /// <typeparam name="T">Type of data to transform</typeparam>
        /// <param name="data">Data point to transform</param>
        /// <returns>Transformed data point</returns>
        public async Task<T> TransformSingleAsync<T>(T data) where T : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Add technical features
            data = await AddTechnicalFeaturesAsync(data);
            
            // Add economic features
            data = await AddEconomicFeaturesAsync(data);

            return data;
        }

        /// <summary>
        /// Adds MVP technical analysis features to the data.
        /// </summary>
        /// <typeparam name="T">Type of data to transform</typeparam>
        /// <param name="data">Data to transform</param>
        /// <returns>Data with added technical features</returns>
        public async Task<T> AddTechnicalFeaturesAsync<T>(T data) where T : class
        {
            // Extract price data
            var prices = ExtractPrices(data);
            
            // Calculate basic technical indicators
            var technicalIndicators = CalculateTechnicalIndicators(prices);
            
            // Add features to data
            AddTechnicalFeaturesToData(data, technicalIndicators);

            return data;
        }

        /// <summary>
        /// Adds MVP economic features to the data.
        /// </summary>
        /// <typeparam name="T">Type of data to transform</typeparam>
        /// <param name="data">Data to transform</param>
        /// <returns>Data with added economic features</returns>
        public async Task<T> AddEconomicFeaturesAsync<T>(T data) where T : class
        {
            // Get basic economic indicators
            var economicContext = await GetEconomicContextAsync();
            
            // Add features to data
            AddEconomicFeaturesToData(data, economicContext);

            return data;
        }

        /// <summary>
        /// Extracts price data from the input.
        /// </summary>
        /// <typeparam name="T">Type of data containing prices</typeparam>
        /// <param name="data">Data containing prices</param>
        /// <returns>List of prices</returns>
        private List<decimal> ExtractPrices<T>(T data) where T : class
        {
            // TODO: Implement price extraction logic
            return new List<decimal>();
        }

        /// <summary>
        /// Calculates MVP technical indicators from price data.
        /// </summary>
        /// <param name="prices">Price data</param>
        /// <returns>Technical indicators</returns>
        private TechnicalIndicators CalculateTechnicalIndicators(List<decimal> prices)
        {
            if (prices == null || prices.Count < 2)
                throw new ArgumentException("At least two prices required for technical indicators");

            var indicators = new TechnicalIndicators();

            // Calculate simple moving averages
            indicators.ShortMA = CalculateMovingAverage(prices, _config.ShortMADays);
            indicators.LongMA = CalculateMovingAverage(prices, _config.LongMADays);

            // Calculate RSI
            indicators.RSI = CalculateRSI(prices);

            // Calculate volatility
            indicators.Volatility = CalculateVolatility(prices);

            return indicators;
        }

        /// <summary>
        /// Gets basic economic context.
        /// </summary>
        /// <returns>Economic context</returns>
        private async Task<EconomicContext> GetEconomicContextAsync()
        {
            // TODO: Implement actual economic context retrieval
            return new EconomicContext
            {
                MarketSentiment = 0.5m,
                InterestRates = 0.05m,
                InflationRate = 0.02m
            };
        }

        /// <summary>
        /// Adds technical features to the data.
        /// </summary>
        /// <typeparam name="T">Type of data to modify</typeparam>
        /// <param name="data">Data to modify</param>
        /// <param name="indicators">Technical indicators</param>
        private void AddTechnicalFeaturesToData<T>(T data, TechnicalIndicators indicators) where T : class
        {
            // TODO: Implement feature addition logic
        }

        /// <summary>
        /// Adds economic features to the data.
        /// </summary>
        /// <typeparam name="T">Type of data to modify</typeparam>
        /// <param name="data">Data to modify</param>
        /// <param name="context">Economic context</param>
        private void AddEconomicFeaturesToData<T>(T data, EconomicContext context) where T : class
        {
            // TODO: Implement feature addition logic
        }

        /// <summary>
        /// Calculates moving average from price data.
        /// </summary>
        /// <param name="prices">Price data</param>
        /// <param name="period">Period for moving average</param>
        /// <returns>Moving average</returns>
        private decimal CalculateMovingAverage(List<decimal> prices, int period)
        {
            if (prices == null || prices.Count < period)
                throw new ArgumentException($"At least {period} prices required for moving average");

            return prices.TakeLast(period).Average();
        }

        /// <summary>
        /// Calculates RSI from price data.
        /// </summary>
        /// <param name="prices">Price data</param>
        /// <returns>RSI value</returns>
        private decimal CalculateRSI(List<decimal> prices)
        {
            if (prices == null || prices.Count < 14) // Standard RSI period
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

        /// <summary>
        /// Calculates volatility from price data.
        /// </summary>
        /// <param name="prices">Price data</param>
        /// <returns>Volatility</returns>
        private decimal CalculateVolatility(List<decimal> prices)
        {
            if (prices == null || prices.Count < 2)
                throw new ArgumentException("At least two prices required to calculate volatility");

            var returns = new List<decimal>();
            for (int i = 1; i < prices.Count; i++)
            {
                var returnRate = (prices[i] - prices[i - 1]) / prices[i - 1];
                returns.Add(returnRate);
            }

            var mean = returns.Average();
            var sumOfSquares = returns.Select(x => (x - mean) * (x - mean)).Sum();
            return (decimal)Math.Sqrt((double)(sumOfSquares / (returns.Count - 1)));
        }
    }

    /// <summary>
    /// Represents technical indicators for MVP.
    /// </summary>
    public class TechnicalIndicators
    {
        public decimal ShortMA { get; set; }
        public decimal LongMA { get; set; }
        public decimal RSI { get; set; }
        public decimal Volatility { get; set; }
    }

    /// <summary>
    /// Represents economic context for MVP.
    /// </summary>
    public class EconomicContext
    {
        public decimal MarketSentiment { get; set; }
        public decimal InterestRates { get; set; }
        public decimal InflationRate { get; set; }
    }
}
