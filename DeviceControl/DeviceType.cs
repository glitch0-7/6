namespace DeviceControl
{
    /// <summary>
    /// Тип измерений, который выполняет эмулируемое устройство.
    /// Определяет, какой эмулятор запустит фабричный метод StartDevice.
    /// </summary>
    public enum DeviceType
    {
        /// <summary>Эмулируемое устройство измеряет длину.</summary>
        LENGTH,

        /// <summary>Эмулируемое устройство измеряет массу.</summary>
        MASS
    }
}
