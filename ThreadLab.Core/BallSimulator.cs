namespace ThreadLab.Core
{
    /// <summary>
    /// Симуляция движения шариков (задание 3, пункт 1): координаты объектов
    /// вычисляются в разных потоках с разными приоритетами; окончание работы
    /// потоков синхронизируется Join и Barrier; состояние потоков анализируется
    /// через заданные промежутки времени (метод ReportThreads).
    /// </summary>
    public class BallSimulator : IDisposable
    {
        // Шарики симуляции
        private readonly List<MovingBall> balls = new();
        // Барьер согласования окончания: потоки шаров + стоп-поток
        private Barrier? finishGate;

        /// <summary>Все шарики симуляции (только чтение).</summary>
        public IReadOnlyList<MovingBall> Balls => balls;

        /// <summary>Признак «симуляция запущена и потоки живы».</summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Создает симуляцию: три шарика с приоритетами потоков Highest,
        /// Normal и Lowest (разный темп обновления координат).
        /// </summary>
        public BallSimulator()
        {
            // Высокий приоритет — самый частый шаг движения
            balls.Add(new MovingBall("Шарик (Highest)", ThreadPriority.Highest, 20, seed: 11));
            // Средний приоритет — умеренный темп
            balls.Add(new MovingBall("Шарик (Normal)", ThreadPriority.Normal, 40, seed: 22));
            // Низкий приоритет — редкий шаг движения
            balls.Add(new MovingBall("Шарик (Lowest)", ThreadPriority.Lowest, 80, seed: 33));
        }

        /// <summary>Запускает потоки движения шариков.</summary>
        public void Start()
        {
            // Повторный запуск: потоки уже движутся
            if (IsRunning)
            {
                return;
            }
            // Барьер: 3 потока шариков + поток остановки
            finishGate = new Barrier(balls.Count + 1);
            // Каждый шарик участвует в барьере окончания
            foreach (MovingBall ball in balls)
            {
                ball.Start(finishGate);
            }
            IsRunning = true;
        }

        /// <summary>
        /// Останавливает симуляцию: сначала все потоки просят завершиться,
        /// затем главный поток входит в общий Barrier (согласованное
        /// окончание) и Join'ом дожидается каждого потока.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            // Все потоки узнают о завершении на очередном шаге
            foreach (MovingBall ball in balls)
            {
                ball.RequestStop();
            }
            // Сторона главного потока входит в Barrier — согласованное
            // окончание: главный поток возвращается, когда все потоки доехали
            // до барьера и вышли из него
            finishGate?.SignalAndWait(new TimeSpan(0, 0, 10));
            // Join: точный старт и конец каждого потока под контролем
            foreach (MovingBall ball in balls)
            {
                ball.Join(5000);
            }
            IsRunning = false;
        }

        /// <summary>
        /// Отчет о состоянии потоков шариков: сумма сведений (имя, жив, 
        /// приоритет, число шагов) для периодического анализа.
        /// </summary>
        public List<string> ReportThreads()
        {
            List<string> report = new();
            report.Add($"Анализ потоков ({DateTime.Now:HH:mm:ss}):");
            foreach (MovingBall ball in balls)
            {
                Thread? thread = ball.WorkerThread;
                // Живой поток и его приоритет — основная диагностика
                string state = thread != null &&
                    thread.ThreadState != ThreadState.Stopped ? "жив" : "остановлен";
                report.Add($"  {ball.Name}: поток {state}, приоритет {ball.Priority}, шагов {ball.Steps}");
            }
            return report;
        }

        /// <summary>Освобождение: остановка потоков при необходимости.</summary>
        public void Dispose()
        {
            if (IsRunning)
            {
                Stop();
            }
        }
    }
}
