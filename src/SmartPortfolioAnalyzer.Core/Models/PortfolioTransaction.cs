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
/// Enum representing different types of portfolio transactions
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// A purchase transaction
    /// </summary>
    Buy = 1,

    /// <summary>
    /// A sale transaction
    /// </summary>
    Sell = 2,

    /// <summary>
    /// A dividend payment transaction
    /// </summary>
    Dividend = 3,

    /// <summary>
    /// A stock split transaction
    /// </summary>
    Split = 4,

    /// <summary>
    /// A fee transaction
    /// </summary>
    Fee = 5
}

/// <summary>
/// Represents a transaction in a portfolio
/// </summary>
/// <remarks>
/// This class tracks all financial transactions that affect a portfolio,
/// including buys, sells, dividends, splits, and fees.
/// </remarks>
public class PortfolioTransaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the transaction
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the portfolio associated with this transaction
    /// </summary>
    public Guid PortfolioId { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the position associated with this transaction
    /// </summary>
    public Guid? PositionId { get; set; }
    
    /// <summary>
    /// Gets or sets the type of transaction
    /// </summary>
    public TransactionType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the symbol of the financial instrument
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the quantity involved in the transaction
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Gets or sets the price per unit at the time of transaction
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Gets or sets the total amount of the transaction
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Gets or sets any fees associated with the transaction
    /// </summary>
    public decimal Fee { get; set; }
    
    /// <summary>
    /// Gets or sets any notes or comments about the transaction
    /// </summary>
    public string Notes { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the date and time when the transaction occurred
    /// </summary>
    public DateTime TransactionDate { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the transaction was created in the system
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the transaction was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
