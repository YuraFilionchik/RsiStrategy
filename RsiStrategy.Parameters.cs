namespace SuperStrategy
{
    using System;
    using StockSharp.Algo.Indicators;
    using StockSharp.Algo.Strategies;
    using StockSharp.Messages;

    public partial class RsiStrategy
    {
        // Параметры стратегии
        private StrategyParam<int> _fastEmaPeriod;
        private StrategyParam<int> _slowEmaPeriod;
        private StrategyParam<int> _longEmaPeriod;
        private StrategyParam<int> _rsiPeriod;
        private StrategyParam<int> _bbPeriod;
        private StrategyParam<decimal> _tradeVolume;
        private StrategyParam<decimal> _riskPerTrade;
        private StrategyParam<DataType> _timeFrame;
        private StrategyParam<bool> _checkTrend;
        //private StrategyParam<bool> _checkATR;        
        private StrategyParam<int> _rsiOpenLong;
        private StrategyParam<int> _rsiOpenShort;
        private StrategyParam<int> _rsiCloseLong;
        private StrategyParam<int> _rsiCloseShort;
        private StrategyParam<decimal> _stopLossMultiplier;
        private StrategyParam<decimal> _bullFilter;
        private StrategyParam<decimal> _bearFilter;
        //private StrategyParam<decimal> _minATR;
        //private StrategyParam<decimal> _maxATR;
        private StrategyParam<int> _atrPeriod;

        // Улучшенное отслеживание сделок
        private int _winCount = 0;
        private int _lossCount = 0;
        private int _slCount = 0;
        private decimal _totalPnL = 0;
        private decimal _winningPnL = 0;
        private decimal _losingPnL = 0;
        private decimal _lastEntryPrice = 0;
        private decimal _tradeEntryVolume = 0;
        private decimal _lastPNL = 0;
        /// <summary>
        /// Инициализация параметров стратегии
        /// </summary>
        private void InitializeParameters()
        {
            // Инициализация параметров стратегии
            _fastEmaPeriod = Param("FastEmaPeriod", 8)
                .SetDisplay("Период быстрой EMA", "Период для быстрой EMA (по умолчанию 8)", "Индикаторы тренда");

            _slowEmaPeriod = Param("SlowEmaPeriod", 21)
                .SetDisplay("Период медленной EMA", "Период для медленной EMA (по умолчанию 21)", "Индикаторы тренда");

            _longEmaPeriod = Param("LongEmaPeriod", 70)
                .SetDisplay("Период длинной EMA", "Период для длинной EMA (по умолчанию 70)", "Индикаторы тренда");

            _rsiPeriod = Param("RsiPeriod", 14)
                .SetDisplay("Период RSI", "Период для RSI (по умолчанию 14)", "Индикаторы перекупленности/перепроданности");

            _bbPeriod = Param("BollingerBandsPeriod", 20)
                .SetDisplay("Период Bollinger Bands", "Период для Bollinger Bands (по умолчанию 20)", "Индикаторы волатильности");

            _tradeVolume = Param("Trade Volume USD", 100.0m)
                .SetDisplay("Объем торговли, USDT", "Базовый объем для торговли (по умолчанию 100.0)", "Управление позицией");

            _riskPerTrade = Param("RiskPerTrade", 0.01m)
                .SetDisplay("Риск на сделку", "Процент риска на одну сделку (по умолчанию 0.01 = 1%)", "Управление рисками");

            _timeFrame = Param("TimeFrame", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Таймфрейм", "Таймфрейм для основной торговли (5 минут)", "Таймфреймы");

            //_checkTrend = Param("Trend filter", true)
            //    .SetDisplay("Фильтр по тренду", "Открывать позиции только по тренду", "Фильтры");

            _rsiOpenLong = Param("RSI Open Long", 24)
                .SetDisplay("Открывать Long если RSI ниже этого значения, 30", "", "Управление позицией");

            _rsiOpenShort = Param("RSI Open Short", 65)
                .SetDisplay("Открывать Short если RSI выше этого значения, 70", "", "Управление позицией");

            _rsiCloseLong = Param("RSI Close Long", 54)
                .SetDisplay("Закрывать Long если RSI выше этого значения, 40", "", "Управление позицией");

            _rsiCloseShort = Param("RSI Close Short", 44)
                .SetDisplay("Закрывать Short если RSI выше этого значения, 60", "", "Управление позицией");

            _stopLossMultiplier = Param("StopLossMultiplier", 1.6m)
                .SetDisplay("Множитель Stop-Loss", "Множитель ATR для Stop-Loss (по умолчанию 1.6)", "Управление позицией");

            _atrPeriod = Param("AtrPeriod", 14)
                .SetDisplay("Период ATR", "Период для ATR (по умолчанию 14)", "Индикаторы волатильности");

            _bullFilter = Param("Filter to Bullish trend", 10m)
                .SetDisplay("Фильтр бычьего тренда", "чем больше - тем меньше чувствительность к бычьему тренду (по умолчанию 10)", "Фильтры");

            _bearFilter = Param("Filter to Bearish trend", -4m)
                .SetDisplay("Фильтр меджвежего тренда", "чем меньше - тем меньше чувствительность к медвежему тренду (по умолчанию -4)", "Фильтры");

            //_checkATR = Param("ATR filter", false)
            //    .SetDisplay("Фильтр по ATR", "Фильтровать слишком низки и высокий ATR (по умолчанию Выкл)", "Фильтры");

            //_minATR = Param("Min ATR", 0.00002m)
            //    .SetDisplay("Граница min. ATR", "Минимальное допустимое значение ATR (по умолчанию 0.00002)", "Фильтры");

            //_maxATR = Param("Max ATR", 0.0001m)
            //    .SetDisplay("Граница max. ATR", "Минимальное допустимое значение ATR (по умолчанию 0.0001)", "Фильтры");

        }

        #region Properties
        // Свойства для параметров стратегии
        public int FastEmaPeriod
        {
            get => _fastEmaPeriod.Value;
            set
            {
                _fastEmaPeriod.Value = value;
                if (_fastEma != null)
                    _fastEma.Length = value;
            }
        }

        public int SlowEmaPeriod
        {
            get => _slowEmaPeriod.Value;
            set
            {
                _slowEmaPeriod.Value = value;
                if (_slowEma != null)
                    _slowEma.Length = value;
            }
        }

        public int LongEmaPeriod
        {
            get => _longEmaPeriod.Value;
            set
            {
                _longEmaPeriod.Value = value;
                if (_longEma != null)
                    _longEma.Length = value;
            }
        }

        public int RsiPeriod
        {
            get => _rsiPeriod.Value;
            set
            {
                _rsiPeriod.Value = value;
                if (_rsi != null)
                    _rsi.Length = value;
            }
        }

        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set
            {
                _atrPeriod.Value = value;
                if (_atr != null)
                    _atr.Length = value;
            }
        }

        //public bool CheckAtr
        //{
        //    get => _checkATR.Value;
            
        //    set => _checkATR.Value = value;
        //}

        //public decimal MinATR
        //{
        //    get => _minATR.Value;
        //    set => _minATR.Value = value;
        //}
        
        //public decimal MaxATR
        //{
        //    get => _maxATR.Value;
        //    set => _maxATR.Value = value;
        //}
        
        
        

        public int BollingerBandsPeriod
        {
            get => _bbPeriod.Value;
            set
            {
                _bbPeriod.Value = value;
                if (_bollingerBands != null)
                    _bollingerBands.Length = value;
            }
        }

        public decimal TradeVolume
        {
            get => _tradeVolume.Value;
            set => _tradeVolume.Value = value;
        }


        public decimal RiskPerTrade
        {
            get => _riskPerTrade.Value;
            set => _riskPerTrade.Value = value;
        }

        public DataType TimeFrame
        {
            get => _timeFrame.Value;
            set => _timeFrame.Value = value;
        }

        public bool CheckTrend
        {
            get => true;
            //get => _checkTrend.Value;
            set => _checkTrend.Value = value;
        }

        public int RsiOpenLong
        {
            get => _rsiOpenLong.Value;
            set => _rsiOpenLong.Value = value;
        }

        public int RsiCloseLong
        {
            get => _rsiCloseLong.Value;
            set => _rsiCloseLong.Value = value;
        }

        public int RsiOpenShort
        {
            get => _rsiOpenShort.Value;
            set => _rsiOpenShort.Value = value;
        }

        public int RsiCloseShort
        {
            get => _rsiCloseShort.Value;
            set => _rsiCloseShort.Value = value;
        }

        public decimal StopLossMultiplier
        {
            get => _stopLossMultiplier.Value;
            set => _stopLossMultiplier.Value = value;
        }

        public decimal BullFilter
        {
            get => _bullFilter.Value;
            set => _bullFilter.Value = value;
        }

        public decimal BearFilter
        {
            get => _bearFilter.Value;
            set => _bearFilter.Value = value;
        }
        #endregion


    }   
}