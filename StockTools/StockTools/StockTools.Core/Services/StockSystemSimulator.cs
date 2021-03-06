﻿using StockTools.Core.Interfaces;
using StockTools.Core.Models;
using StockTools.Core.Models.Delegates;
using StockTools.Core.Models.EventArgs;
using StockTools.Core.Models.Exceptions;
using System;

namespace StockTools.Core.Services
{
    public class StockSystemSimulator : IStockSystemSimulator
    {
        #region DI

        private IHistoricalPriceRepository _historicalPriceRepository;

        public IHistoricalPriceRepository HistoricalPriceRepository
        {
            get { return _historicalPriceRepository; }
        }

        private IOrderProcessor _orderProcessor;

        public IOrderProcessor OrderProcessor
        {
            get { return _orderProcessor; }
            set { _orderProcessor = value; }
        }

        private IPortfolio _portfolio;

        public IPortfolio Portfolio
        {
            get { return _portfolio; }
            set { _portfolio = value; }
        }

        public StockSystemSimulator(DateTime beginDate, IHistoricalPriceRepository historicalPriceRepository, IOrderProcessor orderProcessor, IPortfolio portfolio)
        {
            _historicalPriceRepository = historicalPriceRepository;
            _orderProcessor = orderProcessor;
            _currentDate = beginDate;
            _portfolio = portfolio;

            //When date is changed this class emits signal that it has changed
            this.CurrentDateChanged += _orderProcessor.OnCurrentDateChanged;
            //When order process processes an order it will be handled
            //by OnOrderProcessed method and propagated further
            _orderProcessor.OrderProcessed += this.OnOrderProcessed;
        }

        #endregion DI

        private DateTime _currentDate;

        public DateTime CurrentDate
        {
            get { return _currentDate; }
        }

        public bool IsStockMarketAvailable
        {
            get { return _historicalPriceRepository.AnyTradingInDay(_currentDate); }
        }

        public void Tick(TimeSpan timeSpan)
        {
            _currentDate = _currentDate.Add(timeSpan);
            if (CurrentDateChanged != null)
            {
                CurrentDateChanged(this, EventArgs.Empty);
            }
        }

        public void SubmitOrder(Order order)
        {
            //1. Is there stock like this?
            //2. What's the price of stock?
            //3. Is there enough money?
            //4. Transform order to transaction
            if (_historicalPriceRepository.IsThereCompany(order.CompanyName, CurrentDate) == false)
            {
                throw new CompanyDoesNotExistException(order.CompanyName);
            }
            var stockPrice = _historicalPriceRepository.GetClosest(order.CompanyName, CurrentDate);
            var orderValue = order.Amount * stockPrice;
            if (order.OrderType == Order.OrderTypes.Buy && Portfolio.Cash < orderValue)
            {
                throw new NotEnoughMoneyException(Portfolio);
            }

            _orderProcessor.Enqueue(order);
            //When order will be processed it will fire OnOrderProcessed method
            //by raising OrderProcessed event of OrderProcessor class
        }

        private void OnOrderProcessed(object sender, OrderEventArgs eventArgs)
        {
            OrderProcessed(sender, eventArgs);
        }

        public event OrderProcessedDelegate OrderProcessed;

        public event EventHandler CurrentDateChanged;
    }
}