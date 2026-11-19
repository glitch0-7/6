using DeviceControl;

namespace MeasuringDevice
{
    /// <summary>
    /// Устройство, измеряющее массу. Метрические единицы — килограммы,
    /// имперские — фунты (1 фунт = 0,4536 кг; 1 кг = 2,2046 фунта).
    /// Наследует общую логику от абстрактного MeasureDataDevice и позволяет
    /// задать интервал «сердцебиения» и файл журнала через конструкторы.
    /// </summary>
    public class MeasureMassDevice : MeasureDataDevice
    {
        /// <summary>
        /// Конструктор принимает единицы измерения, интервал «сердцебиения»
        /// и определяет тип эмулируемого устройства — измерение массы.
        /// </summary>
        /// <param name="unit">Единицы измерения устройства (Metric или Imperial).</param>
        /// <param name="heartBeatInterval">Интервал «сердцебиения», мс.</param>
        public MeasureMassDevice(Units unit, int heartBeatInterval)
        {
            unitsToUse = unit;
            measurementType = DeviceType.MASS;
            // Интервал «сердцебиения» хранится в защищённом члене базового класса
            heartBeatIntervalTime = heartBeatInterval;
        }

        /// <summary>
        /// Цепной конструктор: имя файла журнала плюс стандартный интервал
        /// «сердцебиения» 1000 мс. Вызывает первый конструктор неявно.
        /// </summary>
        /// <param name="deviceUnits">Единицы измерения устройства.</param>
        /// <param name="logFileName">Имя файла журнала измерений.</param>
        public MeasureMassDevice(Units deviceUnits, string logFileName)
            : this(deviceUnits, 1000)
        {
            // Стандартный интервал «пульса» — 1000 мс; остаётся задать журнал
            LoggingFileName = logFileName;
        }

        /// <summary>
        /// Переводит последнее измерение в метрические единицы (кг).
        /// </summary>
        public override decimal MetricValue()
        {
            // В метрическом режиме данные уже метрические
            return unitsToUse == Units.Metric ? mostRecentMeasure : mostRecentMeasure * 0.4536m;
        }

        /// <summary>
        /// Переводит последнее измерение в имперские единицы (фунты).
        /// </summary>
        public override decimal ImperialValue()
        {
            // В имперском режиме данные уже имперские
            return unitsToUse == Units.Imperial ? mostRecentMeasure : mostRecentMeasure * 2.2046m;
        }
    }
}
