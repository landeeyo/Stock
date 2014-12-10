﻿using StockTools.BusinessLogic.Abstract;
using StockTools.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTools.BusinessLogic.Concrete
{
    public class BasicPortfolio : IPortfolio
    {
        #region Initialization

        public BasicPortfolio(IPriceProvider priceService, Func<double, double> chargeFunction)
        {
            _priceService = priceService;
            _chargeFunction = chargeFunction;
        }

        #endregion

        #region Properties

        private IPriceProvider _priceService;

        public IPriceProvider PriceService
        {
            get { return _priceService; }
            set { _priceService = value; }
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
            set
            {
                //TODO Modify items and cash or throw exception if there's no cash
                _transactions = value;
            }
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

        public double GetGrossProfit()
        {
            throw new NotImplementedException();
        }

        public double GetNetProfit()
        {
            throw new NotImplementedException();
        }

        public double GetRealisedGrossProfit()
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

            #region Finding companies which have been sold

            Dictionary<string, int> amount = new Dictionary<string, int>();

            var orderedTransactions = Transactions.OrderBy(x => x.Time).ToList();

            foreach (var transaction in orderedTransactions)
            {
                if (transaction.TransactionType == Transaction.TransactionTypes.Buy)
                {
                    if (amount.ContainsKey(transaction.CompanyName))
                    {
                        amount[transaction.CompanyName] += transaction.Amount;
                    }
                    else
                    {
                        amount[transaction.CompanyName] = transaction.Amount;
                    }
                }
                else if (transaction.TransactionType == Transaction.TransactionTypes.Sell)
                {
                    amount[transaction.CompanyName] -= transaction.Amount;
                }
            }

            var soldCompanies = amount.Where(x => x.Value == 0).ToList();

            #endregion

            #region Calculating charges

            var orderedTransactionsOfSoldCompanies = orderedTransactions.Where(x => soldCompanies.Any(y => y.Key == x.CompanyName)).OrderBy(x => x.Time).ToList();

            double charges = 0.0;
            double earnedMoney = 0.0;

            foreach (var item in orderedTransactionsOfSoldCompanies)
            {
                charges += ChargeFunction(item.Value);
                if (item.TransactionType == Transaction.TransactionTypes.Buy)
                {
                    earnedMoney -= item.Value;
                }
                else if (item.TransactionType == Transaction.TransactionTypes.Sell)
                {
                    earnedMoney += item.Value;
                }
                System.Diagnostics.Debug.WriteLine(string.Format("Earned money: {0}", earnedMoney));
            }

            #endregion

            return earnedMoney - charges;
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
                if (item.CompanyName == transaction.CompanyName && item.TransactionType == Transaction.TransactionTypes.Sell)
                {
                    return item;
                }
            }
            return null;
        }

        #endregion





        
    }
}