using DeviceControl;

namespace MeasuringDevice
{
    /// <summary>
    /// Устройство, измеряющее длину. Метрические единицы — миллиметры,
    /// имперские — дюймы (1 дюйм = 25,4 мм; 1 мм = 0,03937 дюйма).
    /// Наследует общую логику от абстрактного MeasureDataDevice.
    /// </summary>
    public class MeasureLengthDevice : MeasureDataDevice
    {
        /// <summary>
        /// Конструктор принимает единицы измерения, задаваемые пользователем,
        /// и определяет тип эмулируемого устройства — измерение длины.
        /// </summary>
        /// <param name="unit">Единицы измерения устройства (Metric или Imperial).</param>
        public MeasureLengthDevice(Units unit)
        {
            unitsToUse = unit;
            measurementType = DeviceType.LENGTH;
            // Стандартный интервал «сердцебиения» — одна секунда
            heartBeatIntervalTime = 1000;
        }

        /// <summary>
        /// Переводит последнее измерение в метрические единицы (мм).
        /// </summary>
        public override decimal MetricValue()
        {
            // В метрическом режиме данные уже метрические
            return unitsToUse == Units.Metric ? mostRecentMeasure : mostRecentMeasure * 25.4m;
        }

        /// <summary>
        /// Переводит последнее измерение в имперские единицы (дюймы).
        /// </summary>
        public override decimal ImperialValue()
        {
            // В имперском режиме данные уже имперские
            return unitsToUse == Units.Imperial ? mostRecentMeasure : mostRecentMeasure * 0.03937m;
        }
    }
}
