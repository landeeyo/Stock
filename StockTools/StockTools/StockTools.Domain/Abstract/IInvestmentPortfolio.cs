﻿using StockTools.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTools.BusinessLogic.Abstract
{
    public interface IInvestmentPortfolio
    {
        /// <summary>
        /// Sum of current prices of all shares in the portfolio
        /// </summary>
        double GetValue();

        /// <summary>
        /// Gross profit of the portfolio
        /// </summary>
        double GetGrossProfit();

        /// <summary>
        /// Net profit of the portfolio
        /// </summary>
        double GetNetProfit();

        /// <summary>
        /// Gross realised profit of the portfolio (only sold shares)
        /// </summary>
        double GetRealisedGrossProfit();

        /// <summary>
        /// Net realised profit of the portfolio (only sold shares)
        /// </summary>
        double GetRealisedNetProfit();

        /// <summary>
        /// Sets charge function which is necessary for profit calculation
        /// </summary>
        void SetChargeFunc(Func<double, double> chargeFunction);

        /// <summary>
        /// Sets value of capital gain tax (during the time because we assume that it can change)
        /// </summary>
        void SetTaxation(List<Taxation> taxationList);
    }
}
