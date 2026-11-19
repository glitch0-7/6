namespace MeasuringDevice
{
    /// <summary>
    /// Единицы измерения, в которых пользователь хочет получать показания
    /// устройства. Задаётся при создании MeasureLengthDevice/MeasureMassDevice.
    /// </summary>
    public enum Units
    {
        /// <summary>Метрические единицы (мм; кг).</summary>
        Metric,

        /// <summary>Имперские единицы (дюймы; фунты).</summary>
        Imperial
    }
}
