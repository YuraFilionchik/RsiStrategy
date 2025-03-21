namespace SuperStrategy
{
    using System;
    using StockSharp.Algo.Indicators;
    using StockSharp.Messages;

    public partial class RsiStrategy
    {
        
        /// <summary>
        /// Проверка сигналов разворота
        /// </summary>
        private bool CheckReverseSignals(ICandleMessage candle, Sides positionSide)
        {
            try
            {
                // Проверка пересечения EMA в противоположном направлении
                bool isFastCrossedBelow = _currentFastEma < _currentSlowEma &&
                                        _previousFastEma >= _previousSlowEma;

                bool isFastCrossedAbove = _currentFastEma > _currentSlowEma &&
                                        _previousFastEma <= _previousSlowEma;

                if ((positionSide == Sides.Buy && isFastCrossedBelow) ||
                    (positionSide == Sides.Sell && isFastCrossedAbove))
                {
                    LogInfo($"Обнаружен сигнал разворота: пересечение EMA в противоположном направлении");
                    return true;
                }

                // Проверка экстремальных значений RSI
                if ((positionSide == Sides.Buy && _currentRsi > 80) ||
                    (positionSide == Sides.Sell && _currentRsi < 20))
                {
                    LogInfo($"Обнаружен сигнал разворота: экстремальное значение RSI = {_currentRsi}");
                    return true;
                }

                // Проверка свечных моделей разворота
                bool isDoji = Math.Abs(candle.OpenPrice - candle.ClosePrice) / candle.OpenPrice < 0.001m &&
                            (candle.HighPrice - candle.LowPrice) / candle.OpenPrice > 0.005m;

                if (isDoji)
                {
                    LogInfo("Обнаружен сигнал разворота: паттерн доджи");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogErrorDetailed("Ошибка при проверке сигналов разворота", ex);
                return false;
            }
        }

        /// <summary>
        /// Фильтр времени
        /// </summary>
        private bool CheckTimeFilter()
        {
            // Проверка времени сессии - можно настроить для конкретной биржи
            return true;
        }

        
         /// <summary>
        /// Фильтр глобального тренда
        /// </summary>
        private bool CheckGlobalTrendFilter(bool isLong)
        {
            return true;
            try
            {
                if (!CheckTrend) return true;
                // Определение направления глобального тренда по longEMA
                bool isUptrend = _currentLongEma > _previousLongEma;
                
                bool result = isLong == isUptrend;
                _previousLongEma = _currentLongEma;
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Ошибка в фильтре глобального тренда: {ex.Message}");
                return true; // По умолчанию пропускаем сигнал при ошибке
            }
        }
        #region Архив функций
        //private bool FilterMaxATR()
        //{
        //    return _currentAtr < MaxATR;
        //}

        //private bool FilterMinATR()
        //{
        //    return _currentAtr > MinATR;
        //}

        //private bool ATRFilter()
        //{
        //    if (!CheckAtr) return true;
        //    return FilterMaxATR() && FilterMinATR(); 
        //}
        #endregion
        private bool TrendFilter(bool isLong, decimal price)
        {
            try
            {
                if (!CheckTrend) return true; //работа выключателя в настройках стратегии
                //if (!isLong) return false; //shorts OFF

                return CheckBullTrend() == isLong;
                //if (!CheckBullTrend()) return false;
                //if (CheckBullTrend()) return true;
                return isLong;
                
            }
            catch (Exception ex)
            {
                LogError($"Ошибка в фильтре глобального тренда: {ex.Message}");
                return true; // По умолчанию пропускаем сигнал при ошибке
            }
        }

        private bool CheckBullTrend()
        {
                var plus_minus = _currentPlusDi - _currentMinusDi;
            if (isBullNow)
            {
                //from bull to bear
                if (plus_minus < BearFilter) //-4
                {
                    isBullNow = false;
                }

            }
            else
            { //from bear to bull
                if (plus_minus > BullFilter) //7
                    
                {
                    isBullNow = true;
                }
            }
            return isBullNow;
        }
        private bool CheckLongEntrySignal(decimal currentPrice)
        {

            if (_currentRsi < RsiOpenLong) return true;
            return false;
            
            //return _currentFastEma > _currentSlowEma && // Быстрая EMA выше медленной
            //_previousFastEma <= _previousSlowEma && // Пересечение снизу вверх
            //currentPrice > _currentLongEma && // Цена выше длинной EMA
            //_currentRsi > 40 && _previousRsi < 30 && // RSI поднимается выше 40 с уровней ниже 30
            //currentPrice <= _currentLowerBand * 1.02m && // Цена около нижней полосы Боллинджера
            //_currentObv > _previousObv; // Объем растет
        }

        private bool CheckShortEntrySignal(decimal currentPrice)
        {
            if (_currentRsi > RsiOpenShort) return true;
            return false;
            //return _currentFastEma < _currentSlowEma && // Быстрая EMA ниже медленной
            //       _previousFastEma >= _previousSlowEma && // Пересечение сверху вниз
            //       currentPrice < _currentLongEma && // Цена ниже длинной EMA
            //       _currentRsi < 60 && _previousRsi > 70 && // RSI опускается ниже 60 с уровней выше 70
            //       currentPrice >= _currentUpperBand * 0.98m && // Цена около верхней полосы Боллинджера
            //       _currentObv < _previousObv; // Объем падает
        }

        /// <summary>
        /// Главный обработчик поиска входа
        /// </summary>
        /// <param name="candle"></param>
        private void CheckEntrySignals(ICandleMessage candle)
        {
            try
            {
                // Проверка, что все индикаторы сформированы
                if (!AreIndicatorsFormed())
                    return;

                // Получение текущей цены
                decimal currentPrice = candle.ClosePrice;
                decimal coinVolume = (0.95m)* TradeVolume / currentPrice;
                
                // Проверка фильтров
                //if (!CheckTimeFilter() || !CheckVolatilityFilter())
                //    return;

                // Проверка сигналов
                bool isLongSignal = CheckLongEntrySignal(currentPrice);
                bool isShortSignal = CheckShortEntrySignal(currentPrice);
                
                if (isLongSignal && 
                    //CheckGlobalTrendFilter(true) && 
                    TrendFilter(true, currentPrice) 
                    )
                {
                    OpenPosition(Sides.Buy, coinVolume, currentPrice);
                }
                else if (isShortSignal && 
                    //CheckGlobalTrendFilter(false) && 
                    TrendFilter(false, currentPrice)
                    )
                {
                    OpenPosition(Sides.Sell, coinVolume, currentPrice);
                }
            }
            catch (Exception ex)
            {
                LogErrorDetailed("Ошибка при проверке сигналов входа", ex);
            }
        }
    }
}