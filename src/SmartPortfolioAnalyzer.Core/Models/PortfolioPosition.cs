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
/// Represents a position within a portfolio
/// </summary>
/// <remarks>
/// This class encapsulates information about a specific financial instrument
/// held within a portfolio, including quantity, cost basis, and market value.
/// </remarks>
public class PortfolioPosition
{
    /// <summary>
    /// Gets or sets the unique identifier for the position
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the portfolio containing this position
    /// </summary>
    public Guid PortfolioId { get; set; }
    
    /// <summary>
    /// Gets or sets the symbol of the financial instrument
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the name of the financial instrument
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the quantity of the instrument held
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Gets or sets the average cost per unit of the instrument
    /// </summary>
    public decimal AverageCost { get; set; }
    
    /// <summary>
    /// Gets or sets the current market price of the instrument
    /// </summary>
    public decimal CurrentPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the total market value of the position
    /// </summary>
    public decimal MarketValue { get; set; }
    
    /// <summary>
    /// Gets or sets the unrealized profit/loss for the position
    /// </summary>
    public decimal UnrealizedPnl { get; set; }
    
    /// <summary>
    /// Gets or sets the weight of this position in the portfolio
    /// </summary>
    public decimal Weight { get; set; }
    
    /// <summary>
    /// Gets or sets the asset class of the instrument
    /// </summary>
    public string AssetClass { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the sector of the instrument
    /// </summary>
    public string Sector { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the last update timestamp for this position
    /// </summary>
    public DateTime LastUpdated { get; set; }
    
    /// <summary>
    /// Gets or sets the transactions associated with this position
    /// </summary>
    public List<PortfolioTransaction> Transactions { get; set; } = new();
}
