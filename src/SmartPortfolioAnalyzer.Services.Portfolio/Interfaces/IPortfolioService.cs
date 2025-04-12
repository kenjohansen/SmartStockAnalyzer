/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartPortfolioAnalyzer.Core.Models;

/// <summary>
/// Provides interfaces for portfolio management services
/// </summary>
namespace SmartPortfolioAnalyzer.Services.Portfolio.Interfaces;

/// <summary>
/// Interface defining the contract for portfolio management operations
/// </summary>
/// <remarks>
/// This interface defines the core methods for managing portfolios,
/// including creation, updates, and analysis of portfolio data.
/// </remarks>
public interface IPortfolioService
{
    // Portfolio Management
    /// <summary>
    /// Creates a new portfolio
    /// </summary>
    /// <param name="portfolio">The portfolio to create</param>
    /// <returns>The created portfolio</returns>
    Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio);

    /// <summary>
    /// Updates an existing portfolio
    /// </summary>
    /// <param name="portfolio">The portfolio to update</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task UpdatePortfolioAsync(Portfolio portfolio);

    /// <summary>
    /// Deletes a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio to delete</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task DeletePortfolioAsync(Guid portfolioId);

    /// <summary>
    /// Gets a portfolio by ID
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio to retrieve</param>
    /// <returns>The portfolio if found, otherwise null</returns>
    Task<Portfolio> GetPortfolioAsync(Guid portfolioId);

    /// <summary>
    /// Gets all portfolios
    /// </summary>
    /// <returns>A collection of all portfolios</returns>
    Task<IEnumerable<Portfolio>> GetPortfoliosAsync();

    /// <summary>
    /// Gets all active portfolios
    /// </summary>
    /// <returns>A collection of active portfolios</returns>
    Task<IEnumerable<Portfolio>> GetActivePortfoliosAsync();

    // Position Management
    /// <summary>
    /// Adds a new position to a portfolio
    /// </summary>
    /// <param name="position">The position to add</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task AddPositionAsync(PortfolioPosition position);

    /// <summary>
    /// Updates an existing position
    /// </summary>
    /// <param name="position">The position to update</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task UpdatePositionAsync(PortfolioPosition position);

    /// <summary>
    /// Deletes a position from a portfolio
    /// </summary>
    /// <param name="positionId">The ID of the position to delete</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task DeletePositionAsync(Guid positionId);

    /// <summary>
    /// Gets all positions in a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>A collection of positions in the portfolio</returns>
    Task<IEnumerable<PortfolioPosition>> GetPositionsAsync(Guid portfolioId);

    /// <summary>
    /// Gets a position by ID
    /// </summary>
    /// <param name="positionId">The ID of the position to retrieve</param>
    /// <returns>The position if found, otherwise null</returns>
    Task<PortfolioPosition> GetPositionAsync(Guid positionId);

    // Transaction Management
    /// <summary>
    /// Adds a new transaction to a portfolio
    /// </summary>
    /// <param name="transaction">The transaction to add</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task AddTransactionAsync(PortfolioTransaction transaction);

    /// <summary>
    /// Gets all transactions for a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>A collection of transactions, ordered by date</returns>
    Task<IEnumerable<PortfolioTransaction>> GetTransactionsAsync(Guid portfolioId);

    /// <summary>
    /// Gets transactions for a portfolio within a date range
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="startDate">The start date of the range</param>
    /// <param name="endDate">The end date of the range</param>
    /// <returns>A collection of transactions within the specified range</returns>
    Task<IEnumerable<PortfolioTransaction>> GetTransactionsAsync(Guid portfolioId, DateTime startDate, DateTime endDate);

    // Portfolio Analysis
    /// <summary>
    /// Calculates the total value of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The total value of the portfolio</returns>
    Task<decimal> CalculateTotalValueAsync(Guid portfolioId);

    /// <summary>
    /// Calculates the total return percentage of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The total return percentage</returns>
    Task<decimal> CalculateTotalReturnAsync(Guid portfolioId);

    /// <summary>
    /// Calculates the annualized return of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The annualized return percentage</returns>
    Task<decimal> CalculateAnnualizedReturnAsync(Guid portfolioId);

    /// <summary>
    /// Calculates the volatility of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The volatility (standard deviation) of the portfolio</returns>
    Task<decimal> CalculateVolatilityAsync(Guid portfolioId);

    /// <summary>
    /// Calculates the Sharpe ratio of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The Sharpe ratio of the portfolio</returns>
    Task<decimal> CalculateSharpeRatioAsync(Guid portfolioId);

    /// <summary>
    /// Calculates the maximum drawdown of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The maximum drawdown percentage of the portfolio</returns>
    Task<decimal> CalculateMaxDrawdownAsync(Guid portfolioId);

    /// <summary>
    /// Calculates all risk metrics for a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>A collection of risk metrics</returns>
    Task<IEnumerable<PortfolioRiskMetric>> CalculateRiskMetricsAsync(Guid portfolioId);

    /// <summary>
    /// Calculates the performance history of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>A collection of performance history data</returns>
    Task<IEnumerable<PortfolioPerformance>> CalculatePerformanceHistoryAsync(Guid portfolioId);

    // Rebalancing
    /// <summary>
    /// Rebalances a portfolio to match target weights
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio to rebalance</param>
    /// <param name="targetWeights">The target weights for portfolio positions</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task RebalancePortfolioAsync(Guid portfolioId, Dictionary<string, decimal> targetWeights);

    /// <summary>
    /// Generates rebalancing transactions to match target weights
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="targetWeights">The target weights for portfolio positions</param>
    /// <returns>A collection of rebalancing transactions</returns>
    Task<IEnumerable<PortfolioTransaction>> GenerateRebalanceTransactionsAsync(Guid portfolioId, Dictionary<string, decimal> targetWeights);
}
