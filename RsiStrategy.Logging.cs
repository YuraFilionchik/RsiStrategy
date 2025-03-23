namespace SuperStrategy
{
    using System;
    using Ecng.Logging;
    using StockSharp.Algo;
    using StockSharp.Logging;
    using StockSharp.Xaml;

    public partial class RsiStrategy
    {
        private readonly bool LogToFile = false;
        private readonly bool LogToConsole = false;
        // Логирование

        /// <summary>
        /// Инициализация логирования
        /// </summary>
        private void InitializeLogging()
        {
            try
            {
                // Получаем существующий LogManager
                var logManager = Ecng.Logging.LogManager.Instance;
                //logManager.LocalTimeZone = TimeZoneInfo.Local;
                if (IsOptimizing()) logManager.Application.LogLevel = Ecng.Logging.LogLevels.Warning;
                
                // Добавляем слушателя для файла
                if (LogToFile)
                {
                    // Создаем лог-файл с датой и временем
                    var logFileName = $"{this.Name}_strategy_log_{DateTime.Now:yyyy-MM-dd_HH-mm}.txt";
                    var fileLogListener = new Ecng.Logging.FileLogListener(logFileName);
                    logManager.Listeners.Add(fileLogListener);
                    LogInfo($"Логирование в файл инициализировано: {logFileName}");
                }

                // Добавляем GUI слушателя
                // Для доступа к монитору логов в Designer
                var ConsoleListener = new Ecng.Logging.ConsoleLogListener();
                if (LogToConsole)
                {
                    logManager.Listeners.Add(ConsoleListener);
                    LogInfo($"Логирование в консоль инициализировано");
                }

                LogLevel = Ecng.Logging.LogLevels.Warning;
                
                // Добавляем источник (стратегию)
                logManager.Sources.Add(this);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации логирования: {ex.Message}");
            }

        }

        private void StopLogging()
        {
            try
            {
                var logManager = Ecng.Logging.LogManager.Instance;
                logManager.Sources.Clear();
                logManager.Listeners.Clear();
                //logManager.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации логирования: {ex.Message}");
            }
        }

        /// <summary>
        /// Расширенное логирование ошибок с полным исключением
        /// </summary>
        private void LogErrorDetailed(string message, Exception exception)
        {
            LogError($"{message}: {exception.Message}");
            LogError($"StackTrace: {exception.StackTrace}");

            if (exception.InnerException != null)
                LogError($"Inner: {exception.InnerException.Message}");
        }


        private void LogInfo(string message)
        {
            if (IsOptimizationMode) return;
            base.LogInfo(message);
        }
    }
        
}