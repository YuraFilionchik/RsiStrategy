﻿
namespace SuperStrategy
{
    using System;
    using System.Drawing;
    using DevExpress.Charts.Model;
    using Ecng.Drawing;
    using Ecng.Logging;
    using NuGet.Common;
    using StockSharp.Algo;
    using StockSharp.Algo.Candles;
    using StockSharp.Algo.Indicators;
    using StockSharp.Algo.Strategies;
    using StockSharp.Algo.Testing;
    using StockSharp.BusinessEntities;
    using StockSharp.Charting;
    using StockSharp.Logging;
    using StockSharp.Messages;

    /// <summary>
    /// Основной класс стратегии для торговли на крипторынке
    /// </summary>
    public partial class RsiStrategy : Strategy
    {
        public static readonly DateTime BuildTimestamp = DateTime.Now;
        private DateTimeOffset _startTimeStrategy;
        private bool IsOptimizationMode;
        ///<summary>
        /// Конструктор стратегии
        /// </summary>
        public RsiStrategy()
        {
            Name = "RSI";
            
            // Инициализация логирования
            InitializeLogging();

            // Инициализация параметров
            InitializeParameters();

            // Инициализация индикаторов
            InitializeIndicators();
        }

        /// <summary>
        /// Запуск стратегии
        /// </summary>
        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            if (!IsSecurityValid())
            {
                Stop(new("Плохие данные Security"));
            }
            InitializeIndicators();
            
            // Логирование запуска стратегии
            LogInfo("Стратегия запущена.");
            IsOptimizationMode = IsOptimizing();
            if (IsBacktesting)
            {                
                if (IsOptimizationMode)
                    LogWarning("Running in optimazing mode");
                else
                    LogWarning("Running in backtesting mode");                
            }
            else
                LogWarning("Running in Live mode");
            IsOptimizationMode = IsOptimizing();
            // Добавляем индикаторы в коллекцию стратегии
            AddIndicatorsToStrategy();
            InitializeStatistics();
            // Создаем и подписываемся на свечи
            var subscription = new Subscription(TimeFrame, Security);

            // Инициализация графика
            InitializeChart();

            // Устанавливаем обработчики для свечей
            this.WhenCandlesFinished(subscription)
                .Do(ProcessCandles)
                .Apply(this);

            // Подписываемся на свечи
            Subscribe(subscription);
        }

        protected override void OnStopped()
        {
            LogPerformanceMetrics();
            
            // Логирование остановки стратегии
            LogInfo("Стратегия остановлена.");
            base.OnStopped();
        }

        /// <summary>
        /// Информация о используемых данных для Designer
        /// </summary>
        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
            return new[]
            {
                (Security, TimeFrame),
            };
        }

        protected override void OnNewMyTrade(MyTrade trade)
        {
            base.OnNewMyTrade(trade);
            
            var Position = GetCurrentPosition();
            _isPositionOpened = Position != 0;

            var chart = _chart;
            if (chart != null &&  (!IsOptimizationMode))
            {
                var data = chart.CreateData();
                data.Group(trade.Trade.ServerTime).Add(_tradesElement, trade);
                chart.Draw(data);
            }
            
            // Если позиция открыта (или частично открыта)
            if (Position != 0 && _lastEntryPrice == 0)
            {
                _lastEntryPrice = trade.Trade.Price;
                _tradeEntryVolume = trade.Trade.Volume;
                _lastPNL = 0;
                if (_chart != null)
                {
                    //var data = _chart.CreateData();
                    //data.Group(trade.Trade.ServerTime).Add(_pnlElement, new DecimalIndicatorValue(pnlIndicator, _lastPNL, trade.Trade.ServerTime));
                    //_chart.Draw(data);
                }
                // LogInfo($"Зафиксирована цена входа: {_lastEntryPrice}, объем: {_tradeEntryVolume} ({_lastEntryPrice * _tradeEntryVolume}$)");
            }
            // Если позиция закрыта (или частично закрыта)
            else if (Position == 0 || (Math.Sign(Position) != Math.Sign(Position - trade.Trade.Volume)))
            {
                if (_lastEntryPrice != 0)
                {
                    decimal volume = Math.Min(_tradeEntryVolume, trade.Trade.Volume);
                    decimal pnl;

                    // Расчет PnL
                    if (trade.Order.Side == Sides.Buy)
                    {
                        // Закрытие короткой позиции
                        pnl = (_lastEntryPrice - trade.Trade.Price) * volume;
                    }
                    else
                    {
                        // Закрытие длинной позиции
                        pnl = (trade.Trade.Price - _lastEntryPrice) * volume;
                    }

                    // Обновляем статистику
                    _totalPnL += pnl;

                    string resultText = pnl > 0 ? "ПРИБЫЛЬНАЯ" : "УБЫТОЧНАЯ";
                    //LogInfo($"Сделка {resultText}. PnL: {pnl}. Всего PnL: {_totalPnL}");
                    //add full logging 
                    if (pnl > 0)
                    {
                        _winCount++;
                        _winningPnL += pnl;
                    }
                    else
                    {
                        _lossCount++;
                        _losingPnL += Math.Abs(pnl);
                    }
                    
                    _lastPNL = pnl;
                    if (_chart != null)
                    {
                        //var data = _chart.CreateData();
                        //data.Group(trade.Trade.ServerTime).Add(_pnlElement, new DecimalIndicatorValue(pnlIndicator, pnl, trade.Trade.ServerTime));
                        //_chart.Draw(data);
                        
                    }

                    //TEST ONLY FOR BACKTESTS
                    if (IsOptimizationMode)
                    {
                        var relativePnL = 100 * _totalPnL / TradeVolume;
                        if ((relativePnL < 10 && (CurrentTime - _startTimeStrategy).TotalDays > 80) ||
                           (relativePnL < -30 && (CurrentTime - _startTimeStrategy).TotalDays > 30))
                        {
                            LogWarning($"Слабая стратегия, не тратим ресурсы. Total PNL = {_totalPnL} ({CurrentTime})");
                            Stop();
                            return;
                        }
                    }

                    // Обновляем статистику
                    decimal winRate = (_winCount + _lossCount) > 0 ? (decimal)_winCount / (_winCount + _lossCount) : 0;
                    LogInfo($"Статистика: Побед: {_winCount}, Поражений: {_lossCount}, Винрейт: {winRate:P2}");

                    // Сбрасываем
                    if (Position == 0)
                    {
                        _lastEntryPrice = 0;
                        _tradeEntryVolume = 0;
                    }
                    else
                    {
                        // Если осталась часть позиции, обновляем данные
                        _tradeEntryVolume = Math.Abs(Position);
                    }
                }
            }
        }

        protected override void OnOrderRegisterFailed(OrderFail fail, bool calcRisk)
        {
            base.OnOrderRegisterFailed(fail, calcRisk);
            LogError($"Order registration failed: {fail.Error}");
        }

        protected override void OnOrderChanged(Order order)
        {
            base.OnOrderChanged(order);
            var Position = GetCurrentPosition();
            if (order.State == OrderStates.Done)
            {
                //LogInfo($"Order {order.TransactionId} executed. Position now: {Position}");
            }
            else if (order.State == OrderStates.Failed)
            {
                LogError($"Order {order.TransactionId} failed");
            }
        }

        private void InitializeStatistics()
        {
            // Регистрируем счетчики для отслеживания статистики
            _winCount = 0;
            _lossCount = 0;
            _totalPnL = 0;
            _winningPnL = 0;
            _losingPnL = 0;
            _slCount = 0;
            LogInfo("Статистика инициализирована успешно");
        }

        private void LogPerformanceMetrics()
        {
            try
            {
                if (IsOptimizationMode) return;
                
                // Расчет метрик производительности
                decimal winRate = _winCount + _lossCount > 0
                    ? (decimal)_winCount / (_winCount + _lossCount)
                    : 0;

                decimal profitFactor = _losingPnL != 0
                    ? _winningPnL / _losingPnL
                    : 0;

                decimal averageWin = _winCount > 0
                    ? _winningPnL / _winCount
                    : 0;

                decimal averageLoss = _lossCount > 0
                    ? _losingPnL / _lossCount
                    : 0;

                decimal rrRatio = averageLoss != 0
                    ? averageWin / averageLoss
                    : 0;

                // Лог метрик производительности
                LogInfo($"===== ИТОГОВАЯ СТАТИСТИКА =====");
                LogInfo($"Общий PnL: {_totalPnL.ToString("N2")} USDT");
                LogInfo($"Винрейт: {winRate.ToString("P2")}");
                LogInfo($"Профит-фактор: {profitFactor.ToString("N2")}");
                LogInfo($"Средняя прибыль: {averageWin.ToString("N2")} USDT");
                LogInfo($"Средний убыток: {averageLoss.ToString("N2")} USDT");
                LogInfo($"Risk-Reward Ratio: {rrRatio.ToString("N2")}");
                LogInfo($"Всего сделок: {_winCount + _lossCount}");
                LogInfo($"Прибыльных сделок: {_winCount}");
                LogInfo($"Убыточных сделок: {_lossCount}");
                LogInfo($"Число StopLoss: {_slCount}");
                LogInfo($"=============================");
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при логировании метрик: {ex.Message}");
            }
        }

        private void LogLevels(decimal entryPrice, decimal stopLossPrice, decimal takeProfitPrice)
        {
            LogInfo($"=== УРОВНИ ПОЗИЦИИ ===");
            LogInfo($"Цена входа: {entryPrice:F8}");
            LogInfo($"Stop-Loss: {stopLossPrice:F8} ({(Math.Abs(stopLossPrice - entryPrice) / entryPrice):P2})");
            LogInfo($"Take-Profit: {takeProfitPrice:F8} ({(Math.Abs(takeProfitPrice - entryPrice) / entryPrice):P2})");
            LogInfo($"=====================");
        }

        private bool IsSecurityValid()
        {
            if (Security == null)
            {
                LogError("Security is null. Стратегия не была запущена");
                return false;
            }
            return true;
            //LogInfo("======Security info======");
            //LogInfo(Security.ToString());
            //LogInfo($"Security settings: VolumeStep={Security.VolumeStep}, PriceStep={Security.PriceStep}");

            //bool result = Security.VolumeStep != null && Security.VolumeStep != 0 &&
            //    Security.PriceStep != null && Security.PriceStep != 0;

            //if (!result) LogInfo("Стратегия не запущена из-за плохих данных Security");
            //return result;

        }

        private void InitializePortfolio(Decimal price)
        {
            //Portfolio.BeginValue = TradeVolume     / price;
            //Portfolio.CurrentValue = TradeVolume / price;
            //Portfolio.Currency = Ecng.Common.CurrencyTypes.USDT;
            //Portfolio.Security = Security;
            //Portfolio.StrategyId = this.Id.ToString();

            isPortfolioInitialized = true;
        }

        private bool IsOptimizing()
        {
            // Check if we're running in optimization mode
            // This property might be available as a parameter on your strategy
            var optimizing = Parameters.TryGetValue("IsOptimizing", out var param) &&
                            param is IStrategyParam optParam &&
                            optParam.Value != null &&
                            (bool)optParam.Value;

            // Alternative check - optimization often disables chart drawing
            if (!optimizing)
            {
                optimizing = GetChart() == null;
            }

            return optimizing && IsBacktesting;
        }
    }
}