/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartPortfolioAnalyzer.Core.Models;
using SmartPortfolioAnalyzer.Services.Portfolio.Interfaces;
using SmartPortfolioAnalyzer.Infrastructure.Data;
using SmartPortfolioAnalyzer.Services.Portfolio.Services;

/// <summary>
/// Provides services for portfolio management in the Smart Portfolio Analyzer
/// </summary>
namespace SmartPortfolioAnalyzer.Services.Portfolio.Services;

/// <summary>
/// Service implementation for managing portfolios
/// </summary>
/// <remarks>
/// This service provides the core business logic for portfolio management,
/// including creation, updates, analysis, risk assessment, performance tracking,
/// and rebalancing of portfolios.
/// </remarks>
public class PortfolioService : IPortfolioService
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PortfolioService"/> class.
    /// </summary>
    /// <param name="context">The application database context</param>
    public PortfolioService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Portfolio Management
    /// <summary>
    /// Creates a new portfolio
    /// </summary>
    /// <param name="portfolio">The portfolio to create</param>
    /// <returns>The created portfolio</returns>
    public async Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio)
    {
        portfolio.Id = Guid.NewGuid();
        portfolio.CreatedAt = DateTime.UtcNow;
        portfolio.UpdatedAt = DateTime.UtcNow;
        
        await _context.Portfolios.AddAsync(portfolio);
        await _context.SaveChangesAsync();
        
        return portfolio;
    }

    /// <summary>
    /// Updates an existing portfolio
    /// </summary>
    /// <param name="portfolio">The portfolio to update</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task UpdatePortfolioAsync(Portfolio portfolio)
    {
        portfolio.UpdatedAt = DateTime.UtcNow;
        _context.Portfolios.Update(portfolio);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio to delete</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task DeletePortfolioAsync(Guid portfolioId)
    {
        var portfolio = await _context.Portfolios
            .Include(p => p.Positions)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == portfolioId);

        if (portfolio != null)
        {
            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets a portfolio by ID
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio to retrieve</param>
    /// <returns>The portfolio if found, otherwise null</returns>
    public async Task<Portfolio> GetPortfolioAsync(Guid portfolioId)
    {
        return await _context.Portfolios
            .Include(p => p.Positions)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == portfolioId);
    }

    /// <summary>
    /// Gets all portfolios
    /// </summary>
    /// <returns>A collection of all portfolios</returns>
    public async Task<IEnumerable<Portfolio>> GetPortfoliosAsync()
    {
        return await _context.Portfolios
            .Include(p => p.Positions)
            .Include(p => p.Transactions)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all active portfolios
    /// </summary>
    /// <returns>A collection of active portfolios</returns>
    public async Task<IEnumerable<Portfolio>> GetActivePortfoliosAsync()
    {
        return await _context.Portfolios
            .Where(p => p.IsActive)
            .Include(p => p.Positions)
            .Include(p => p.Transactions)
            .ToListAsync();
    }

    // Position Management
    /// <summary>
    /// Adds a new position to a portfolio
    /// </summary>
    /// <param name="position">The position to add</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task AddPositionAsync(PortfolioPosition position)
    {
        position.Id = Guid.NewGuid();
        position.LastUpdated = DateTime.UtcNow;
        
        await _context.PortfolioPositions.AddAsync(position);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an existing position
    /// </summary>
    /// <param name="position">The position to update</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task UpdatePositionAsync(PortfolioPosition position)
    {
        position.LastUpdated = DateTime.UtcNow;
        _context.PortfolioPositions.Update(position);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a position from a portfolio
    /// </summary>
    /// <param name="positionId">The ID of the position to delete</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task DeletePositionAsync(Guid positionId)
    {
        var position = await _context.PortfolioPositions.FindAsync(positionId);
        if (position != null)
        {
            _context.PortfolioPositions.Remove(position);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets all positions in a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>A collection of positions in the portfolio</returns>
    public async Task<IEnumerable<PortfolioPosition>> GetPositionsAsync(Guid portfolioId)
    {
        return await _context.PortfolioPositions
            .Where(p => p.PortfolioId == portfolioId)
            .Include(p => p.Transactions)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a position by ID
    /// </summary>
    /// <param name="positionId">The ID of the position to retrieve</param>
    /// <returns>The position if found, otherwise null</returns>
    public async Task<PortfolioPosition> GetPositionAsync(Guid positionId)
    {
        return await _context.PortfolioPositions
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == positionId);
    }

    // Transaction Management
    /// <summary>
    /// Adds a new transaction to a portfolio
    /// </summary>
    /// <param name="transaction">The transaction to add</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task AddTransactionAsync(PortfolioTransaction transaction)
    {
        transaction.Id = Guid.NewGuid();
        transaction.CreatedAt = DateTime.UtcNow;
        transaction.UpdatedAt = DateTime.UtcNow;
        
        await _context.PortfolioTransactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets all transactions for a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>A collection of transactions, ordered by date</returns>
    public async Task<IEnumerable<PortfolioTransaction>> GetTransactionsAsync(Guid portfolioId)
    {
        return await _context.PortfolioTransactions
            .Where(t => t.PortfolioId == portfolioId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets transactions for a portfolio within a date range
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="startDate">The start date of the range</param>
    /// <param name="endDate">The end date of the range</param>
    /// <returns>A collection of transactions within the specified range</returns>
    public async Task<IEnumerable<PortfolioTransaction>> GetTransactionsAsync(Guid portfolioId, DateTime startDate, DateTime endDate)
    {
        return await _context.PortfolioTransactions
            .Where(t => t.PortfolioId == portfolioId && 
                        t.TransactionDate >= startDate && 
                        t.TransactionDate <= endDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    // Risk Assessment
    /// <summary>
    /// Calculates the portfolio volatility
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The portfolio volatility as a percentage</returns>
    public async Task<decimal> CalculateVolatilityAsync(Guid portfolioId)
    {
        var transactions = await GetTransactionsAsync(portfolioId);
        var returns = CalculateDailyReturns(transactions);
        return RiskCalculations.CalculateVolatility(returns);
    }

    /// <summary>
    /// Calculates daily returns from portfolio transactions
    /// </summary>
    /// <param name="transactions">The portfolio transactions</param>
    /// <returns>A collection of daily returns</returns>
    private IEnumerable<decimal> CalculateDailyReturns(IEnumerable<PortfolioTransaction> transactions)
    {
        var dailyValues = new Dictionary<DateTime, decimal>();
        var currentValue = 0m;

        foreach (var transaction in transactions.OrderBy(t => t.TransactionDate))
        {
            currentValue += transaction.Amount;
            dailyValues[transaction.TransactionDate.Date] = currentValue;
        }

        var dailyReturns = new List<decimal>();
        decimal previousValue = dailyValues.First().Value;

        foreach (var value in dailyValues.Values.Skip(1))
        {
            var returnRate = (value - previousValue) / previousValue;
            dailyReturns.Add(returnRate);
            previousValue = value;
        }

        return dailyReturns;
    }

    /// <summary>
    /// Calculates the maximum drawdown of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The maximum drawdown percentage</returns>
    public async Task<decimal> CalculateMaxDrawdownAsync(Guid portfolioId)
    {
        var transactions = await GetTransactionsAsync(portfolioId);
        var dailyValues = CalculateDailyValues(transactions);
        return RiskCalculations.CalculateMaxDrawdown(dailyValues);
    }

    /// <summary>
    /// Calculates daily portfolio values from transactions
    /// </summary>
    /// <param name="transactions">The portfolio transactions</param>
    /// <returns>A collection of daily portfolio values</returns>
    private IEnumerable<decimal> CalculateDailyValues(IEnumerable<PortfolioTransaction> transactions)
    {
        var dailyValues = new List<decimal>();
        var currentValue = 0m;

        foreach (var transaction in transactions.OrderBy(t => t.TransactionDate))
        {
            currentValue += transaction.Amount;
            dailyValues.Add(currentValue);
        }

        return dailyValues;
    }

    /// <summary>
    /// Calculates the Sharpe Ratio of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="riskFreeRate">The risk-free rate of return</param>
    /// <returns>The portfolio's Sharpe Ratio</returns>
    public async Task<decimal> CalculateSharpeRatioAsync(Guid portfolioId, decimal riskFreeRate = 0.02m)
    {
        var transactions = await GetTransactionsAsync(portfolioId);
        var returns = CalculateDailyReturns(transactions);
        return RiskCalculations.CalculateSharpeRatio(returns, riskFreeRate);
    }

    /// <summary>
    /// Calculates all risk metrics for a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="riskFreeRate">The risk-free rate of return</param>
    /// <returns>A collection of risk metrics</returns>
    public async Task<IEnumerable<PortfolioRiskMetric>> CalculateRiskMetricsAsync(Guid portfolioId, decimal riskFreeRate = 0.02m)
    {
        var metrics = new List<PortfolioRiskMetric>
        {
            new PortfolioRiskMetric
            {
                Name = "Volatility",
                Value = await CalculateVolatilityAsync(portfolioId)
            },
            new PortfolioRiskMetric
            {
                Name = "Sharpe Ratio",
                Value = await CalculateSharpeRatioAsync(portfolioId, riskFreeRate)
            },
            new PortfolioRiskMetric
            {
                Name = "Max Drawdown",
                Value = await CalculateMaxDrawdownAsync(portfolioId)
            }
        };

        return metrics;
    }

    // Performance Tracking
    /// <summary>
    /// Calculates the simple return of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The simple return percentage</returns>
    public async Task<decimal> CalculateSimpleReturnAsync(Guid portfolioId)
    {
        var portfolio = await GetPortfolioAsync(portfolioId);
        if (portfolio == null) return 0;

        var currentValue = await CalculateCurrentValueAsync(portfolioId);
        return PerformanceCalculations.CalculateSimpleReturn(portfolio.InitialValue, currentValue);
    }

    /// <summary>
    /// Calculates the annualized return of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The annualized return percentage</returns>
    public async Task<decimal> CalculateAnnualizedReturnAsync(Guid portfolioId)
    {
        var portfolio = await GetPortfolioAsync(portfolioId);
        if (portfolio == null) return 0;

        var currentValue = await CalculateCurrentValueAsync(portfolioId);
        return PerformanceCalculations.CalculateAnnualizedReturn(
            portfolio.InitialValue,
            currentValue,
            portfolio.CreatedAt,
            DateTime.UtcNow
        );
    }

    /// <summary>
    /// Calculates the rolling returns of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="window">The rolling window size in days</param>
    /// <returns>A collection of rolling returns</returns>
    public async Task<IEnumerable<decimal>> CalculateRollingReturnsAsync(Guid portfolioId, int window = 30)
    {
        var transactions = await GetTransactionsAsync(portfolioId);
        var values = CalculateDailyValues(transactions);
        return PerformanceCalculations.CalculateRollingReturns(values, window);
    }

    /// <summary>
    /// Calculates the performance attribution by asset class
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>A dictionary of asset class returns</returns>
    public async Task<Dictionary<string, decimal>> CalculatePerformanceAttributionAsync(Guid portfolioId)
    {
        var portfolio = await GetPortfolioAsync(portfolioId);
        if (portfolio == null) return new Dictionary<string, decimal>();

        var currentValue = await CalculateCurrentValueAsync(portfolioId);
        return PerformanceCalculations.CalculatePerformanceAttribution(currentValue, portfolio.Positions);
    }

    /// <summary>
    /// Calculates the performance history of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>A collection of performance history data</returns>
    public async Task<IEnumerable<PortfolioPerformance>> CalculatePerformanceHistoryAsync(Guid portfolioId)
    {
        var portfolio = await GetPortfolioAsync(portfolioId);
        if (portfolio == null) return Enumerable.Empty<PortfolioPerformance>();

        var transactions = await GetTransactionsAsync(portfolioId);
        var values = CalculateDailyValues(transactions);
        var dates = transactions.Select(t => t.TransactionDate).Distinct().OrderBy(d => d);
        return PerformanceCalculations.CalculatePerformanceHistory(values, dates);
    }

    /// <summary>
    /// Calculates the current value of a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <returns>The current value of the portfolio</returns>
    private async Task<decimal> CalculateCurrentValueAsync(Guid portfolioId)
    {
        var portfolio = await GetPortfolioAsync(portfolioId);
        if (portfolio == null) return 0;

        var currentValue = portfolio.InitialValue;
        foreach (var transaction in portfolio.Transactions)
        {
            currentValue += transaction.Amount;
        }

        return currentValue;
    }

    // Rebalancing
    /// <summary>
    /// Rebalances a portfolio to match target weights
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio to rebalance</param>
    /// <param name="targetAllocation">The target asset allocation</param>
    /// <param name="feeRate">The transaction fee rate</param>
    /// <param name="minTransactionSize">Minimum transaction size</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task RebalancePortfolioAsync(
        Guid portfolioId,
        Dictionary<string, decimal> targetAllocation,
        decimal feeRate = 0.001m,
        decimal minTransactionSize = 100m)
    {
        var portfolio = await GetPortfolioAsync(portfolioId);
        if (portfolio == null) return;

        var positions = await GetPositionsAsync(portfolioId);
        var currentValue = await CalculateCurrentValueAsync(portfolioId);

        var targetWeights = RebalancingCalculations.CalculateTargetWeights(positions, targetAllocation);
        var transactions = RebalancingCalculations.GenerateRebalancingTransactions(positions, targetWeights, currentValue);
        var optimizedTransactions = RebalancingCalculations.OptimizeRebalancingTransactions(transactions, feeRate, minTransactionSize);

        foreach (var transaction in optimizedTransactions)
        {
            await AddTransactionAsync(transaction);
        }
    }

    /// <summary>
    /// Calculates the rebalancing cost for a portfolio
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="targetAllocation">The target asset allocation</param>
    /// <param name="feeRate">The transaction fee rate</param>
    /// <returns>The total rebalancing cost</returns>
    public async Task<decimal> CalculateRebalancingCostAsync(
        Guid portfolioId,
        Dictionary<string, decimal> targetAllocation,
        decimal feeRate = 0.001m)
    {
        var portfolio = await GetPortfolioAsync(portfolioId);
        if (portfolio == null) return 0;

        var positions = await GetPositionsAsync(portfolioId);
        var currentValue = await CalculateCurrentValueAsync(portfolioId);
        var targetWeights = RebalancingCalculations.CalculateTargetWeights(positions, targetAllocation);
        var transactions = RebalancingCalculations.GenerateRebalancingTransactions(positions, targetWeights, currentValue);

        return RebalancingCalculations.CalculateRebalancingCost(transactions, feeRate);
    }

    /// <summary>
    /// Calculates weight deviations from target allocation
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="targetAllocation">The target asset allocation</param>
    /// <returns>A dictionary of weight deviations for each position</returns>
    public async Task<Dictionary<string, decimal>> CalculateWeightDeviationsAsync(
        Guid portfolioId,
        Dictionary<string, decimal> targetAllocation)
    {
        var portfolio = await GetPortfolioAsync(portfolioId);
        if (portfolio == null) return new Dictionary<string, decimal>();

        var positions = await GetPositionsAsync(portfolioId);
        return RebalancingCalculations.CalculateWeightDeviations(positions, targetAllocation);
    }

    /// <summary>
    /// Generates rebalancing transactions without executing them
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="targetAllocation">The target asset allocation</param>
    /// <param name="feeRate">The transaction fee rate</param>
    /// <param name="minTransactionSize">Minimum transaction size</param>
    /// <returns>A collection of rebalancing transactions</returns>
    public async Task<IEnumerable<PortfolioTransaction>> GenerateRebalancingTransactionsAsync(
        Guid portfolioId,
        Dictionary<string, decimal> targetAllocation,
        decimal feeRate = 0.001m,
        decimal minTransactionSize = 100m)
    {
        var portfolio = await GetPortfolioAsync(portfolioId);
        if (portfolio == null) return Enumerable.Empty<PortfolioTransaction>();

        var positions = await GetPositionsAsync(portfolioId);
        var currentValue = await CalculateCurrentValueAsync(portfolioId);
        var targetWeights = RebalancingCalculations.CalculateTargetWeights(positions, targetAllocation);
        var transactions = RebalancingCalculations.GenerateRebalancingTransactions(positions, targetWeights, currentValue);

        return RebalancingCalculations.OptimizeRebalancingTransactions(transactions, feeRate, minTransactionSize);
    }
}
