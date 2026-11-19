namespace MeasuringDevice
{
    /// <summary>
    /// Контракт измеряющего устройства (расширен по сравнению с пятой
    /// лабораторной работой: добавлены ведение файла журнала и свойства
    /// состояния устройства).
    /// </summary>
    public interface IMeasuringDevice
    {
        /// <summary>
        /// Преобразует накопленные устройством данные в метрическое значение.
        /// </summary>
        /// <returns>Последнее измерение устройства в метрических единицах.</returns>
        decimal MetricValue();

        /// <summary>
        /// Преобразует накопленные устройством данные в имперское значение.
        /// </summary>
        /// <returns>Последнее измерение устройства в имперских единицах.</returns>
        decimal ImperialValue();

        /// <summary>Запускает измеряющее устройство.</summary>
        void StartCollecting();

        /// <summary>Останавливает измеряющее устройство.</summary>
        void StopCollecting();

        /// <summary>
        /// Даёт доступ к необработанным данным устройства в его текущих единицах.
        /// </summary>
        /// <returns>Необработанные данные устройства.</returns>
        int[] GetRawData();

        /// <summary>
        /// Возвращает имя файла журнала измерений устройства.
        /// </summary>
        /// <returns>Имя файла журнала.</returns>
        string GetLoggingFile();

        /// <summary>Система единиц, выбранная пользователем устройства.</summary>
        Units UnitsToUse { get; }

        /// <summary>Массив измерений, снятых устройством (кольцевой буфер).</summary>
        int[] DataCaptured { get; }

        /// <summary>Самое свежее измерение устройства.</summary>
        int MostRecentMeasure { get; }

        /// <summary>
        /// Имя файла журнала измерений. При смене имени текущий файл
        /// закрывается, следующим измерением открывается новый файл.
        /// </summary>
        string LoggingFileName { get; set; }
    }
}
