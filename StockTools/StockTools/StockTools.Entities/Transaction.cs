﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTools.Entities
{
    public class Transaction : ICloneable
    {
        public enum TransactionTypes { Buy, Sell }

        public TransactionTypes TransactionType { get; set; }
        public string CompanyName { get; set; }
        public DateTime Time { get; set; }
        public int Amount { get; set; }
        public double Price { get; set; }
        
        public double Value { get { return Amount * Price; } }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
