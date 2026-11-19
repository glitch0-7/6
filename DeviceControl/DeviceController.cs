namespace DeviceControl
{
    /// <summary>
    /// Контроллер эмулируемого измеряющего устройства. Физического устройства
    /// в лабораторной нет, поэтому этот класс связывает приложение с эмуляторами
    /// Fabrikam (длина) и Contoso (масса) через интерфейс IControllableDevice.
    /// </summary>
    public class DeviceController : IDisposable
    {
        // Управляемое устройство; null, пока устройство не запущено фабричным методом
        private IControllableDevice? device;

        /// <summary>
        /// Фабричный метод: создаёт и запускает новый экземпляр устройства.
        /// </summary>
        /// <param name="MeasurementType">Тип устройства: MASS или LENGTH.</param>
        /// <returns>Контроллер с запущенным устройством.</returns>
        public static DeviceController StartDevice(DeviceType MeasurementType)
        {
            // Контроллер создаётся всегда, устройство подбирается по типу
            DeviceController controller = new DeviceController();
            switch (MeasurementType)
            {
                case DeviceType.LENGTH:
                    // Длину измеряет эмулятор Fabrikam
                    controller.device = new FabrikamDevices.LengthMeasuringDevice();
                    break;
                case DeviceType.MASS:
                    // Массу измеряет эмулятор Contoso
                    controller.device = new ContosoDevices.MassMeasuringDevice();
                    break;
            }
            // Устройство запускается сразу после привязки к контроллеру
            if (controller.device != null)
            {
                controller.device.StartDevice();
            }

            return controller;
        }

        /// <summary>Останавливает управляемое устройство.</summary>
        public void StopDevice()
        {
            // Защита: остановка контроллера без запущенного устройства безопасна
            device?.StopDevice();
        }

        /// <summary>
        /// Заставляет управляемое устройство снять измерение. Метод блокирует
        /// поток до готовности измерения: эмуляторы имитируют задержку получения
        /// данных, поэтому DoWork избыточных измерений не накапливает.
        /// </summary>
        /// <returns>Измерение, снятое устройством.</returns>
        public int TakeMeasurement()
        {
            // Запрашиваем измерение у устройства напрямую
            return device != null ? device.GetLatestMeasure() : 0;
        }

        /// <summary>Освобождает контроллер (эмуляторы не требуют очистки).</summary>
        public void Dispose()
        {
        }
    }
}
