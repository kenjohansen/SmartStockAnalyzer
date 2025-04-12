/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartPortfolioAnalyzer.Web.Services;

/// <summary>
/// Provides services for real-time portfolio updates using SignalR
/// </summary>
public interface ISignalRService
{
    /// <summary>
    /// Connects to the SignalR hub
    /// </summary>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task ConnectAsync();

    /// <summary>
    /// Disconnects from the SignalR hub
    /// </summary>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task DisconnectAsync();

    /// <summary>
    /// Event raised when a portfolio update is received
    /// </summary>
    event EventHandler<string> OnPortfolioUpdate;

    /// <summary>
    /// Event raised when a risk metrics update is received
    /// </summary>
    event EventHandler<string> OnRiskMetricsUpdate;

    /// <summary>
    /// Event raised when a performance update is received
    /// </summary>
    event EventHandler<string> OnPerformanceUpdate;

    /// <summary>
    /// Event raised when an alert is received
    /// </summary>
    event EventHandler<string> OnAlert;
}

/// <summary>
/// Service for handling real-time portfolio updates via SignalR
/// </summary>
/// <remarks>
/// This service manages the connection to the SignalR hub and handles
/// real-time updates for portfolio data, risk metrics, and performance.
/// </remarks>
public class SignalRService : ISignalRService
{
    private HubConnection _connection;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for the SignalR connection</param>
    public SignalRService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Event raised when a portfolio update is received
    /// </summary>
    public event EventHandler<string> OnPortfolioUpdate;

    /// <summary>
    /// Event raised when a risk metrics update is received
    /// </summary>
    public event EventHandler<string> OnRiskMetricsUpdate;

    /// <summary>
    /// Event raised when a performance update is received
    /// </summary>
    public event EventHandler<string> OnPerformanceUpdate;

    /// <summary>
    /// Event raised when an alert is received
    /// </summary>
    public event EventHandler<string> OnAlert;

    /// <summary>
    /// Connects to the SignalR hub
    /// </summary>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task ConnectAsync()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_httpClient.BaseAddress + "signalr/portfoliohub")
            .Build();

        _connection.On<string, object>("SendPortfolioUpdate", (portfolioId, update) =>
            OnPortfolioUpdate?.Invoke(this, $"{portfolioId}:{update}")
        );

        _connection.On<string, object>("SendRiskMetricsUpdate", (portfolioId, metrics) =>
            OnRiskMetricsUpdate?.Invoke(this, $"{portfolioId}:{metrics}")
        );

        _connection.On<string, object>("SendPerformanceUpdate", (portfolioId, performance) =>
            OnPerformanceUpdate?.Invoke(this, $"{portfolioId}:{performance}")
        );

        _connection.On<string, string>("SendAlert", (portfolioId, message) =>
            OnAlert?.Invoke(this, $"{portfolioId}:{message}")
        );

        await _connection.StartAsync();
    }

    /// <summary>
    /// Disconnects from the SignalR hub
    /// </summary>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task DisconnectAsync()
    {
        if (_connection != null && _connection.State == HubConnectionState.Connected)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}
