namespace SuperStrategy
{
    using System;
    using Ecng.ComponentModel;
    using Ecng.Logging;
    using GeneticSharp;
    using StockSharp.Algo.Candles;
    using StockSharp.Algo.Indicators;
    using StockSharp.Charting;
    using StockSharp.Messages;

    public partial class RsiStrategy
    {
        // Индикаторы тренда
        private ExponentialMovingAverage _fastEma;
        private ExponentialMovingAverage _slowEma;
        private ExponentialMovingAverage _longEma;
        private ADX _adx;

        // Индикаторы перекупленности/перепроданности
        private RelativeStrengthIndex _rsi;

        // Индикаторы волатильности
        private AverageTrueRange _atr;
        private BollingerBands _bollingerBands;

        // Объемные индикаторы
        private VolumeIndicator _obv;
        private PnLIndicator pnlIndicator;
        private PnLIndicator testIndicator;
        
        // Текущие и предыдущие значения индикаторов
        IIndicatorValue fastEmaValue;
        IIndicatorValue slowEmaValue;
        IIndicatorValue rsiValue;
        IIndicatorValue obvValue;
        IIndicatorValue bbValue;
        IIndicatorValue longEmaValue;
        IIndicatorValue atrValue;
        IIndicatorValue adxValueComplex;
        IIndicatorValue adxValue;
        IIndicatorValue adxDXValue;
        IIndicatorValue adxDMIValue;
        IIndicatorValue adxMinusValue;
        IIndicatorValue adxPlusValue;
        private decimal _currentFastEma;
        private decimal _previousFastEma;
        private decimal _currentSlowEma;
        private decimal _previousSlowEma;
        private decimal _currentLongEma;
        private decimal _previousLongEma;
        private decimal _currentADXValue;
        private decimal _currentPlusDi;
        private decimal _currentMinusDi;
        private decimal _currentRsi;
        private decimal _previousRsi;
        private decimal _currentAtr;
        private decimal _currentUpperBand;
        private decimal _currentMiddleBand;
        private decimal _currentLowerBand;
        private decimal _currentObv;
        private decimal _previousObv;
        private decimal trendStrength;

        private bool isPortfolioInitialized = false;
        private bool isBullNow;
        
        /// <summary>
        /// Инициализация индикаторов
        /// </summary>
        private void InitializeIndicators()
        {
            try
            {
                // Инициализация индикаторов
                _fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
                _slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
                _longEma = new ExponentialMovingAverage { Length = LongEmaPeriod };
                _rsi = new RelativeStrengthIndex { Length = RsiPeriod };
                _atr = new AverageTrueRange { Length = AtrPeriod };
                _adx = new ADX { Length = LongEmaPeriod };
                _bollingerBands = new BollingerBands
                {
                    Length = BollingerBandsPeriod,
                    Width = 2.0m  // Стандартное отклонение
                };

                _obv = new VolumeIndicator();
                pnlIndicator = new PnLIndicator();
                testIndicator = new PnLIndicator();
                isBullNow = true;
                LogInfo("Индикаторы инициализированы успешно");
            }
            catch (Exception ex)
            {
                LogErrorDetailed("Ошибка при инициализации индикаторов", ex);
            }
        }

        /// <summary>
        /// Добавление индикаторов в коллекцию стратегии
        /// </summary>
        private void AddIndicatorsToStrategy()
        {
            // Добавляем индикаторы в коллекцию стратегии
            Indicators.Add(_fastEma);
            Indicators.Add(_slowEma);
            Indicators.Add(_longEma);
            Indicators.Add(_rsi);
            Indicators.Add(_bollingerBands);
            Indicators.Add(_obv);
            Indicators.Add(_atr);
            Indicators.Add(_adx);
            LogInfo("Индикаторы добавлены в коллекцию стратегии");
        }
        
        /// <summary>
        /// Обработка  свечи
        /// </summary>
        private void ProcessCandles(ICandleMessage candle)
        {
            try
            {
                // Если свеча не финальная, то выходим
                if (candle.State != CandleStates.Finished)
                    return;
                _currentPNL = CalculateCurrentPnL(candle.ClosePrice);

                if (Portfolio != null && !isPortfolioInitialized)
                    InitializePortfolio(candle.OpenPrice);
                
                #region Обработка индикаторов и сохранение текущих значений

                // Сохраняем предыдущие значения
                _previousFastEma = _currentFastEma;
                _previousSlowEma = _currentSlowEma;
                _previousRsi = _currentRsi;
                _previousObv = _currentObv;
                
                ProcessIndicators(candle);               
                                
                #endregion

                // Если индикаторы не сформированы, выходим
                if (!IsFormedAndOnlineAndAllowTrading())
                    return;
                CheckBullTrend();
                var Position = GetCurrentPosition();

                // Проверка сигналов и управление позицией
                if (Position != 0)
                {
                    ManagePosition(candle);
                    
                }
                else
                {
                    CheckEntrySignals(candle);
                }
            }
            catch (Exception ex)
            {
                LogErrorDetailed($"Ошибка в ProcessCandles({candle.OpenTime} - {candle.CloseTime})", ex);
            }
        }

        /// <summary>
        /// Проверка формирования всех индикаторов
        /// </summary>
        private bool AreIndicatorsFormed()
        {
            return _fastEma.IsFormed &&
                   _slowEma.IsFormed &&
                   _longEma.IsFormed &&
                   _rsi.IsFormed &&
                   _atr.IsFormed &&
                   _adx.IsFormed &&
                   _obv.IsFormed;
        }
        
        void ProcessIndicators(ICandleMessage candle)
        {
            try
            {
                fastEmaValue = _fastEma.Process(candle);
                slowEmaValue = _slowEma.Process(candle);
                rsiValue = _rsi.Process(candle);
                obvValue = _obv.Process(candle);
                bbValue = _bollingerBands.Process(candle);
                longEmaValue = _longEma.Process(candle);
                atrValue = _atr.Process(candle);
                adxValueComplex = _adx.Process(candle);
                
                // Преобразуем в десятичные значения
                if (fastEmaValue.IsFormed && !fastEmaValue.IsEmpty)
                    _currentFastEma = fastEmaValue.GetValue<decimal>();

                if (slowEmaValue.IsFormed && !slowEmaValue.IsEmpty)
                    _currentSlowEma = slowEmaValue.GetValue<decimal>();

                if (longEmaValue.IsFormed && !longEmaValue.IsEmpty)
                    _currentLongEma = longEmaValue.GetValue<decimal>();

                if (rsiValue.IsFormed && !rsiValue.IsEmpty)
                    _currentRsi = rsiValue.GetValue<decimal>();

                if (obvValue.IsFormed && !obvValue.IsEmpty)
                    _currentObv = obvValue.GetValue<decimal>();

                if (atrValue.IsFormed && !atrValue.IsEmpty)
                    _currentAtr = atrValue.GetValue<decimal>();

                if (adxValueComplex.IsFormed && !adxValueComplex.IsEmpty)
                {
                    var complexValue = adxValueComplex as ComplexIndicatorValue;
                    if (complexValue != null)
                    {
                        //for chart drawing
                        adxDXValue = complexValue.InnerValues[_adx.Dx];
                        adxValue = complexValue.InnerValues[_adx.MovingAverage];

                        //for signals and filters
                        if (adxDXValue.IsFormed || !adxDXValue.IsEmpty)
                        {
                            var dxComplexValue = adxDXValue as ComplexIndicatorValue;
                            if (dxComplexValue != null)
                            {
                                //adxValue = dxComplexValue.InnerValues[_adx];
                                // Now get +DI and -DI from the Dx component
                                adxPlusValue = dxComplexValue.InnerValues[_adx.Dx.Plus];
                                adxMinusValue = dxComplexValue.InnerValues[_adx.Dx.Minus];
                                adxDMIValue = dxComplexValue.InnerValues[_adx.Dx];
                                _currentPlusDi = adxPlusValue.GetValue<decimal>();
                                _currentMinusDi = adxMinusValue.GetValue<decimal>();
                                _currentADXValue = adxDMIValue.GetValue<decimal>();
                            }
                        }
                    }
                }

                UpdateChart(candle);
            }catch(Exception ex)
            {
                LogError("Ошибка ProcessIndicators", ex);
            }
        }

    }
}