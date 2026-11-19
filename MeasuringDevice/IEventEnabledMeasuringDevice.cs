namespace MeasuringDevice
{
    /// <summary>
    /// Событийная надстройка над контрактом измеряющего устройства.
    /// Новый интерфейс расширяет старый, поэтому весь прежний код,
    /// работающий через IMeasuringDevice, сохраняет совместимость,
    /// а новый код получает доступ к событиям устройства.
    /// </summary>
    public interface IEventEnabledMeasuringDevice : IMeasuringDevice
    {
        /// <summary>
        /// Событие, возникающее после того, как устройство сняло новое
        /// измерение, записало его в буфер и журнал.
        /// </summary>
        event EventHandler? NewMeasurementTaken;

        /// <summary>Событие «сердцебиение» устройства (раз в CurrentInterval).</summary>
        event HeartBeatEventHandler? HeartBeat;

        /// <summary>Интервал «сердцебиения» в миллисекундах (задаётся в конструкторе).</summary>
        int HeartBeatInterval { get; }
    }
}
