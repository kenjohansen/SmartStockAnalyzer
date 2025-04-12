/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Provides core models for the Smart Portfolio Analyzer application
/// </summary>
namespace SmartPortfolioAnalyzer.Core.Models;

/// <summary>
/// Represents a portfolio of financial assets
/// </summary>
/// <remarks>
/// This class encapsulates all the information about a financial portfolio,
/// including its positions, transactions, and performance metrics.
/// </remarks>
public class Portfolio
{
    /// <summary>
    /// Gets or sets the unique identifier for the portfolio
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the portfolio
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a description of the portfolio
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the date and time when the portfolio was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the portfolio was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the initial value of the portfolio
    /// </summary>
    public decimal InitialValue { get; set; }
    
    /// <summary>
    /// Gets or sets the current value of the portfolio
    /// </summary>
    public decimal CurrentValue { get; set; }
    
    /// <summary>
    /// Gets or sets the total return percentage of the portfolio
    /// </summary>
    public decimal TotalReturn { get; set; }
    
    /// <summary>
    /// Gets or sets the annualized return percentage of the portfolio
    /// </summary>
    public decimal AnnualizedReturn { get; set; }
    
    /// <summary>
    /// Gets or sets the volatility (standard deviation) of the portfolio
    /// </summary>
    public decimal Volatility { get; set; }
    
    /// <summary>
    /// Gets or sets the Sharpe ratio of the portfolio
    /// </summary>
    public decimal SharpeRatio { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum drawdown percentage of the portfolio
    /// </summary>
    public decimal MaxDrawdown { get; set; }
    
    /// <summary>
    /// Gets or sets a flag indicating whether the portfolio is active
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Gets or sets the list of positions in the portfolio
    /// </summary>
    public List<PortfolioPosition> Positions { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of transactions for the portfolio
    /// </summary>
    public List<PortfolioTransaction> Transactions { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of risk metrics for the portfolio
    /// </summary>
    public List<PortfolioRiskMetric> RiskMetrics { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the performance history of the portfolio
    /// </summary>
    public List<PortfolioPerformance> PerformanceHistory { get; set; } = new();
}
