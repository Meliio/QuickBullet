using System.Text;

namespace QuickBullet
{
    public class ConsoleManager
    {
        private readonly Checker _checker;

        public ConsoleManager(Checker checker)
        {
            _checker = checker;
        }

        public async Task StartUpdatingTitleAsync()
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            var checkerStats = new StringBuilder();

            while (true)
            {
                checkerStats
                    .Append((int)_checker.Parallelizer.Progress)
                    .Append("% Success: ")
                    .Append(_checker.Stats.Success)
                    .Append(" Custom: ")
                    .Append(_checker.Stats.Custom)
                    .Append(" Failure: ")
                    .Append(_checker.Stats.Failure)
                    .Append(" ToCheck: ")
                    .Append(_checker.Stats.ToCheck)
                    .Append(" Retry: ")
                    .Append(_checker.Stats.Retry)
                    .Append(" Ban: ")
                    .Append(_checker.Stats.Ban)
                    .Append(" Error: ")
                    .Append(_checker.Stats.Error)
                    .Append(" Bots: ")
                    .Append(_checker.Stats.DegreeOfParallelism)
                    .Append(" CPM: ")
                    .Append(_checker.Parallelizer.CPM)
                    .Append(" | ")
                    .Append(_checker.Parallelizer.Elapsed.ToString(@"hh\:mm\:ss"));

                Console.Title = checkerStats.ToString();

                checkerStats.Clear();

                await periodicTimer.WaitForNextTickAsync();
            }
        }

        public async Task StartListeningKeysAsync()
        {
            await Task.Delay(1000);

            while (true)
            {
                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.P:
                        await _checker.Pause();
                        break;
                    case ConsoleKey.R:
                        await _checker.Resume();
                        break;
                    case ConsoleKey.S:
                        await _checker.Stop();
                        break;
                    case ConsoleKey.A:
                        await _checker.Abort();
                        break;
                    case ConsoleKey.UpArrow:
                        await _checker.UpDegreeOfParallelism();
                        break;
                    case ConsoleKey.DownArrow:
                        await _checker.DownDegreeOfParallelism();
                        break;
                }
            }
        }
    }
}
