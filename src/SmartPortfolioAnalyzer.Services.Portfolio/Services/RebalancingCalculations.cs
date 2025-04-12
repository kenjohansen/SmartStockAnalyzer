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
/// Provides rebalancing calculation methods for portfolio management
/// </summary>
/// <remarks>
/// This class contains algorithms for portfolio rebalancing,
/// including target weight calculations, transaction generation,
/// and optimization strategies.
/// </remarks>
namespace SmartPortfolioAnalyzer.Services.Portfolio.Services
{
    /// <summary>
    /// Provides rebalancing calculations for portfolio management
    /// </summary>
    public static class RebalancingCalculations
    {
        /// <summary>
        /// Calculates the target weights for portfolio positions
        /// </summary>
        /// <param name="positions">The current portfolio positions</param>
        /// <param name="targetAllocation">The target asset allocation</param>
        /// <returns>A dictionary of target weights for each position</returns>
        /// <remarks>
        /// Target weights are calculated based on the target allocation
        /// and the current portfolio composition.
        /// </remarks>
        public static Dictionary<string, decimal> CalculateTargetWeights(
            IEnumerable<PortfolioPosition> positions,
            Dictionary<string, decimal> targetAllocation)
        {
            var totalValue = positions.Sum(p => p.MarketValue);
            var targetWeights = new Dictionary<string, decimal>();

            foreach (var position in positions)
            {
                if (targetAllocation.TryGetValue(position.Symbol, out var targetWeight))
                {
                    targetWeights[position.Symbol] = targetWeight;
                }
                else
                {
                    // If position is not in target allocation, set weight to 0
                    targetWeights[position.Symbol] = 0;
                }
            }

            return targetWeights;
        }

        /// <summary>
        /// Generates rebalancing transactions to match target weights
        /// </summary>
        /// <param name="positions">The current portfolio positions</param>
        /// <param name="targetWeights">The target weights for each position</param>
        /// <param name="portfolioValue">The total portfolio value</param>
        /// <returns>A collection of rebalancing transactions</returns>
        /// <remarks>
        /// Transactions are generated to move each position's weight
        /// closer to its target weight, while minimizing transaction costs.
        /// </remarks>
        public static IEnumerable<PortfolioTransaction> GenerateRebalancingTransactions(
            IEnumerable<PortfolioPosition> positions,
            Dictionary<string, decimal> targetWeights,
            decimal portfolioValue)
        {
            var transactions = new List<PortfolioTransaction>();

            foreach (var position in positions)
            {
                if (!targetWeights.TryGetValue(position.Symbol, out var targetWeight))
                    continue;

                var targetValue = portfolioValue * targetWeight;
                var currentValue = position.MarketValue;
                var difference = targetValue - currentValue;

                if (Math.Abs(difference) > 0)
                {
                    var transaction = new PortfolioTransaction
                    {
                        Symbol = position.Symbol,
                        Type = difference > 0 ? TransactionType.Buy : TransactionType.Sell,
                        Quantity = Math.Abs(difference / position.CurrentPrice),
                        Price = position.CurrentPrice,
                        Amount = Math.Abs(difference),
                        TransactionDate = DateTime.UtcNow
                    };

                    transactions.Add(transaction);
                }
            }

            return transactions;
        }

        /// <summary>
        /// Calculates the rebalancing cost for a set of transactions
        /// </summary>
        /// <param name="transactions">The rebalancing transactions</param>
        /// <param name="feeRate">The transaction fee rate</param>
        /// <returns>The total rebalancing cost</returns>
        /// <remarks>
        /// Rebalancing cost is calculated as the sum of transaction amounts
        /// multiplied by the fee rate.
        /// </remarks>
        public static decimal CalculateRebalancingCost(
            IEnumerable<PortfolioTransaction> transactions,
            decimal feeRate = 0.001m) // Default 0.1% fee rate
        {
            return transactions.Sum(t => t.Amount * feeRate);
        }

        /// <summary>
        /// Optimizes rebalancing transactions to minimize costs
        /// </summary>
        /// <param name="transactions">The rebalancing transactions</param>
        /// <param name="feeRate">The transaction fee rate</param>
        /// <param name="minTransactionSize">Minimum transaction size</param>
        /// <returns>A collection of optimized rebalancing transactions</returns>
        /// <remarks>
        /// Optimization involves:
        - Removing small transactions below minTransactionSize
        - Combining similar transactions where possible
        - Minimizing the number of transactions while maintaining targets
        /// </remarks>
        public static IEnumerable<PortfolioTransaction> OptimizeRebalancingTransactions(
            IEnumerable<PortfolioTransaction> transactions,
            decimal feeRate = 0.001m,
            decimal minTransactionSize = 100m)
        {
            var optimized = new List<PortfolioTransaction>();
            var grouped = transactions.GroupBy(t => t.Symbol);

            foreach (var group in grouped)
            {
                var netAmount = group.Sum(t => t.Type == TransactionType.Buy ? t.Amount : -t.Amount);
                
                if (Math.Abs(netAmount) >= minTransactionSize)
                {
                    optimized.Add(new PortfolioTransaction
                    {
                        Symbol = group.Key,
                        Type = netAmount > 0 ? TransactionType.Buy : TransactionType.Sell,
                        Amount = Math.Abs(netAmount),
                        Quantity = Math.Abs(netAmount / group.First().Price),
                        Price = group.First().Price,
                        TransactionDate = DateTime.UtcNow
                    });
                }
            }

            return optimized;
        }

        /// <summary>
        /// Calculates the deviation from target weights
        /// </summary>
        /// <param name="positions">The current portfolio positions</param>
        /// <param name="targetWeights">The target weights for each position</param>
        /// <returns>A dictionary of weight deviations for each position</returns>
        /// <remarks>
        /// Weight deviation is calculated as the absolute difference
        /// between current weight and target weight.
        /// </remarks>
        public static Dictionary<string, decimal> CalculateWeightDeviations(
            IEnumerable<PortfolioPosition> positions,
            Dictionary<string, decimal> targetWeights)
        {
            var deviations = new Dictionary<string, decimal>();
            var totalValue = positions.Sum(p => p.MarketValue);

            foreach (var position in positions)
            {
                var currentWeight = position.MarketValue / totalValue;
                if (targetWeights.TryGetValue(position.Symbol, out var targetWeight))
                {
                    deviations[position.Symbol] = Math.Abs(currentWeight - targetWeight);
                }
            }

            return deviations;
        }
    }
}
