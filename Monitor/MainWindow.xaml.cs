using System.Windows;
using MeasuringDevice;

namespace Monitor
{
    /// <summary>
    /// Окно монитора: создает MeasureMassDevice и подписывается на его события
    /// NewMeasurementTaken и HeartBeat (задания 1 и 2).
    /// </summary>
    public partial class MainWindow : Window
    {
        // Тестируемое устройство (создается кнопкой Start Collecting)
        private MeasureMassDevice? device;

        // Делегат, ссылающийся на обработчик NewMeasurementTaken
        private EventHandler? newMeasurementTaken;

        /// <summary>Создание устройства, подписка на события и запуск сбора.</summary>
        private void startCollecting_Click(object sender, RoutedEventArgs e)
        {
            // Повторный запуск: если устройство уже есть, создания не требуется
            if (device == null)
            {
                device = new MeasureMassDevice(Units.Metric, 1000);

                // Делегат инициализируется методом handler'а (задание 1)
                newMeasurementTaken = new EventHandler(device_NewMeasurementTaken);
                // Подписка на событие устройства оператором +=
                device.NewMeasurementTaken += newMeasurementTaken;

                // Лямбда-выражение обрабатывает HeartBeat (задание 2):
                // метка времени выводится в подпись «сердцебиения»
                device.HeartBeat += (o, args) =>
                {
                    heartBeatTimeStamp.Content = "HeartBeat Timestamp: " + args.TimeStamp;
                };
            }
            // Повторный запуск: список Raw Data очищается под новый сбор
            rawDataValues.ItemsSource = null;
            device.StartCollecting();
        }

        /// <summary>Остановка устройства и отключение обработчика события.</summary>
        private void stopCollecting_Click(object sender, RoutedEventArgs e)
        {
            if (device != null)
            {
                device.StopCollecting();
                // Отключение делегата от события оператором -= (задание 1)
                device.NewMeasurementTaken -= newMeasurementTaken;
            }
        }

        /// <summary>Освобождение устройства: фоновый «пульс» останавливается.</summary>
        private void disposeObject_Click(object sender, RoutedEventArgs e)
        {
            if (device != null)
            {
                device.Dispose();
                device = null;
            }
        }

        /// <summary>
        /// Обработчик NewMeasurementTaken: обновляет окно свежими данными
        /// устройства (место хранения значений, величины и сырой буфер).
        /// </summary>
        private void device_NewMeasurementTaken(object? sender, EventArgs e)
        {
            if (device != null)
            {
                // Самое свежее измерение в собственных единицах
                mostRecentMeasureBox.Text = device.MostRecentMeasure.ToString();
                // Метрическое и имперское значение последнего измерения
                metricValueBox.Text = device.MetricValue().ToString();
                imperialValueBox.Text = device.ImperialValue().ToString();
                // Список необработанных данных обновляется двойной установкой:
                // сначала reset в null, затем привязка нового массива
                rawDataValues.ItemsSource = null;
                rawDataValues.ItemsSource = device.GetRawData();
            }
        }
    }
}
