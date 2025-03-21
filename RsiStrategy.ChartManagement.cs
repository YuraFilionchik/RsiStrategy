namespace SuperStrategy
{
    using System;
    using StockSharp.Messages;
    using StockSharp.Charting;
    using StockSharp.Algo.Indicators;
    using Newtonsoft.Json.Linq;
    using Ecng.Drawing;
    using StockSharp.BusinessEntities;
    using System.Drawing;
    using StockSharp.Xaml.Charting;
    using DevExpress.Mvvm.Native;
    using StockSharp.Algo.Candles;
    using DevExpress.Charts.Model;
    using StockSharp.Algo.Strategies;

    public partial class RsiStrategy
    {
        
        // Элементы графика
        private IChartTradeElement _tradesElement;
        private IChartCandleElement _candleElement;
        private IChartIndicatorElement _fastEmaElement;
        private IChartIndicatorElement _slowEmaElement;
        private IChartIndicatorElement _longEmaElement;
        private IChartIndicatorElement _rsiElement;
        private IChartIndicatorElement _adxElement;
        private IChartIndicatorElement _adxPlusElement;
        private IChartIndicatorElement _adxMinusElement;
        private IChartIndicatorElement _obvElement;
        private IChartIndicatorElement _bollingerUpperElement;
        private IChartIndicatorElement _bollingerMiddleElement;
        private IChartIndicatorElement _bollingerLowerElement;
        private IChartIndicatorElement _pnlElement;
        private IChartIndicatorElement _testElement;
        private IChartOrderElement _ordersElement;
        private IChartLineElement _stopLossLine;
        private IChartLineElement _takeProfitLine;
        private IChart _chart;
        private IChartArea _pnlArea;
        private IChartArea priceArea;
        private IChartArea indicatorArea;
        private Queue<decimal> LongValues; //history
        private bool isChatPropertiesApplied = false;
        int lenghtLongQueue = 4;
        /// <summary>
        /// Инициализация графика
        /// </summary>
        private void InitializeChart()
        {
            try
            {
                // Инициализация графика
                _chart = GetChart();

                if (_chart == null)
                {
                    LogInfo("Chart unavailable, visualization disabled.");
                    return;
                }
                
                LongValues = new Queue<decimal>(lenghtLongQueue);
                
                // Create chart areas
                priceArea = CreateChartArea();
                indicatorArea = CreateChartArea();
                _pnlArea = CreateChartArea();
                _pnlArea.Height = 140; 
                 
                _pnlElement = _pnlArea.AddIndicator(pnlIndicator);
                _pnlElement.IsVisible = true;
                //_pnlElement.Color = System.Drawing.Color.Green;
                
                // Add elements to chart
                _candleElement = priceArea.AddCandles();
                _tradesElement = DrawOwnTrades(priceArea);
                  
                // Add indicators
                _fastEmaElement = priceArea.AddIndicator(_fastEma);
                _fastEmaElement.Color = System.Drawing.Color.Blue;
                _slowEmaElement = priceArea.AddIndicator(_slowEma);
                _slowEmaElement.Color = System.Drawing.Color.Red;

                _longEmaElement = priceArea.AddIndicator(_longEma);
                _longEmaElement.Color = System.Drawing.Color.GreenYellow;
                _longEmaElement.StrokeThickness = 3;
                _obvElement = _pnlArea.AddIndicator(_obv);
                //_rsiElement = indicatorArea.AddIndicator(_rsi);
                //_rsiElement.IsVisible = false;
                //_adxElement = indicatorArea.AddIndicator(_adx.MovingAverage);
                _adxPlusElement = indicatorArea.AddIndicator(new SimpleMovingAverage { Length = 1 });
                _adxMinusElement = indicatorArea.AddIndicator(new SimpleMovingAverage { Length = 1 });
                _adxPlusElement.Color = System.Drawing.Color.Green;
                _adxMinusElement.Color = System.Drawing.Color.Red;
                
                //_testElement = indicatorArea.AddIndicator(testIndicator);
                //_testElement.StrokeThickness = 2;
                LogInfo("Chart initialized successfully.");
            }
            catch (Exception ex)
            {
                LogErrorDetailed("Error initializing chart", ex);
            }
        }

        //void DrawLineOnChart(IChartArea area, decimal value, Color color)
        //{
        //    return;
        //    if (_chart == null)
        //        return;
        //    var lineElement = _chart.CreateLineElement();
        //    lineElement.Color = color;
        //    lineElement.StrokeThickness = 2;
        //    _chart.AddElement(area, lineElement);
        //    var data = _chart.CreateData();
        //    data.Group(StartedTime).Add(lineElement, value);
        //    _chart.Draw(data);

        //}
        

        /// <summary>
        /// Обновление графика
        /// </summary>
        private void UpdateChart(ICandleMessage candle)
        {
            try
            {
                if (_chart == null)
                    return;
                
                var data = _chart.CreateData();
                var group = data.Group(candle.OpenTime);
                
                // Добавляем данные на график
                group.Add(_candleElement, candle);
                group.Add(_slowEmaElement, slowEmaValue);
                //group.Add(_rsiElement, rsiValue);
                //group.Add(_rsiElement, new DecimalIndicatorValue(_rsi,0, candle.OpenTime)); //zero line
                group.Add(_fastEmaElement, fastEmaValue);
                group.Add(_longEmaElement, longEmaValue);
                var position = GetCurrentPosition();
                //add PNL info 
                //group.Add(_pnlElement, new DecimalIndicatorValue(pnlIndicator, position == 0 ? _lastPNL : 0, candle.OpenTime));
                group.Add(_pnlElement, new DecimalIndicatorValue(pnlIndicator, isBullNow ? 1 : -1, candle.OpenTime));
                //group.Add(_obvElement, obvValue);
                group.Add(_obvElement, new DecimalIndicatorValue(_obv, 0, candle.OpenTime));
                //group.Add(_adxElement, adxValue);                
                //group.Add(_adxPlusMinusElement, adxDXValue);
                group.Add(_adxPlusElement, new DecimalIndicatorValue(pnlIndicator, _currentPlusDi, candle.OpenTime));
                group.Add(_adxMinusElement, new DecimalIndicatorValue(pnlIndicator, _currentMinusDi, candle.OpenTime));
                _chart.Draw(data);
                
            }
            catch (Exception ex)
            {
                // Ошибки визуализации не критичны для работы стратегии
                LogError($"Ошибка при обновлении графика: {ex.Message}");
            }
        }
    }
}