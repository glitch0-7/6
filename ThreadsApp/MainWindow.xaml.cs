using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;
using ThreadLab.Core;

namespace ThreadsApp
{
    /// <summary>
    /// Окно приложения задания 3: визуализация движения шариков — координаты
    /// вычисляются в потоках с разными приоритетами; окончание работы потоков
    /// синхронизируется Join и Barrier; состояние потоков анализируется
    /// по таймеру. Кнопки запускают демонстрации Lock/Monitor, мьютекса,
    /// семафора, Task, Parallel.For и Async/Await.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Масштаб перевода условных координат (0..100) в пиксели полотна (270)
        private const double Scale = 2.5;

        // Симуляция шариков (создается кнопкой «Запустить шарики»)
        private BallSimulator? simulator;
        // Нарисованные шарики: пары (шарик, эллипс)
        private readonly List<(MovingBall Ball, Ellipse Shape)> drawn = new();
        // Таймер перерисовки полотна (30 мс)
        private readonly DispatcherTimer drawTimer;
        // Таймер анализа состояния потоков (1000 мс)
        private readonly DispatcherTimer analysisTimer;

        public MainWindow()
        {
            InitializeComponent();
            // Таймер перерисовки: события Dispatcher — безопасное обновление UI
            drawTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            drawTimer.Tick += drawTimer_Tick;
            // Анализ состояния потоков раз в секунду
            analysisTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            analysisTimer.Tick += analysisTimer_Tick;
        }

        // Запуск симуляции: новые шарики, новые потоки, таймеры включены
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (simulator != null && simulator.IsRunning)
            {
                AppendLog(new[] { "Симуляция уже запущена" });
                return;
            }
            // Симуляция перезапускается «с нуля»
            simulator = new BallSimulator();
            simulator.Start();
            DrawBalls();
            drawTimer.Start();
            analysisTimer.Start();
            AppendLog(new[]
            {
                "Шарики запущены: координаты вычисляются в потоках",
                "Приоритеты: Highest (шаг 20 мс), Normal (40 мс), Lowest (80 мс)"
            });
        }

        // Остановка: согласованное окончание через Join и Barrier
        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            if (simulator == null || !simulator.IsRunning)
            {
                AppendLog(new[] { "Симуляция не запущена" });
                return;
            }
            drawTimer.Stop();
            analysisTimer.Stop();
            simulator.Stop();
            AppendLog(new[]
            {
                "Потоки остановлены: Barrier синхронизировал окончание,",
                "затем Join дождался завершения каждого потока"
            });
        }

        // Ручной анализ состояния потоков (кнопка Reports)
        private void reportButton_Click(object sender, RoutedEventArgs e)
        {
            if (simulator == null)
            {
                AppendLog(new[] { "Симуляция еще не запускалась" });
                return;
            }
            AppendLog(simulator.ReportThreads());
        }

        // Рисование шариков: эллипсы трех цветов по числу потоков
        private void DrawBalls()
        {
            // Без симуляции рисовать нечего
            if (simulator == null)
            {
                return;
            }
            pitch.Children.Clear();
            drawn.Clear();
            // Цвета по приоритетам в порядке создания шариков
            var colors = new[] { "Red", "SeaGreen", "RoyalBlue" };
            for (int i = 0; i < simulator.Balls.Count; i++)
            {
                MovingBall ball = simulator.Balls[i];
                Ellipse shape = new()
                {
                    Width = 20,
                    Height = 20,
                    Fill = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                            .ConvertFromString(colors[i]))
                };
                pitch.Children.Add(shape);
                drawn.Add((ball, shape));
            }
        }

        // Перерисовка: координаты шариков читаются из потоков
        private void drawTimer_Tick(object? sender, EventArgs e)
        {
            if (simulator == null)
            {
                return;
            }
            foreach ((MovingBall Ball, Ellipse Shape) item in drawn)
            {
                // Условные координаты масштабируются в пиксели полотна
                Canvas.SetLeft(item.Shape, item.Ball.X * Scale);
                Canvas.SetTop(item.Shape, item.Ball.Y * Scale);
            }
        }

        // Анализ состояния потоков каждые 1000 мс
        private void analysisTimer_Tick(object? sender, EventArgs e)
        {
            if (simulator != null && simulator.IsRunning)
            {
                AppendLog(simulator.ReportThreads());
            }
        }

        // 2. Демонстрация Lock и Monitor
        private async void lockDemoButton_Click(object sender, RoutedEventArgs e)
        {
            lockDemoButton.IsEnabled = false;
            try
            {
                List<string> lines = await Task.Run(() => ThreadingDemos.RunLockMonitorDemo());
                AppendLog(lines);
            }
            finally
            {
                lockDemoButton.IsEnabled = true;
            }
        }

        // 3. Демонстрация мьютекса
        private async void mutexDemoButton_Click(object sender, RoutedEventArgs e)
        {
            mutexDemoButton.IsEnabled = false;
            try
            {
                List<string> lines = await Task.Run(() => ThreadingDemos.RunMutexDemo());
                AppendLog(lines);
            }
            finally
            {
                mutexDemoButton.IsEnabled = true;
            }
        }

        // 3. Демонстрация семафора
        private async void semaphoreDemoButton_Click(object sender, RoutedEventArgs e)
        {
            semaphoreDemoButton.IsEnabled = false;
            try
            {
                List<string> lines = await Task.Run(() => ThreadingDemos.RunSemaphoreDemo());
                AppendLog(lines);
            }
            finally
            {
                semaphoreDemoButton.IsEnabled = true;
            }
        }

        // 4. Демонстрация Task (TPL)
        private async void taskDemoButton_Click(object sender, RoutedEventArgs e)
        {
            taskDemoButton.IsEnabled = false;
            try
            {
                AppendLog(await ParallelDemos.RunTaskDemoAsync());
            }
            finally
            {
                taskDemoButton.IsEnabled = true;
            }
        }

        // 5. Демонстрация Parallel.For
        private async void parallelForButton_Click(object sender, RoutedEventArgs e)
        {
            parallelForButton.IsEnabled = false;
            try
            {
                AppendLog(await ParallelDemos.RunParallelForDemoAsync());
            }
            finally
            {
                parallelForButton.IsEnabled = true;
            }
        }

        // 6. Демонстрация Async/Await
        private async void asyncDemoButton_Click(object sender, RoutedEventArgs e)
        {
            asyncDemoButton.IsEnabled = false;
            try
            {
                AppendLog(await ParallelDemos.RunAsyncAwaitDemoAsync());
            }
            finally
            {
                asyncDemoButton.IsEnabled = true;
            }
        }

        // Добавление строк лога с прокруткой списка к концу
        private void AppendLog(IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                logList.Items.Add(line);
            }
            if (logList.Items.Count > 0)
            {
                // Прокрутка к последней строке лога
                logList.ScrollIntoView(logList.Items[logList.Items.Count - 1]);
            }
        }

        // Уборка при закрытии окна: таймеры и потоки останавливаются
        protected override void OnClosed(EventArgs e)
        {
            drawTimer.Stop();
            analysisTimer.Stop();
            simulator?.Dispose();
            base.OnClosed(e);
        }
    }
}
