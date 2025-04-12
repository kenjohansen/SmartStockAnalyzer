/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using System.Threading.Tasks;

/// <summary>
/// Provides interfaces for SignalR communication in the Smart Portfolio Analyzer
/// </summary>
namespace SmartPortfolioAnalyzer.Core.Services;

/// <summary>
/// Interface defining the SignalR hub methods for real-time portfolio updates
/// </summary>
/// <remarks>
/// This interface defines the contract for real-time communication between
/// the client and server components of the portfolio management system.
/// </remarks>
public interface ISignalRHub
{
    /// <summary>
    /// Sends a portfolio update notification to connected clients
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio being updated</param>
    /// <param name="update">The update data to send to clients</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task SendPortfolioUpdate(string portfolioId, object update);

    /// <summary>
    /// Sends updated risk metrics to connected clients
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio with updated metrics</param>
    /// <param name="metrics">The new risk metrics data</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task SendRiskMetricsUpdate(string portfolioId, object metrics);

    /// <summary>
    /// Sends updated performance data to connected clients
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio with updated performance</param>
    /// <param name="performance">The new performance data</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task SendPerformanceUpdate(string portfolioId, object performance);

    /// <summary>
    /// Sends an alert notification to connected clients
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio related to the alert</param>
    /// <param name="message">The alert message to send</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task SendAlert(string portfolioId, string message);
}
