/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace SmartPortfolioAnalyzer.Web.Services;

/// <summary>
/// Provides services for portfolio data visualization
/// </summary>
public interface IChartService
{
    /// <summary>
    /// Initializes a chart for a portfolio
    /// </summary>
    /// <param name="chartId">The ID of the chart</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task InitializeChart(string chartId);

    /// <summary>
    /// Updates a chart for a portfolio
    /// </summary>
    /// <param name="chartId">The ID of the chart</param>
    /// <param name="data">The data to update the chart with</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task UpdateChart(string chartId, string data);
}

/// <summary>
/// Service for managing portfolio charts and visualizations
/// </summary>
/// <remarks>
/// This service handles the creation and management of various portfolio charts,
/// including performance charts, risk metrics, and position distributions.
/// </remarks>
public class ChartService : IChartService
{
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime service</param>
    public ChartService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Initializes a chart for a portfolio
    /// </summary>
    /// <param name="chartId">The ID of the chart</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task InitializeChart(string chartId)
    {
        await _jsRuntime.InvokeVoidAsync(
            "portfolioChart.initialize",
            chartId
        );
    }

    /// <summary>
    /// Updates a chart for a portfolio
    /// </summary>
    /// <param name="chartId">The ID of the chart</param>
    /// <param name="data">The data to update the chart with</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task UpdateChart(string chartId, string data)
    {
        await _jsRuntime.InvokeVoidAsync(
            "portfolioChart.update",
            chartId,
            data
        );
    }
}
