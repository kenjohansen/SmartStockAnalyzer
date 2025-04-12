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
/// Provides performance calculation methods for portfolio analysis
/// </summary>
/// <remarks>
/// This class contains statistical calculations for portfolio performance metrics,
/// including returns, performance history, and attribution analysis.
/// </remarks>
namespace SmartPortfolioAnalyzer.Services.Portfolio.Services
{
    /// <summary>
    /// Provides statistical calculations for portfolio performance metrics
    /// </summary>
    public static class PerformanceCalculations
    {
        /// <summary>
        /// Calculates the simple return of a portfolio
        /// </summary>
        /// <param name="initialValue">The initial value of the portfolio</param>
        /// <param name="finalValue">The final value of the portfolio</param>
        /// <returns>The simple return percentage</returns>
        /// <remarks>
        /// Simple return is calculated as (final value - initial value) / initial value
        /// </remarks>
        public static decimal CalculateSimpleReturn(decimal initialValue, decimal finalValue)
        {
            if (initialValue == 0) return 0;
            return (finalValue - initialValue) / initialValue;
        }

        /// <summary>
        /// Calculates the annualized return of a portfolio
        /// </summary>
        /// <param name="initialValue">The initial value of the portfolio</param>
        /// <param name="finalValue">The final value of the portfolio</param>
        /// <param name="startDate">The start date of the period</param>
        /// <param name="endDate">The end date of the period</param>
        /// <returns>The annualized return percentage</returns>
        /// <remarks>
        /// Annualized return is calculated using the formula:
        /// (final value / initial value)^(1/years) - 1
        /// where years = (endDate - startDate).TotalDays / 365
        /// </remarks>
        public static decimal CalculateAnnualizedReturn(decimal initialValue, decimal finalValue, DateTime startDate, DateTime endDate)
        {
            if (initialValue == 0) return 0;
            var years = (endDate - startDate).TotalDays / 365;
            return Math.Pow(finalValue / initialValue, 1 / years) - 1;
        }

        /// <summary>
        /// Calculates the rolling returns of a portfolio
        /// </summary>
        /// <param name="values">A collection of portfolio values over time</param>
        /// <param name="window">The rolling window size in days</param>
        /// <returns>A collection of rolling returns</returns>
        /// <remarks>
        /// Rolling returns are calculated using a sliding window of the specified size
        /// </remarks>
        public static IEnumerable<decimal> CalculateRollingReturns(IEnumerable<decimal> values, int window = 30)
        {
            var valueList = values.ToList();
            if (valueList.Count < window) yield break;

            for (int i = window; i < valueList.Count; i++)
            {
                var startValue = valueList[i - window];
                var endValue = valueList[i];
                yield return CalculateSimpleReturn(startValue, endValue);
            }
        }

        /// <summary>
        /// Calculates the performance attribution by asset class
        /// </summary>
        /// <param name="portfolioValue">The total portfolio value</param>
        /// <param name="positions">The portfolio positions</param>
        /// <returns>A dictionary of asset class returns</returns>
        /// <remarks>
        /// Attribution is calculated based on each position's contribution to total returns
        /// </remarks>
        public static Dictionary<string, decimal> CalculatePerformanceAttribution(decimal portfolioValue, IEnumerable<PortfolioPosition> positions)
        {
            var attribution = new Dictionary<string, decimal>();
            
            foreach (var position in positions)
            {
                var positionReturn = (position.MarketValue - position.AverageCost * position.Quantity) / (position.AverageCost * position.Quantity);
                var weightedReturn = positionReturn * (position.MarketValue / portfolioValue);
                
                // Group by asset class (using symbol as proxy here)
                if (attribution.ContainsKey(position.Symbol))
                {
                    attribution[position.Symbol] += weightedReturn;
                }
                else
                {
                    attribution[position.Symbol] = weightedReturn;
                }
            }

            return attribution;
        }

        /// <summary>
        /// Calculates the performance history of a portfolio
        /// </summary>
        /// <param name="values">A collection of portfolio values over time</param>
        /// <param name="dates">Corresponding dates for each value</param>
        /// <returns>A collection of performance history points</returns>
        /// <remarks>
        /// Performance history includes daily returns, cumulative returns, and drawdowns
        /// </remarks>
        public static IEnumerable<PortfolioPerformance> CalculatePerformanceHistory(IEnumerable<decimal> values, IEnumerable<DateTime> dates)
        {
            var valueList = values.ToList();
            var dateList = dates.ToList();
            var performanceHistory = new List<PortfolioPerformance>();

            if (!valueList.Any()) return performanceHistory;

            var initialValue = valueList.First();
            var peakValue = initialValue;
            var peakDate = dateList.First();

            for (int i = 1; i < valueList.Count; i++)
            {
                var currentValue = valueList[i];
                var currentDate = dateList[i];
                var previousValue = valueList[i - 1];

                var dailyReturn = CalculateSimpleReturn(previousValue, currentValue);
                var cumulativeReturn = CalculateSimpleReturn(initialValue, currentValue);
                
                if (currentValue > peakValue)
                {
                    peakValue = currentValue;
                    peakDate = currentDate;
                }

                var drawdown = (peakValue - currentValue) / peakValue;

                performanceHistory.Add(new PortfolioPerformance
                {
                    Date = currentDate,
                    DailyReturn = dailyReturn,
                    CumulativeReturn = cumulativeReturn,
                    Drawdown = drawdown,
                    PeakDate = peakDate
                });
            }

            return performanceHistory;
        }
    }
}
