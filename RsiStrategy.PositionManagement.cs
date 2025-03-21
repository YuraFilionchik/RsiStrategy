namespace SuperStrategy
{
    using System;
    using StockSharp.Messages;
    using StockSharp.BusinessEntities;
    using DevExpress.XtraSpellChecker.Parser;
    using DevExpress.Xpf.Core;
    using Ecng.ComponentModel;
    using StockSharp.Algo.Indicators;
    using Ecng.Common;
    using System.Security.Cryptography;

    public partial class RsiStrategy
    {
        // Флаги и статусы
        private bool _isPositionOpened = false;
        private bool _isTrailingStopActivated = false;
        private decimal _currentStopLoss = 0;
        private decimal _currentTrailingStop = 0;
        private DateTimeOffset _positionOpenTime;
        private decimal _openPricePosition = 0;
        private decimal _currentPNL = 0;

        /// <summary>
        /// Открытие позиции
        /// </summary>
        private void OpenPosition(Sides side, decimal coinVolume, decimal price)
        {
            try
            {
                Order order;
                if (side == Sides.Buy)
                {
                    order = BuyMarket(coinVolume);
                }
                else
                {
                    order = SellMarket(coinVolume);
                }
                // Обновление состояния стратегии
                _openPricePosition = price;
                _positionOpenTime = CurrentTime;
                _isPositionOpened = true;
                _currentStopLoss = GetStopLossPriceByATR(_currentAtr, side, price);
                order.Comment = $"Открытие позиции. SL = {(100 * (_currentStopLoss - price).Abs() / price).ToString("N2")}% ({((_currentStopLoss - price).Abs() * coinVolume).ToString("N2")}USDT)";

            }
            catch (Exception ex)
            {
                LogErrorDetailed("Ошибка при открытии позиции", ex);
            }
        }

        #region Расчет уровней стоп-лосса и тейк-пр

        private decimal GetStopLossPriceByATR(decimal ATR, Sides side, decimal price)
        {
            return side == Sides.Buy
            ? price - (ATR * StopLossMultiplier)
                    : price + (ATR * StopLossMultiplier);
        }
        #endregion

        /// <summary>
        /// Управление позицией
        /// </summary>
        private void ManagePosition(ICandleMessage candle)
        {
            try
            {
                // Получение текущей цены
                decimal currentPrice = candle.ClosePrice;

                //Если позиции нет, сбрасываем состояние
                decimal position = GetCurrentPosition();
                int minutesLimit = 180;
                if ((CurrentTime - _positionOpenTime).TotalMinutes > minutesLimit)
                {
                    CloseCurrentPosition($"По времени. {minutesLimit}min");
                    //LogInfo($"ClosePosition - {CurrentTime}");
                    return;
                }

                if (CheckStopLossForCurrentPosition(currentPrice)) return;

                if (CheckTakeProfitForCurrentPosition(currentPrice)) return;
                
            }
            catch (Exception ex)
            {
                LogErrorDetailed("Ошибка при управлении позицией", ex);
            }
        }

        /// <summary>
        /// Закрытие позиции
        /// </summary>

        void CloseCurrentPosition(string msg)
        {
            var value = GetCurrentPosition();
            Order order;
            if (value > 0)
                order = SellMarket(value);
            else order = BuyMarket(value.Abs());
            order.Comment = $"Закрытие позиции. {msg}";
            _isPositionOpened = false;
        }

        private decimal GetCurrentPosition()
        {

            if (Position == 0)
            {
                var p = Positions?.FirstOrDefault()?.CurrentValue;
                if (p != null)
                    return (decimal)p;

            }

            return Position;
        }

        /// <summary>
        /// Check and Close current position if SL
        /// </summary>
        /// <param name="currentPrice"></param>
        private bool CheckStopLossForCurrentPosition(decimal currentPrice)
        {
            var position = GetCurrentPosition();
            //LONG
            if (position > 0 && currentPrice <= _currentStopLoss)
            {
                CloseCurrentPosition($"(LONG) - Stop loss, PnL={_currentPNL}");
                _slCount++;
                return true;
            }
            //SHORT
            else if (position < 0 && currentPrice >= _currentStopLoss)
            {
                CloseCurrentPosition($"(SHORT) - Stop loss, PnL={_currentPNL}");
                _slCount++;
                return true ;
            }

            return false;
        }

        private bool CheckTakeProfitForCurrentPosition(decimal price)
        {
            
            if (_currentPNL < 0) return false;
            if (_currentRsi < RsiCloseShort && Position < 0)
                {
                //SHORT
                CloseCurrentPosition($"(SHORT) - По сигналу, PnL={_currentPNL}");
                return true;
                }
            else if ((_currentRsi > RsiCloseLong && Position > 0))
            {
                //LONG
                CloseCurrentPosition($"(LONG) - По сигналу, PnL={_currentPNL}");
                return true;
            }
                return false;
        }

        private decimal CalculateCurrentPnL(decimal currentPrice)
        {
            var position = GetCurrentPosition();
            if (position == 0 || currentPrice == 0 || _openPricePosition == 0) return 0;
            var deltaPrice = position > 0 ? currentPrice - _openPricePosition : _openPricePosition - currentPrice;
            return deltaPrice * position;
        }
    }

}