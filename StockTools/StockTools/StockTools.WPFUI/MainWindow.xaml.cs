﻿using Microsoft.Win32;
using Moq;
using StockTools.Domain.Abstract;
using StockTools.Domain.Concrete;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace StockTools.WPFUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IIntradayDataDownloader _intradayDataDownloader = new BOSSAIntradayDataDownloader();
        IIntradayDataParser _intradayDataParser = new BOSSAIntradayDataParser();


        public double ChargeFunc(double price)
        {
            if (price <= 769)
            {
                return 3.0;
            }
            else
            {
                return price * (0.39 / 100.0);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            //var outputStream = new MemoryStream();
            _intradayDataDownloader.Address = @"http://bossa.pl/index.jsp?layout=intraday&page=1&news_cat_id=875&dirpath=/mstock/daily/";
            _intradayDataDownloader.Parser = _intradayDataParser;
            _intradayDataDownloader.DownloadToFileParallel(dialog.SelectedPath);
        }

        private PlotModel PrepareModel(Dictionary<DateTime, double> data)
        {
            var Plot = new PlotModel { Title = "Profit" };
            //this.MyModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));

            #region Axis

            var axisX = new DateTimeAxis()
                {
                    Position = AxisPosition.Bottom,
                    Title = "Date"

                };

            Plot.Axes.Add(axisX);

            #endregion

            #region Series

            var series = new LineSeries { Title = "Realised gross profit", MarkerType = MarkerType.Triangle };

            foreach (var point in data)
            {
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(point.Key), point.Value));
            }

            Plot.Series.Add(series);
            #endregion

            return Plot;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                Mock<ICurrentPriceProvider> currentPriceProviderMock = new Mock<ICurrentPriceProvider>();
                currentPriceProviderMock.Setup(x => x.GetPriceByFullName(It.IsAny<string>())).Returns(1.0);
                currentPriceProviderMock.Setup(x => x.GetPriceByShortName(It.IsAny<string>())).Returns(1.0);

                Mock<IArchivePriceProvider> archivePriceProviderMock = new Mock<IArchivePriceProvider>();
                archivePriceProviderMock.Setup(x => x.GetPriceByFullNameAndDateTime(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1.0);

                IPortfolio portfolio = new BasicPortfolio(currentPriceProviderMock.Object, archivePriceProviderMock.Object, ChargeFunc);
                IBookkeepingService bookkeepingService = new MbankBookkeepingService();
                var file = File.ReadAllBytes(openFileDialog.FileName);
                MemoryStream stream = new MemoryStream(file);
                var transactions = bookkeepingService.ReadTransactionHistory(stream);
                //portfolio.Transactions = transactions;
                foreach (var transaction in transactions)
                {
                    portfolio.AddTransaction(transaction);
                }
                profit.Content = portfolio.GetRealisedGrossProfit().ToString();

                var profitByTime = portfolio.GetRealisedGrossProfitTable();

                this.TransactionProfitPlot.Model = PrepareModel(profitByTime);
            }
        }
    }
}
