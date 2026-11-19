using DeviceControl;
using MeasuringDevice;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadLab.Core;

namespace LabTests
{
    /// <summary>
    /// Проверки проекта DeviceControl: перечисление DeviceType, фабричный
    /// метод контроллера и блокирующие эмуляторы. В отличие от пятой
    /// лабораторной работы эмуляторы имитируют время получения измерения
    /// задержкой 0,5-1,5 секунды, поэтому вызов занимает реальное время.
    /// </summary>
    [TestClass]
    public class DeviceControlTests
    {
        [TestMethod]
        public void DeviceTypeHasLengthAndMass()
        {
            // Перечисление содержит ровно два значения: LENGTH и MASS
            DeviceType[] values = (DeviceType[])Enum.GetValues(typeof(DeviceType));
            Assert.AreEqual(2, values.Length);
            Assert.AreEqual(DeviceType.LENGTH, values[0]);
            Assert.AreEqual(DeviceType.MASS, values[1]);
        }

        [TestMethod]
        public void EmulatorsImplementIControllableDevice()
        {
            // Оба эмулятора совместимы с общим интерфейсом IControllableDevice
            FabrikamDevices.LengthMeasuringDevice length = new();
            ContosoDevices.MassMeasuringDevice mass = new();
            Assert.IsInstanceOfType(length, typeof(IControllableDevice));
            Assert.IsInstanceOfType(mass, typeof(IControllableDevice));
        }

        [TestMethod]
        public void StartDeviceReturnsControllerForBothTypes()
        {
            // Фабричный метод запускает устройство каждого типа
            DeviceController lengthController = DeviceController.StartDevice(DeviceType.LENGTH);
            Assert.IsNotNull(lengthController);
            Assert.IsInstanceOfType(lengthController, typeof(IDisposable));

            DeviceController massController = DeviceController.StartDevice(DeviceType.MASS);
            Assert.IsNotNull(massController);
            massController.Dispose();
        }

        [TestMethod]
        public void BlockedTakeMeasurementReturnsCorrectRange()
        {
            // Блокирующее измерение Fabrikam: пауза 0,5-1,5 с, значение 1..999
            DeviceController lengthController = DeviceController.StartDevice(DeviceType.LENGTH);
            int measure = lengthController.TakeMeasurement();
            Assert.IsTrue(measure >= 1 && measure <= 999,
                $"Измерение эмулятора Fabrikam должно быть в диапазоне 1..999, получено {measure}");

            DeviceController massController = DeviceController.StartDevice(DeviceType.MASS);
            int massMeasure = massController.TakeMeasurement();
            Assert.IsTrue(massMeasure >= 1 && massMeasure <= 4999,
                $"Измерение эмулятора Contoso должно быть в диапазоне 1..4999, получено {massMeasure}");
            massController.Dispose();
        }

        [TestMethod]
        public void StopDeviceAndDisposeAreSafe()
        {
            // Остановка и освобождение контроллера не выбрасывают исключений
            DeviceController controller = DeviceController.StartDevice(DeviceType.LENGTH);
            controller.StopDevice();
            controller.Dispose();
            // Остановка уже остановленного контроллера также безопасна
            controller.StopDevice();
        }
    }

    /// <summary>
    /// Проверки событий и «сердцебиения» (задания 1 и 2): событие
    /// NewMeasurementTaken после каждого измерения, событие HeartBeat с
    /// HeartBeatEventArgs, конструкторы MeasureMassDevice, кольцевой буфер
    /// и файл журнала. Эмулятор выдает измерение за 0,5-1,5 секунды,
    /// поэтому на первое измерение выделяется до 8 секунд ожидания.
    /// </summary>
    [TestClass]
    public class EventsAndHeartbeatTests
    {
        [TestMethod]
        public void NewMeasurementTakenIsRaised()
        {
            // Счетчик событий и первый аргумент события
            int raised = 0;
            MeasureMassDevice device = new(Units.Metric, 1000);
            device.NewMeasurementTaken += (o, e) =>
            {
                System.Threading.Interlocked.Increment(ref raised);
            };
            try
            {
                device.StartCollecting();
                // Дожидаемся хотя бы одного события (интервал эмулятора 0,5-1,5 с)
                DateTime deadline = DateTime.UtcNow.AddSeconds(8);
                while (DateTime.UtcNow < deadline && raised == 0)
                {
                    System.Threading.Thread.Sleep(100);
                }
                Assert.IsTrue(raised >= 1,
                    $"Событие NewMeasurementTaken не возникло за 8 секунд ({raised})");
                // Буфер получил измерение, о котором было объявлено событием
                Assert.IsTrue(device.GetRawData().Any(v => v != 0),
                    "Буфер содержит снятое измерение");
            }
            finally
            {
                device.StopCollecting();
                device.Dispose();
            }
        }

        [TestMethod]
        public void EventStopsAfterStopCollecting()
        {
            // После остановки новые события не возникают
            int raised = 0;
            MeasureMassDevice device = new(Units.Metric, 1000);
            device.NewMeasurementTaken += (o, e) =>
            {
                System.Threading.Interlocked.Increment(ref raised);
            };
            device.StartCollecting();
            DateTime deadline = DateTime.UtcNow.AddSeconds(8);
            while (DateTime.UtcNow < deadline && raised == 0)
            {
                System.Threading.Thread.Sleep(100);
            }
            // Не создается исключение в тесте, если событие не возникло:
            // проверку события выполняет предыдущий тест
            device.StopCollecting();
            // Измерение, начатое до остановки, может завершиться (до 1,5 с):
            // даем ему закончиться, прежде чем зафиксировать счетчик
            System.Threading.Thread.Sleep(2500);
            int raisedAfterStop = System.Threading.Volatile.Read(ref raised);
            // Два с половиной секунды запаса на новые события: их нет
            System.Threading.Thread.Sleep(2500);
            Assert.AreEqual(raisedAfterStop, raised,
                "После StopCollecting новые события не должны возникать");
            device.Dispose();
        }

        [TestMethod]
        public void HeartBeatRaisesWithTimeStamp()
        {
            // Событие «сердцебиения» несет собственные HeartBeatEventArgs
            MeasureMassDevice device = new(Units.Metric, 1000);
            DateTime stamp = DateTime.MinValue;
            var heartEvent = new ManualResetEvent(false);
            device.HeartBeat += (o, args) =>
            {
                // Гонок нет: событие возникает на UI-подобном сообщении прогресса
                stamp = args.TimeStamp;
                heartEvent.Set();
            };
            try
            {
                device.StartCollecting();
                // Интервал 1000 мс: дожидаемся первого «пульса»
                Assert.IsTrue(heartEvent.WaitOne(TimeSpan.FromSeconds(5)),
                    "Событие HeartBeat не возникло за 5 секунд");
                // Метка времени заполнена реальным моментом события
                Assert.AreEqual(DateTime.Now.Date, stamp.Date, "Метка «пульса» корректна");
            }
            finally
            {
                device.StopCollecting();
                device.Dispose();
            }
        }

        [TestMethod]
        public void HeartBeatIntervalIsConfigurable()
        {
            // Интервал «пульса» задается конструктором
            MeasureMassDevice device = new(Units.Metric, 500);
            Assert.AreEqual(500, device.HeartBeatInterval);
            // Цепной конструктор задает стандартные 1000 мс
            MeasureMassDevice chained = new(Units.Metric, "lr6_log_test.txt");
            Assert.AreEqual(1000, chained.HeartBeatInterval);
        }

        [TestMethod]
        public void ChainedCtorSetsLoggingFile()
        {
            // Цепной конструктор: имя файла доступно через интерфейс
            MeasureMassDevice device = new(Units.Metric, "lr6_log_chain.txt");
            Assert.AreEqual("lr6_log_chain.txt", device.LoggingFileName);
            Assert.AreEqual("lr6_log_chain.txt", device.GetLoggingFile());
        }

        [TestMethod]
        public void LoggingFileCollectsMeasurements()
        {
            // Файл журнала получает строку на каждое измерение
            string logFile = Path.Combine(Path.GetTempPath(),
                "lr6_logging_test_" + Guid.NewGuid().ToString("N") + ".txt");
            MeasureMassDevice device = new(Units.Metric, logFile);
            try
            {
                device.StartCollecting();
                DateTime deadline = DateTime.UtcNow.AddSeconds(8);
                while (DateTime.UtcNow < deadline && !device.GetRawData().Any(v => v != 0))
                {
                    System.Threading.Thread.Sleep(100);
                }
                device.StopCollecting();
                device.Dispose();
                // Файл журнала создан и содержит строки «Measurement - X»
                Assert.IsTrue(File.Exists(logFile), "Файл журнала создан");
                string content = File.ReadAllText(logFile);
                Assert.IsTrue(content.Contains("Measurement - "),
                    "В журнале есть строки измерений: " + content);
            }
            finally
            {
                // Уборка временного файла журнала
                if (File.Exists(logFile))
                {
                    File.Delete(logFile);
                }
            }
        }

        [TestMethod]
        public void RawDataBufferHasTenSlots()
        {
            // Кольцевой ровно на 10 ячеек; DataCaptured — тот же массив
            MeasureMassDevice device = new(Units.Metric, 1000);
            Assert.AreEqual(10, device.GetRawData().Length);
            Assert.AreSame(device.GetRawData(), device.DataCaptured);
            Assert.AreEqual(Units.Metric, device.UnitsToUse);
        }

        [TestMethod]
        public void UnitConversionsUseRecentMeasure()
        {
            // При метрическом устройстве ImperialValue пересчитывается:
            // имперское = метрическое * 2,2046 (после первого измерения)
            MeasureMassDevice device = new(Units.Metric, 1000);
            try
            {
                device.StartCollecting();
                DateTime deadline = DateTime.UtcNow.AddSeconds(8);
                while (DateTime.UtcNow < deadline && device.MostRecentMeasure == 0)
                {
                    System.Threading.Thread.Sleep(100);
                }
                device.StopCollecting();
                int m = device.MostRecentMeasure;
                Assert.IsTrue(m >= 1 && m <= 4999,
                    $"Измерение должно быть 1..4999, получено {m}");
                // Конверсии соответствуют коэффициентам пятой лабораторной
                Assert.AreEqual(m * 2.2046m, device.ImperialValue(), "кг -> фунты");
                Assert.AreEqual((decimal)m, device.MetricValue(), "метрическое значение как есть");
            }
            finally
            {
                device.Dispose();
            }
        }
    }

    /// <summary>
    /// Проверки многопоточного задания 3: симуляция шариков
    /// (потоки с приоритетами, Barrier и Join), Lock/Monitor, мьютекс,
    /// семафор, Task, Parallel.For и Async/Await.
    /// </summary>
    [TestClass]
    public class ThreadingTests
    {
        [TestMethod]
        public void BallSimulatorMovesBalls()
        {
            // Потоки шариков меняют координаты независимо
            BallSimulator simulator = new();
            simulator.Start();
            // За 400 мс سه шарика выполняют шаги движения
            System.Threading.Thread.Sleep(400);
            Assert.IsTrue(simulator.IsRunning);
            Assert.IsTrue(simulator.Balls.All(b => b.Steps > 0),
                "Все потоки шариков выполнили хотя бы один шаг");
            // Остановка: Barrier + Join завершают потоки
            simulator.Stop();
            Assert.IsFalse(simulator.IsRunning);
        }

        [TestMethod]
        public void ReportThreadsListsAllBalls()
        {
            // Отчет содержит строку заголовка и по строке на каждый шарик
            BallSimulator simulator = new();
            List<string> report = simulator.ReportThreads();
            Assert.AreEqual(4, report.Count, "Заголовок + три шарика");
            Assert.IsTrue(report[1].Contains("Highest"), "Первый шарик Highest");
            Assert.IsTrue(report[2].Contains("Normal"), "Второй шарик Normal");
            Assert.IsTrue(report[3].Contains("Lowest"), "Третий шарик Lowest");
        }

        [TestMethod]
        public void LockMonitorDemoCountsExactly()
        {
            // Блокировка lock/Monitor: итог точен, TryEnter отклоняет вход
            List<string> log = ThreadingDemos.RunLockMonitorDemo();
            Assert.IsTrue(log.Any(l => l.Contains("Итог под блокировкой: 100")),
                "Счетчик под блокировкой ровно 100");
            Assert.IsTrue(log.Any(l => l.Contains("TryEnter")),
                "Демонстрация TryEnter отражена в логе");
        }

        [TestMethod]
        public void MutexDemoHasSequentialOwners()
        {
            // Мьютекс: каждый из трех потоков владеет секцией по очереди
            List<string> log = ThreadingDemos.RunMutexDemo();
            Assert.AreEqual(3, log.Count(l => l.Contains("владеет мьютексом")),
                "Три последовательных владельца мьютекса");
        }

        [TestMethod]
        public void SemaphoreDemoLimitsInsideCount()
        {
            // Семафор: одновременно в секции не более 2 потоков
            List<string> log = ThreadingDemos.RunSemaphoreDemo();
            Assert.IsTrue(log.Any(l => l.Contains("(внутри 2)")), "Было 2 внутри секции");
            Assert.IsFalse(log.Any(l => l.Contains("(внутри 3)")),
                "Внутри секции никогда не было 3 потоков");
        }

        [TestMethod]
        public void TaskDemoComputesSum()
        {
            // Task: сумма квадратов посчитана задачей и доложена в лог
            List<string> log = ParallelDemos.RunTaskDemoAsync().GetAwaiter().GetResult();
            Assert.IsTrue(log.Any(l => l.Contains("42925")), "Сумма квадратов 1..50");
        }

        [TestMethod]
        public void ParallelForDemoSumsArray()
        {
            // Parallel.For: сумма 10 млн единиц точно 10000000
            List<string> log = ParallelDemos.RunParallelForDemoAsync().GetAwaiter().GetResult();
            Assert.IsTrue(log.Any(l => l.Contains("10000000")), "Сумма массива точна");
        }

        [TestMethod]
        public void AsyncAwaitDemoLoadsParts()
        {
            // Async/Await: две «части» загружены await'ами по порядку
            List<string> log = ParallelDemos.RunAsyncAwaitDemoAsync().GetAwaiter().GetResult();
            Assert.IsTrue(log.Any(l => l.Contains("часть 1")) &&
                          log.Any(l => l.Contains("часть 2")),
                "Обе части загружены");
        }
    }
}
