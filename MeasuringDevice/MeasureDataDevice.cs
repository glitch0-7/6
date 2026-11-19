using System.ComponentModel;
using DeviceControl;

namespace MeasuringDevice
{
    /// <summary>
    /// Абстрактный базовый класс измеряющих устройств (реализация задания 1 и 2).
    /// Общая логика: буфер последних 10 измерений, опрос эмулятора компонентом
    /// BackgroundWorker в фоновом потоке, событие NewMeasurementTaken после
    /// каждого измерения, «сердцебиение» (HeartBeat) на лямбда-выражениях с
    /// настраиваемым интервалом, ведение файла журнала измерений. Преобразование
    /// единиц оставлено наследникам — коэффициенты у длины и массы разные,
    /// поэтому методы MetricValue/ImperialValue объявлены абстрактными.
    /// </summary>
    public abstract class MeasureDataDevice : IEventEnabledMeasuringDevice, IDisposable
    {
        /// <summary>
        /// Преобразует накопленные устройством данные в метрическое значение.
        /// Реализация зависит от типа устройства, поэтому метод абстрактный.
        /// </summary>
        public abstract decimal MetricValue();

        /// <summary>
        /// Преобразует накопленные устройством данные в имперское значение.
        /// Реализация зависит от типа устройства, поэтому метод абстрактный.
        /// </summary>
        public abstract decimal ImperialValue();

        // Единицы измерения, выбранные пользователем при создании устройства
        protected Units unitsToUse;
        // Кольцевой буфер: последние 10 измерений, старые перезаписываются
        protected int[] dataCaptured = new int[10];
        // Самое свежее измерение устройства
        protected int mostRecentMeasure;
        // Контроллер эмулируемого устройства (null, когда устройство остановлено)
        protected DeviceController? controller;
        // Тип измерений устройства (LENGTH или MASS); задаётся в конструкторе наследника
        protected DeviceType measurementType;
        // Признак «объект освобождён»: фоновые циклы прекращают работу
        private volatile bool disposed;
        // Писатель в файл журнала измерений; null, пока журнал не ведется
        private StreamWriter? loggingFileWriter;
        // Имя файла журнала
        private string? loggingFileName;

        /// <summary>
        /// Событие возникает после того, как новое измерение записано
        /// в буфер и журнал (задание 1).
        /// </summary>
        public event EventHandler? NewMeasurementTaken;

        /// <summary>Событие «сердцебиение» устройства (задание 2).</summary>
        public event HeartBeatEventHandler? HeartBeat;

        // Интервал «сердцебиения» в миллисекундах (значение по умолчанию — 1 с)
        protected int heartBeatIntervalTime = 1000;

        /// <summary>Интервал «сердцебиения» в миллисекундах.</summary>
        public int HeartBeatInterval
        {
            get => heartBeatIntervalTime;
            // Закрытый сеттер: интервал задает конструктор класса-наследника
            private set => heartBeatIntervalTime = value;
        }

        // Фоновый сборщик измерений (задание 1)
        private BackgroundWorker? dataCollector;
        // Фоновый таймер-«сердцебиение» (задание 2)
        private BackgroundWorker? heartBeatTimer;

        /// <summary>Запускает сбор измерений и «сердцебиение».</summary>
        public void StartCollecting()
        {
            // Контроллер создаётся фабричным методом DeviceController
            controller = DeviceController.StartDevice(measurementType);
            // Журнал измерений открывается при запуске устройства
            EnsureLoggingWriter();
            // Запускается «сердцебиение» по лямбда-выражениям (задание 2)
            StartHeartBeat();
            // Запускается фоновый опрос эмулятора (задание 1)
            GetMeasurements();
        }

        /// <summary>Останавливает сбор измерений («сердцебиение» продолжается,
        /// пока объект не освобожден — так задано в методичке).</summary>
        public void StopCollecting()
        {
            // Фоновый сборщик останавливается отменой
            if (dataCollector != null)
            {
                dataCollector.CancelAsync();
                dataCollector = null;
            }
            // Контроллер эмулятора останавливается и отвязывается,
            // чтобы повторный запуск создал свежее устройство
            if (controller != null)
            {
                controller.StopDevice();
                controller = null;
            }
        }

        /// <summary>Возвращает накопленные устройством данные.</summary>
        public int[] GetRawData()
        {
            return dataCaptured;
        }

        /// <summary>Свойство интерфейса: тот же кольцевой буфер.</summary>
        public int[] DataCaptured => dataCaptured;

        /// <summary>Свойство интерфейса: самое свежее измерение.</summary>
        public int MostRecentMeasure => mostRecentMeasure;

        /// <summary>Свойство интерфейса: выбранная пользователем система единиц.</summary>
        public Units UnitsToUse => unitsToUse;

        /// <summary>Свойство интерфейса: имя файла журнала измерений.</summary>
        public string GetLoggingFile()
        {
            return LoggingFileName;
        }

        /// <summary>
        /// Имя файла журнала. При смене имени текущий файл закрывается,
        /// новый откроется следующим измерением.
        /// </summary>
        public string LoggingFileName
        {
            // Пока журнал не задан, возврат пустой строки
            get => loggingFileName ?? string.Empty;
            set
            {
                // Смена имени файла журнала закрывает текущий файл
                if (loggingFileName != value)
                {
                    loggingFileName = value;
                    if (loggingFileWriter != null)
                    {
                        loggingFileWriter.Close();
                        loggingFileWriter = null;
                    }
                }
            }
        }

        // Открывает файл журнала, если имя файла задано и файл ещё не открыт
        private void EnsureLoggingWriter()
        {
            if (loggingFileWriter == null && !string.IsNullOrEmpty(loggingFileName))
            {
                loggingFileWriter = new StreamWriter(loggingFileName, append: false);
            }
        }

        // Настраивает и запускает фоновый опрос эмулятора (задание 1)
        private void GetMeasurements()
        {
            // Новый сборщик измерений под каждый запуск
            dataCollector = new BackgroundWorker();
            // Разрешаем отмену фонового опроса
            dataCollector.WorkerSupportsCancellation = true;
            // Разрешаем уведомления о прогрессе (каждое новое измерение)
            dataCollector.WorkerReportsProgress = true;
            // Подписка на событие DoWork: сбор и хранение измерений
            dataCollector.DoWork += new DoWorkEventHandler(dataCollector_DoWork);
            // Подписка на событие ProgressChanged: вызов события NewMeasurementTaken
            dataCollector.ProgressChanged +=
                new ProgressChangedEventHandler(dataCollector_ProgressChanged);
            // Запуск фонового опроса
            dataCollector.RunWorkerAsync();
        }

        // Фоновый метод DoWork: снимает измерения и пишет их в буфер и журнал
        private void dataCollector_DoWork(object? sender, DoWorkEventArgs e)
        {
            // Буфер пересоздаётся при каждом запуске устройства
            dataCaptured = new int[10];
            // Индекс текущей ячейки кольцевого буфера
            int i = 0;
            // Цикл продолжается, пока сбор не отменен
            while (dataCollector != null && !dataCollector.CancellationPending)
            {
                // TakeMeasurement блокирует поток до готовности измерения
                dataCaptured[i] = controller != null ? controller.TakeMeasurement() : dataCaptured[i];
                // Свежее измерение запоминается отдельно
                mostRecentMeasure = dataCaptured[i];
                // Освобожденный объект прекращает сбор немедленно
                if (disposed)
                {
                    break;
                }
                // В журнал пишется строка вида «Measurement - значение»
                EnsureLoggingWriter();
                if (loggingFileWriter != null)
                {
                    loggingFileWriter.WriteLine($"Measurement - {mostRecentMeasure}");
                }
                // ProgressChanged объявляет UI о новом измерении
                dataCollector.ReportProgress(0);
                // Переход к следующей ячейке кольцевого буфера
                i++;
                if (i > 9)
                {
                    // После десятого измерения буфер начинается с нуля,
                    // перезаписывая самое старое значение
                    i = 0;
                }
            }
        }

        // При каждом уведомлении о прогрессе поднимается событие NewMeasurementTaken
        private void dataCollector_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            // Событие для подписчиков (стенд обновляет окно в этом обработчике)
            OnNewMeasurementTaken();
        }

        // Поднимает событие NewMeasurementTaken (проверка подписчиков обязательна:
        // без подписчиков делегат события равен null и вызов выбросит исключение)
        protected virtual void OnNewMeasurementTaken()
        {
            // Проверка подписчиков обязательна: без подписчиков делегат
            // события равен null и вызов выбросил бы исключение.
            // По заданию данные события «пустые» (EventArgs.Empty — аналог null).
            NewMeasurementTaken?.Invoke(this, EventArgs.Empty);
        }

        // Настраивает и запускает фоновое «сердцебиение» (задание 2):
        // обработчики DoWork и ProgressChanged заданы лямбда-выражениями
        private void StartHeartBeat()
        {
            // Повторный запуск устройства не должен создавать второй «пульс»
            if (heartBeatTimer != null)
            {
                return;
            }
            // Фоновый таймер «сердцебиения»
            heartBeatTimer = new BackgroundWorker();
            // Разрешаем отмену при освобождении объекта
            heartBeatTimer.WorkerSupportsCancellation = true;
            // Разрешаем уведомления о «пульсе»
            heartBeatTimer.WorkerReportsProgress = true;
            // Лямбда-выражение обрабатывает DoWork: пауза и уведомление о прогрессе
            heartBeatTimer.DoWork += (o, args) =>
            {
                while (heartBeatTimer != null && !heartBeatTimer.CancellationPending)
                {
                    // Пауза между «пульсами» — интервал HeartBeatInterval
                    Thread.Sleep(HeartBeatInterval);
                    // Освобожденный объект прекращает «пульс»
                    if (disposed)
                    {
                        break;
                    }
                    // Уведомление о прогрессе триггерит ProgressChanged
                    heartBeatTimer.ReportProgress(0);
                }
            };
            // Лямбда-выражение обрабатывает ProgressChanged: событие HeartBeat
            heartBeatTimer.ProgressChanged += (o, args) =>
            {
                // Событие «сердцебиение» со своей HeartBeatEventArgs
                OnHeartBeat();
            };
            // Запуск фонового «сердцебиения»
            heartBeatTimer.RunWorkerAsync();
        }

        // Поднимает событие HeartBeat с меткой времени в HeartBeatEventArgs
        protected virtual void OnHeartBeat()
        {
            // Проверка подписчиков: без них делегат события равен null
            HeartBeat?.Invoke(this, new HeartBeatEventArgs());
        }

        /// <summary>
        /// Освобождает устройство: останавливает фоновые потоки, контроллер
        /// и закрывает файл журнала.
        /// </summary>
        public void Dispose()
        {
            // Фоновые циклы завершаются по этому флагу
            disposed = true;
            // Остановка и освобождение сборщика измерений
            if (dataCollector != null)
            {
                dataCollector.CancelAsync();
                dataCollector.Dispose();
                dataCollector = null;
            }
            // Остановка и освобождение таймера «сердцебиения»
            if (heartBeatTimer != null)
            {
                heartBeatTimer.CancelAsync();
                heartBeatTimer.Dispose();
                heartBeatTimer = null;
            }
            // Остановка контроллера эмулятора
            if (controller != null)
            {
                controller.StopDevice();
                controller = null;
            }
            // Закрытие файла журнала
            if (loggingFileWriter != null)
            {
                loggingFileWriter.Close();
                loggingFileWriter = null;
            }
        }
    }
}
