/*
 * Copyright (c) 2025 Ken Johansen. All rights reserved.
 * This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

/**
 * Chart.js configuration for portfolio visualization
 * @namespace portfolioCharts
 * @description Contains configurations for various portfolio visualization charts
 */

// Portfolio Performance Chart Configuration
/**
 * Configuration for portfolio performance line chart
 * @type {Object}
 * @property {string} type - Chart type (line)
 * @property {Object} data - Chart data configuration
 * @property {Array} data.labels - X-axis labels
 * @property {Array} data.datasets - Chart datasets
 * @property {string} data.datasets.label - Dataset label
 * @property {Array} data.datasets.data - Dataset values
 * @property {string} data.datasets.borderColor - Dataset border color
 * @property {number} data.datasets.tension - Line tension
 * @property {Object} options - Chart options
 * @property {boolean} options.responsive - Responsive chart
 * @property {Object} options.plugins - Chart plugins configuration
 */
const portfolioChartConfig = {
    type: 'line',
    data: {
        labels: [],
        datasets: [
            {
                label: 'Portfolio Value',
                data: [],
                borderColor: 'rgb(75, 192, 192)',
                tension: 0.1
            }
        ]
    },
    options: {
        responsive: true,
        plugins: {
            legend: {
                position: 'top',
            },
            title: {
                display: true,
                text: 'Portfolio Performance'
            }
        },
        scales: {
            y: {
                beginAtZero: false
            }
        }
    }
};

// Risk Metrics Chart Configuration
/**
 * Configuration for risk metrics bar chart
 * @type {Object}
 * @property {string} type - Chart type (bar)
 * @property {Object} data - Chart data configuration
 * @property {Array} data.labels - Risk metric labels
 * @property {Array} data.datasets - Chart datasets
 * @property {string} data.datasets.label - Dataset label
 * @property {Array} data.datasets.data - Dataset values
 * @property {string} data.datasets.backgroundColor - Dataset background color
 * @property {Object} options - Chart options
 */
const riskMetricsConfig = {
    type: 'bar',
    data: {
        labels: ['Volatility', 'Sharpe Ratio', 'Max Drawdown'],
        datasets: [
            {
                label: 'Metrics',
                data: [0, 0, 0],
                backgroundColor: [
                    'rgba(255, 99, 132, 0.2)',
                    'rgba(54, 162, 235, 0.2)',
                    'rgba(255, 206, 86, 0.2)',
                ],
                borderColor: [
                    'rgba(255, 99, 132, 1)',
                    'rgba(54, 162, 235, 1)',
                    'rgba(255, 206, 86, 1)',
                ],
                borderWidth: 1
            }
        ]
    },
    options: {
        responsive: true,
        plugins: {
            legend: {
                position: 'top',
            },
            title: {
                display: true,
                text: 'Risk Metrics'
            }
        }
    }
};
