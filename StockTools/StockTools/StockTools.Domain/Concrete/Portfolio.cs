﻿using StockTools.Domain.Abstract;
using StockTools.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTools.Domain.Concrete
{
    public class Portfolio : IPortfolio
    {
        #region Initialization

        public Portfolio(IPriceProvider priceProvider, Func<double, double> chargeFunction)
        {
            _priceProvider = priceProvider;
            _chargeFunction = chargeFunction;
            _transactions = new List<Transaction>();
            _items = new List<InvestmentPortfolioItem>();
        }

        #endregion

        #region Properties

        IPriceProvider _priceProvider;

        public IPriceProvider PriceProvider
        {
            get { return _priceProvider; }
            set { _priceProvider = value; }
        }

        private List<InvestmentPortfolioItem> _items;

        public List<InvestmentPortfolioItem> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        private List<Transaction> _transactions;

        public List<Transaction> Transactions
        {
            get { return _transactions; }
            set { _transactions = value; }
        }

        Func<double, double> _chargeFunction;

        public Func<double, double> ChargeFunction
        {
            get { return _chargeFunction; }
            set { _chargeFunction = value; }
        }

        double _cash;

        public double Cash
        {
            get { return _cash; }
            set { _cash = value; }
        }

        List<Taxation> _taxationList;

        public List<Taxation> TaxationList
        {
            get { return _taxationList; }
            set { _taxationList = value; }
        }

        public double Value
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region Methods

        public void AddTransaction(Transaction transaction)
        {
            var companyExistsInPortfolio = _items.Any(x => x.CompanyName == transaction.CompanyName);
            var canBeSold = _items.Where(x => x.CompanyName == transaction.CompanyName).Where(x => x.NumberOfShares >= transaction.Amount).ToList().Count != 0;
            //var canBeAdded = transaction.TransactionType == Transaction.TransactionTypes.Sell ? canBeSold : true;

            if (transaction.TransactionType == Transaction.TransactionTypes.Buy)
            {
                if (companyExistsInPortfolio)
                {
                    _items.Where(x => x.CompanyName == transaction.CompanyName).Single().NumberOfShares += transaction.Amount;
                }

                _cash -= transaction.Value;
                _cash -= ChargeFunction(transaction.Value);

                if (!companyExistsInPortfolio)
                {
                    _items.Add(new InvestmentPortfolioItem()
                    {
                        CompanyName = transaction.CompanyName,
                        NumberOfShares = transaction.Amount
                    });
                }

                _transactions.Add(transaction);
            }
            else
            {
                if (canBeSold)
                {
                    if (companyExistsInPortfolio)
                    {
                        _items.Where(x => x.CompanyName == transaction.CompanyName).Single().NumberOfShares -= transaction.Amount;
                    }

                    _cash += transaction.Value;
                    _cash -= ChargeFunction(transaction.Value);
                    _transactions.Add(transaction);
                }
            }
        }

        public double GetGrossProfitByDate(DateTime? date)
        {
            #region Prerequisite check

            if (Transactions == null)
            {
                throw new Exception("Transactions has not been set");
            }

            if (ChargeFunction == null)
            {
                throw new Exception("Charge function has not been set");
            }

            #endregion

            if (!date.HasValue)
            {
                date = DateTime.Now;
            }

            var realisedProfitByDate = GetRealisedGrossProfitByDate(date);
            var unrealisedProfitByDate = 0.0;

            //Backup of transactions and items lists
            var backupTransactions = _transactions.Select(x => x.Clone() as Transaction).ToList();
            var backupItems = _items.Select(x => x.Clone() as InvestmentPortfolioItem).ToList();

            _transactions = new List<Transaction>();
            _items = new List<InvestmentPortfolioItem>();

            //Simulating portfolio state at given date
            var transactionsUntilDate = backupTransactions.Where(x => x.Time < date).OrderBy(x => x.Time).ToList();
            foreach (var transaction in transactionsUntilDate)
            {
                this.AddTransaction(transaction);
            }

            var unsoldStocks =
                (from item in _items
                 where item.NumberOfShares > 0
                 select item.CompanyName).ToList();

            foreach (var stock in unsoldStocks)
            {
                var averageBuyPrice = _transactions.Where(x => x.CompanyName == stock)
                        .Where(x => x.TransactionType == Transaction.TransactionTypes.Buy)
                        .Sum(x => x.Value) / _items.Where(x => x.CompanyName == stock).Single().NumberOfShares;
                var amount = _items.Where(x => x.CompanyName == stock).Single().NumberOfShares;

                //Reading price
                double? price = null;
                price = _priceProvider.GetClosestPrice(stock, date.Value);
                //if (!currentPrice.HasValue)
                //{
                //    currentPrice = _currentPriceProvider.GetPriceByFullName(stock);
                //}
                if (price.HasValue)
                {
                    //Incrementing profit
                    unrealisedProfitByDate += price.Value * amount - averageBuyPrice * amount;
                    //Clearing
                    _items.Where(x => x.CompanyName == stock).Single().NumberOfShares = 0;
                }
            }

            //Restoring state
            _transactions = backupTransactions;
            _items = backupItems;

            //TODO Add test method for this method
            return realisedProfitByDate + unrealisedProfitByDate;
        }

        public double GetGrossProfit()
        {
            return GetGrossProfitByDate((DateTime.Now));
        }

        public double GetNetProfit()
        {
            throw new NotImplementedException();
        }

        private double GetRealisedGrossProfitByDate(DateTime? date)
        {
            #region Prerequisite check

            if (Transactions == null)
            {
                throw new Exception("Transactions has not been set");
            }

            if (ChargeFunction == null)
            {
                throw new Exception("Charge function has not been set");
            }

            #endregion

            if (!date.HasValue)
            {
                date = DateTime.Now;
            }

            var orderedTransactions = Transactions.Where(x => x.Time <= date).OrderBy(x => x.Time).ToList();
            Dictionary<string, Queue<Transaction>> companyTransaction = new Dictionary<string, Queue<Transaction>>();

            for (int i = 0; i < orderedTransactions.Count; i++)
            {
                var transaction = orderedTransactions[i];

                if (companyTransaction.ContainsKey(transaction.CompanyName))
                {
                    companyTransaction[transaction.CompanyName].Enqueue(transaction);
                }
                else
                {
                    companyTransaction[transaction.CompanyName] = new Queue<Transaction>();
                    companyTransaction[transaction.CompanyName].Enqueue(transaction);
                }
            }

            double charges = 0.0;
            double earnedMoney = 0.0;

            foreach (var key in companyTransaction.Keys)
            {
                var buyAmount = companyTransaction[key]
                    .Where(x => x.TransactionType == Transaction.TransactionTypes.Buy)
                    .Sum(x => x.Amount);
                var sellAmount = companyTransaction[key]
                    .Where(x => x.TransactionType == Transaction.TransactionTypes.Sell)
                    .Sum(x => x.Amount);

                if (buyAmount == sellAmount)
                {
                    while (companyTransaction[key].Count > 0)
                    {
                        var transaction = companyTransaction[key].Dequeue();
                        if (transaction.TransactionType == Transaction.TransactionTypes.Sell)
                        {
                            earnedMoney += transaction.Value;
                            charges += ChargeFunction(transaction.Value);
                        }
                        if (transaction.TransactionType == Transaction.TransactionTypes.Buy)
                        {
                            earnedMoney -= transaction.Value;
                            charges += ChargeFunction(transaction.Value);
                        }
                    }
                }

                if (buyAmount > sellAmount)
                {
                    if (sellAmount == 0)
                        continue;
                    DateTime lastSellTime = companyTransaction[key]
                        .Where(x => x.TransactionType == Transaction.TransactionTypes.Sell)
                        .OrderByDescending(x => x.Time)
                        .ToList()[0].Time;
                    var buyAmountBeforeLastSell = companyTransaction[key]
                        .Where(x => x.TransactionType == Transaction.TransactionTypes.Buy)
                        .Where(x => x.Time <= lastSellTime)
                        .Sum(x => x.Amount);
                    var averageBuyPrice = companyTransaction[key]
                        .Where(x => x.TransactionType == Transaction.TransactionTypes.Buy)
                        .Where(x => x.Time <= lastSellTime)
                        .Sum(x => x.Value) / buyAmountBeforeLastSell;
                    var sellLimit = sellAmount;

                    while (sellLimit > 0)
                    {
                        var transaction = companyTransaction[key].Dequeue();
                        if (transaction.TransactionType == Transaction.TransactionTypes.Sell)
                        {
                            sellLimit -= transaction.Amount;
                            charges += ChargeFunction(transaction.Value);
                            earnedMoney += transaction.Value - averageBuyPrice * transaction.Amount;
                        }
                        else if (transaction.TransactionType == Transaction.TransactionTypes.Buy)
                        {
                            charges += ChargeFunction(transaction.Value);
                        }
                        if (companyTransaction[key].Count == 0)
                        {
                            break;
                        }
                    }
                }

                if (buyAmount < sellAmount)
                {
                    throw new Exception("Error in the transaction list, there are more sold papers than bought!");
                }
            }

            return earnedMoney - charges;
        }

        public double GetRealisedGrossProfit(DateTime? date)
        {
            return GetRealisedGrossProfitByDate(date);
        }

        public double GetRealisedGrossProfit()
        {
            return GetRealisedGrossProfitByDate(DateTime.Now);
        }

        public double GetRealisedNetProfit()
        {
            throw new NotImplementedException();
        }

        public List<Transaction> GetPairedTransactions()
        {
            if (Transactions.Count == 0)
            {
                return new List<Transaction>();
            }

            var paired = new List<int>();
            for (int i = Transactions.Count - 1; i >= 0; i--)
            {
                if (paired.Any(x => x == i))
                {
                    continue;
                }
                var pair = GetTransactionPair(Transactions[i]);
                if (pair != null)
                {
                    paired.Add(Transactions.IndexOf(Transactions[i]));
                    paired.Add(Transactions.IndexOf(pair));
                }
            }

            var result = new List<Transaction>(paired.Count);
            for (int i = paired.Count - 1; i >= 0; i--)
            {
                result.Add(Transactions[paired[i]]);
            }

            return result;
        }

        public Transaction GetTransactionPair(Transaction transaction)
        {
            var index = Transactions.IndexOf(transaction);
            for (int i = index - 1; i >= 0; i--)
            {
                var item = Transactions[i];
                if (item.CompanyName == transaction.CompanyName && item.TransactionType == (transaction.TransactionType == Transaction.TransactionTypes.Sell ? Transaction.TransactionTypes.Buy : Transaction.TransactionTypes.Sell))
                {
                    return item;
                }
            }
            return null;
        }

        public Dictionary<DateTime, double> GetRealisedGrossProfitTable()
        {
            var firstTransactionDate = Transactions.OrderBy(x => x.Time).ToList()[0].Time;
            var result = new Dictionary<DateTime, double>();

            for (DateTime date = firstTransactionDate; date < DateTime.Now; date = date.AddMinutes(15))
            {
                result[date] = GetRealisedGrossProfitByDate(date);
            }

            return result;
        }

        public Dictionary<DateTime, double> GetGrossProfitTable()
        {
            var firstTransactionDate = Transactions.OrderBy(x => x.Time).ToList()[0].Time;
            var result = new Dictionary<DateTime, double>();

            //TODO Rethink and implement smarter
            //for (DateTime date = firstTransactionDate; date < DateTime.Now; date = date.AddMinutes(15))
            ////for (DateTime date = firstTransactionDate; date < DateTime.Now; date = date.AddSeconds(10))
            //{
            //    result[date] = GetGrossProfitByDate(date);
            //}

            return result;
        }

        #endregion
    }
}
