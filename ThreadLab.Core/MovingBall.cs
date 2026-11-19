namespace ThreadLab.Core
{
    /// <summary>
    /// Перемещающийся объект визуализации (задание 3, пункт 1). Координаты
    /// шарика вычисляются в собственном потоке с заданным приоритетом:
    /// потоки с разным приоритетом получают разный темп обновления координат.
    /// Доступ к координатам защищен блокировкой (состояние объекта).
    /// </summary>
    public class MovingBall
    {
        // Границы прямоугольного поля движения (условные единицы)
        public const double Bound = 100.0;

        // Блокировка состояния координат (публичные свойства читают из UI)
        private readonly object stateLock = new object();
        // Координаты и скорость шарика
        private double x;
        private double y;
        private double vx;
        private double vy;
        // Рабочий поток шарика и признак «работает»
        private Thread? worker;
        private volatile bool running;

        /// <summary>Имя шарика для отчета о потоках.</summary>
        public string Name { get; }

        /// <summary>Приоритет потока, в котором движется шарик.</summary>
        public ThreadPriority Priority { get; }

        /// <summary>Пауза между шагами движения (мс) — темп зависит от приоритета.</summary>
        public int IntervalMs { get; }

        /// <summary>Число выполненных шагов движения.</summary>
        public int Steps { get; private set; }

        /// <summary>Текущая координата X.</summary>
        public double X
        {
            get
            {
                lock (stateLock)
                {
                    return x;
                }
            }
        }

        /// <summary>Текущая координата Y.</summary>
        public double Y
        {
            get
            {
                lock (stateLock)
                {
                    return y;
                }
            }
        }

        /// <summary>Рабочий поток шарика (для отчета о состоянии потоков).</summary>
        public Thread? WorkerThread => worker;

        /// <summary>
        /// Создает шарик: имя, приоритет потока и интервал шага задают темп.
        /// </summary>
        public MovingBall(string name, ThreadPriority priority, int intervalMs,
            int seed = 0)
        {
            Name = name;
            Priority = priority;
            IntervalMs = intervalMs;
            // Начальная позиция и скорость: разнообразие через зерно генератора
            Random random = seed == 0 ? new Random() : new Random(seed);
            x = random.NextDouble() * (Bound - 10);
            y = random.NextDouble() * (Bound - 10);
            vx = 1.5 + random.NextDouble() * 3.0;
            vy = 1.5 + random.NextDouble() * 3.0;
        }

        /// <summary>
        /// Запускает поток движения. Общий барьер finishGate используется всеми
        /// потоками шариков и стоп-потоком для согласованного окончания (Join и Barrier).
        /// </summary>
        public void Start(Barrier finishGate)
        {
            // Повторный запуск недопущен: поток уже работает
            if (worker != null && worker.IsAlive)
            {
                return;
            }
            running = true;
            // Поток фоновый: приложение завершается независимо от него
            worker = new Thread(Work)
            {
                Name = "Movement thread: " + Name,
                IsBackground = true,
                // Приоритет потока задает темп обновления координат
                Priority = Priority
            };
            // Барьер передается потоку параметром Start(object)
            worker.Start(finishGate);
        }

        /// <summary>Просит поток завершиться на следующем шаге.</summary>
        public void RequestStop()
        {
            running = false;
        }

        /// <summary>Ожидает завершения рабочего потока (Join).</summary>
        public bool Join(int timeoutMs)
        {
            // Join возвращает true, когда поток полностью завершен
            return worker != null && worker.Join(timeoutMs);
        }

        // Основной цикл движения в собственном потоке
        private void Work(object? obj)
        {
            // Точка входа получает барьер, переданный потоком Start(object);
            // параметр null невозможен, поэтому используется проверка!
            Barrier finishGate = (Barrier)obj!;
            try
            {
                // Шаги повторяются, пока объект не остановлен
                while (running)
                {
                    // Пауза между шагами: приоритет потока отражается в темпе
                    Thread.Sleep(IntervalMs);
                    // Шаг вычисления новых координат под блокировкой
                    lock (stateLock)
                    {
                        Step();
                        Steps++;
                    }
                }
            }
            finally
            {
                // Барьер-синхронизация окончания: дожидаемся остальных
                // участников, прежде чем поток завершится в этом шаге
                finishGate.SignalAndWait(new TimeSpan(0, 0, 10));
            }
        }

        // Один шаг: смещение и отражение от границ поля
        private void Step()
        {
            // Смещение по скорости
            x += vx;
            y += vy;
            // Отражение от левой/правой границы
            if (x < 0)
            {
                x = 0;
                vx = -vx;
            }
            if (x > Bound - 10)
            {
                x = Bound - 10;
                vx = -vx;
            }
            // Отражение от верхней/нижней границы
            if (y < 0)
            {
                y = 0;
                vy = -vy;
            }
            if (y > Bound - 10)
            {
                y = Bound - 10;
                vy = -vy;
            }
        }
    }
}
