namespace MeasuringDevice
{
    /// <summary>
    /// Данные события «сердцебиение» устройства. Пользовательский класс
    /// EventArgs может нести любые свойства — обработчик получает сведения
    /// о событии в момент его возникновения (метка времени «пульса»).
    /// </summary>
    public class HeartBeatEventArgs : EventArgs
    {
        /// <summary>Момент времени, когда был зафиксирован «пульс».</summary>
        public DateTime TimeStamp { get; }

        /// <summary>
        /// Конструктор фиксирует текущую дату и время (метка «пульса»).
        /// </summary>
        public HeartBeatEventArgs()
        {
            TimeStamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Пользовательский тип делегата для события HeartBeat: ссылается на
    /// метод без возвращаемого значения с параметрами sender и args.
    /// </summary>
    public delegate void HeartBeatEventHandler(object sender, HeartBeatEventArgs args);
}
