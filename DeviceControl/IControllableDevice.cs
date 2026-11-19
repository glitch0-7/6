namespace DeviceControl
{
    /// <summary>
    /// Общий контракт управляемого устройства. Оба эмулятора
    /// (FabrikamDevices.LengthMeasuringDevice и ContosoDevices.MassMeasuringDevice)
    /// реализуют этот интерфейс, поэтому контроллер работает с ними одинаково.
    /// </summary>
    public interface IControllableDevice
    {
        /// <summary>Запускает устройство.</summary>
        void StartDevice();

        /// <summary>Останавливает устройство.</summary>
        void StopDevice();

        /// <summary>
        /// Возвращает очередное измерение устройства. Вызов блокирует поток
        /// до готовности измерения (эмулятор имитирует время получения данных).
        /// </summary>
        /// <returns>Измерение в собственных единицах устройства.</returns>
        int GetLatestMeasure();
    }
}
