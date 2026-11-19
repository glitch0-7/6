namespace ThreadLab.Core
{
    /// <summary>
    /// Демонстрации новой парадигмы многопоточности (задание 3, пункты 4-6):
    /// задачи Task (TPL), многопоточный цикл Parallel.For и асинхронный
    /// метод на async/await.
    /// </summary>
    public static class ParallelDemos
    {
        /// <summary>
        /// Пункт 4, Task: вычисление выполняется отдельной задачей.
        /// Плюс TPL: задача попадает в очередь пула потоков сама, результат
        /// забирается аргументом ожидания, исключения собираются в
        /// AggregateException, ручного создания Thread не нужно.
        /// </summary>
        public static async Task<List<string>> RunTaskDemoAsync()
        {
            List<string> log = new();
            log.Add("Task (TPL): вычисление в фоновом пуле потоков");
            // Задача складывает квадраты чисел 1..50 в отдельном потоке
            Task<long> sumTask = Task.Run(() =>
            {
                long sum = 0;
                for (int i = 1; i <= 50; i++)
                {
                    sum += (long)i * i;
                }
                return sum;
            });
            // Await: результат забирается после завершения задачи
            long sum = await sumTask;
            log.Add($"  Сумма квадратов 1..50 = {sum} (ожидалось 42925)");
            log.Add("  Задача завершена без ручного создания Thread");
            return log;
        }

        /// <summary>
        /// Пункт 5, Parallel.For: многопоточный цикл — подсчет суммы
        /// элементов массива; итерации распределяются по всем ядрам.
        /// </summary>
        public static async Task<List<string>> RunParallelForDemoAsync()
        {
            List<string> log = new();
            log.Add("Parallel.For: сумма элементов массива на всех ядрах");
            // Массив: 10 млн единиц
            int[] data = new int[10_000_000];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 1;
            }
            // Атомарное сложение: итерации идут на разных ядрах
            long total = 0;
            Parallel.For(0, data.Length, i =>
            {
                Interlocked.Add(ref total, data[i]);
            });
            log.Add($"  Сумма 10 млн единиц = {total} (ожидалось 10000000)");
            log.Add($"  Логических ядер для Parallel.For: {Environment.ProcessorCount}");
            return log;
        }

        /// <summary>
        /// Пункт 6, async/await: асинхронный метод с await — вызывающий код
        /// не блокирует поток, продолжение выполняется после завершения.
        /// </summary>
        public static async Task<List<string>> RunAsyncAwaitDemoAsync()
        {
            List<string> log = new();
            log.Add("Async/Await: асинхронная «загрузка» двух частей");
            // Две await-операции поочередно: паузы без блокировки потока
            string part1 = await LoadPartAsync("часть 1", 150);
            string part2 = await LoadPartAsync("часть 2", 150);
            log.Add($"  Загружены: {part1}, {part2}");
            log.Add("  Порядок сохранен await'ами, поток не блокировался");
            return log;
        }

        // Асинхронная «загрузка части»: пауза через await Task.Delay
        private static async Task<string> LoadPartAsync(string name, int delayMs)
        {
            // Пауза без блокировки потока (в отличие от Thread.Sleep)
            await Task.Delay(delayMs);
            return name;
        }
    }
}
