using DeviceControl;

namespace FabrikamDevices
{
    /// <summary>
    /// Эмулятор длиноизмеряющего устройства компании Fabrikam.
    /// Получение измерения имитируется паузой 0,5-1,5 секунды, поэтому
    /// метод GetLatestMeasure блокирует поток до готовности данных.
    /// </summary>
    public class LengthMeasuringDevice : IControllableDevice
    {
        // Генератор «измерений» и задержек эмулятора
        private readonly Random generator = new Random();
        // Признак «устройство запущено»
        private bool started;

        /// <summary>Запускает эмулятор.</summary>
        public void StartDevice()
        {
            started = true;
        }

        /// <summary>Останавливает эмулятор.</summary>
        public void StopDevice()
        {
            started = false;
        }

        /// <summary>
        /// Возвращает очередное измерение длины, имитируя время получения
        /// данных задержкой 0,5-1,5 секунды.
        /// </summary>
        public int GetLatestMeasure()
        {
            // Эмуляция ожидания готовности измерения блокирующим вызовом
            System.Threading.Thread.Sleep(generator.Next(500, 1500));
            // Остановленный эмулятор измерений не даёт
            return started ? generator.Next(1, 1000) : 0;
        }
    }
}
