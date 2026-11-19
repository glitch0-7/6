namespace ThreadLab.Core
{
    /// <summary>
    /// Демонстрации блокировок потоков (задание 3, пункты 2 и 3):
    /// блокировка оператором lock и методами класса Monitor (в том числе
    /// TryEnter), а также мьютекс и семафор.
    /// </summary>
    public static class ThreadingDemos
    {
        /// <summary>
        /// Пункт 2: блокировка потока оператором lock и методами класса
        /// Monitor. Четыре потока увеличивают общий счётчик в блокировке;
        /// затем показывается отказ Monitor.TryEnter на захваченной блокировке.
        /// </summary>
        public static List<string> RunLockMonitorDemo()
        {
            List<string> log = new();
            // Объект блокировки счетчика
            object sync = new object();
            // Общий счётчик: без блокировки итог был бы невоспроизводим
            int counter = 0;
            log.Add("Lock/Monitor: 4 потока x 25 инкрементов под блокировкой");
            var threads = new List<Thread>();
            for (int t = 0; t < 4; t++)
            {
                int threadNo = t + 1;
                threads.Add(new Thread(() =>
                {
                    for (int k = 0; k < 25; k++)
                    {
                        // Флаг входа должен быть false перед каждым захватом
                        bool entered = false;
                        try
                        {
                            // Явный вход Monitor.Enter с фиксацией входа
                            Monitor.Enter(sync, ref entered);
                            counter++;
                            // Короткая работа внутри блокировки
                            if (k == 24)
                            {
                                lock (log)
                                {
                                    log.Add($"  Поток {threadNo} закончил 25 инкрементов");
                                }
                            }
                        }
                        finally
                        {
                            // Выход только если вход состоялся
                            if (entered)
                            {
                                Monitor.Exit(sync);
                            }
                        }
                    }
                }));
            }
            foreach (Thread thread in threads)
            {
                thread.Start();
            }
            // Join: дожидаемся каждого потока перед проверкой суммы
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            log.Add($"  Итог под блокировкой: {counter} (должно быть ровно 100)");

            // TryEnter: попытка входа в захваченную блокировку не блокирует,
            // а возвращает false (возможность «пройти мимо» занятой секции)
            lock (sync)
            {
                if (Monitor.TryEnter(sync, TimeSpan.FromMilliseconds(50)))
                {
                    log.Add("  TryEnter: неожиданно вошли во вторую блокировку");
                    Monitor.Exit(sync);
                }
                else
                {
                    log.Add("  TryEnter: блокировка занята — вход отклонен (ожидали)");
                }
            }
            return log;
        }

        /// <summary>
        /// Пункт 3, мьютекс: серия потоков (по одной машинке) захватывает
        /// мьютекс для «критической секции» — в секции может быть один поток.
        /// </summary>
        public static List<string> RunMutexDemo()
        {
            List<string> log = new();
            log.Add("Мьютекс: 3 потока по очереди владеют критической секцией");
            // Именованный мьютекс, изначально свободный
            using Mutex mutex = new(false, "ThreadLabCore_Mutex_" + Guid.NewGuid().ToString("N"));
            for (int i = 1; i <= 3; i++)
            {
                int threadNo = i;
                var thread = new Thread(() =>
                {
                    try
                    {
                        // Захват мьютекса (владение — ровно у одного потока)
                        mutex.WaitOne();
                        log.Add($"  Поток {threadNo} владеет мьютексом");
                        // «Работа» в критической секции
                        Thread.Sleep(120);
                        log.Add($"  Поток {threadNo} освобождает мьютекс");
                    }
                    finally
                    {
                        // Выход из мьютекса обязателен в finally
                        mutex.ReleaseMutex();
                    }
                });
                thread.Start();
                // Очередность получит: порядок владения воспроизводим в логе
                thread.Join();
            }
            return log;
        }

        /// <summary>
        /// Пункт 3, семафор: до 2 потоков одновременно «в очереди»,
        /// остальные ждут освобождения слота семафора.
        /// </summary>
        public static List<string> RunSemaphoreDemo()
        {
            List<string> log = new();
            log.Add("Семафор: 5 потоков, одновременно в секции не более 2");
            // Семлфор с 2 свободными слотами и максимумом 2
            using Semaphore semaphore = new(2, 2);
            int inside = 0;                     // сколько потоков внутри секции
            object sync = new object();         // блокировка счетчика inside
            var threads = new List<Thread>();
            for (int i = 1; i <= 5; i++)
            {
                int threadNo = i;
                threads.Add(new Thread(() =>
                {
                    // Ожидание свободного слота семафора
                    semaphore.WaitOne();
                    lock (sync)
                    {
                        inside++;
                        log.Add($"  Поток {threadNo} входит (внутри {inside})");
                    }
                    // «Работа» внутри ограниченной секции
                    Thread.Sleep(200);
                    lock (sync)
                    {
                        inside--;
                        log.Add($"  Поток {threadNo} выходит (внутри {inside})");
                    }
                    // Освобождение слота для следующего потока
                    semaphore.Release();
                }));
            }
            foreach (Thread thread in threads)
            {
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            log.Add("  Семафор: одновременных владельцев не больше 2 (контроль по счетчику)");
            return log;
        }
    }
}
