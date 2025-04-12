/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using SmartPortfolioAnalyzer.Core.Models;

/// <summary>
/// Provides risk calculation methods for portfolio analysis
/// </summary>
/// <remarks>
/// This class contains statistical calculations for portfolio risk metrics,
/// including volatility, drawdown, and correlation analysis.
/// </remarks>
namespace SmartPortfolioAnalyzer.Services.Portfolio.Services
{
    /// <summary>
    /// Provides statistical calculations for portfolio risk metrics
    /// </summary>
    public static class RiskCalculations
    {
        /// <summary>
        /// Calculates the portfolio volatility (standard deviation of returns)
        /// </summary>
        /// <param name="returns">A collection of daily returns</param>
        /// <returns>The annualized volatility percentage</returns>
        /// <remarks>
        /// Volatility is calculated as the standard deviation of daily returns,
        /// annualized by multiplying by the square root of the number of trading days.
        /// </remarks>
        public static decimal CalculateVolatility(IEnumerable<decimal> returns)
        {
            if (!returns.Any()) return 0;

            var meanReturn = returns.Average();
            var squaredDeviations = returns.Select(r => Math.Pow((double)(r - meanReturn), 2));
            var variance = (decimal)squaredDeviations.Average();
            var dailyVolatility = (decimal)Math.Sqrt((double)variance);
            
            // Annualize volatility (assuming 252 trading days)
            return dailyVolatility * (decimal)Math.Sqrt(252);
        }

        /// <summary>
        /// Calculates the maximum drawdown of a portfolio
        /// </summary>
        /// <param name="values">A collection of portfolio values over time</param>
        /// <returns>The maximum drawdown percentage</returns>
        /// <remarks>
        /// Maximum drawdown is the maximum observed loss from a peak to a trough
        /// before a new peak is attained.
        /// </remarks>
        public static decimal CalculateMaxDrawdown(IEnumerable<decimal> values)
        {
            if (!values.Any()) return 0;

            var maxDrawdown = 0m;
            var peak = values.First();

            foreach (var value in values)
            {
                if (value > peak)
                {
                    peak = value;
                }
                else
                {
                    var drawdown = (peak - value) / peak;
                    if (drawdown > maxDrawdown)
                    {
                        maxDrawdown = drawdown;
                    }
                }
            }

            return maxDrawdown;
        }

        /// <summary>
        /// Calculates the correlation coefficient between two time series
        /// </summary>
        /// <param name="series1">First time series of values</param>
        /// <param name="series2">Second time series of values</param>
        /// <returns>The correlation coefficient (-1 to 1)</returns>
        /// <remarks>
        /// Correlation coefficient measures the linear relationship between two datasets.
        /// A value of 1 means perfect positive correlation, -1 means perfect negative correlation,
        /// and 0 means no correlation.
        /// </remarks>
        public static decimal CalculateCorrelation(IEnumerable<decimal> series1, IEnumerable<decimal> series2)
        {
            if (series1.Count() != series2.Count())
            {
                throw new ArgumentException("Time series must have the same length");
            }

            var n = series1.Count();
            var sumX = series1.Sum();
            var sumY = series2.Sum();
            var sumXY = series1.Zip(series2, (x, y) => x * y).Sum();
            var sumX2 = series1.Select(x => x * x).Sum();
            var sumY2 = series2.Select(y => y * y).Sum();

            var numerator = n * sumXY - sumX * sumY;
            var denominator = (decimal)Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

            return denominator == 0 ? 0 : numerator / denominator;
        }

        /// <summary>
        /// Calculates the Sharpe Ratio of a portfolio
        /// </summary>
        /// <param name="returns">A collection of portfolio returns</param>
        /// <param name="riskFreeRate">The risk-free rate of return</param>
        /// <returns>The Sharpe Ratio</returns>
        /// <remarks>
        /// Sharpe Ratio measures the performance of an investment compared to a risk-free asset,
        /// after adjusting for its risk. A higher Sharpe Ratio indicates better risk-adjusted returns.
        /// </remarks>
        public static decimal CalculateSharpeRatio(IEnumerable<decimal> returns, decimal riskFreeRate = 0.02m)
        {
            if (!returns.Any()) return 0;

            var excessReturns = returns.Select(r => r - riskFreeRate);
            var meanExcessReturn = excessReturns.Average();
            var volatility = CalculateVolatility(excessReturns);

            return volatility == 0 ? 0 : meanExcessReturn / volatility;
        }
    }
}
