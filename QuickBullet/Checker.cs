using LiteDB;
using QuickBullet.Models;
using RuriLib.Parallelization;
using Spectre.Console;

namespace QuickBullet
{
    public class Checker
    {
        public Parallelizer<BotInput, bool> Parallelizer { get; }
        public CheckerStats Stats { get; }

        private readonly Record _record;

        public Checker(Parallelizer<BotInput, bool> parallelizer, CheckerStats checkerStats, Record record)
        {
            Parallelizer = parallelizer;
            Stats = checkerStats;
            _record = record;
        }

        public async Task StartAsync()
        {
            await Parallelizer.Start();

            AnsiConsole.MarkupLine($"started at {Parallelizer.StartTime}");

            _ = StartUpdatingRecordAsync();

            await Parallelizer.WaitCompletion();

            AnsiConsole.MarkupLine($"[red3]aborted at {Parallelizer.EndTime}[/]");
        }

        public async Task UpDegreeOfParallelism()
        {
            if (Stats.DegreeOfParallelism >= Parallelizer.MaxDegreeOfParallelism)
            {
                return;
            }

            Stats.DegreeOfParallelism++;

            await Parallelizer.ChangeDegreeOfParallelism(Stats.DegreeOfParallelism);
        }

        public async Task DownDegreeOfParallelism()
        {
            if (Stats.DegreeOfParallelism <= 1)
            {
                return;
            }

            Stats.DegreeOfParallelism--;

            await Parallelizer.ChangeDegreeOfParallelism(Stats.DegreeOfParallelism);
        }

        public async Task Pause()
        {
            await Parallelizer.Pause();

            AnsiConsole.MarkupLine($"[darkorange]pause at {Parallelizer.StartTime}[/]");
        }

        public async Task Resume()
        {
            await Parallelizer.Resume();

            AnsiConsole.MarkupLine($"[greenyellow]resume at {Parallelizer.StartTime}[/]");

            _ = StartUpdatingRecordAsync();
        }

        public async Task Stop() => await Parallelizer.Stop();

        public async Task Abort() => await Parallelizer.Abort();

        private async Task StartUpdatingRecordAsync()
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            using var database = new LiteDatabase("Kraken.db");

            var collection = database.GetCollection<Record>("records");

            while (Parallelizer.Status == ParallelizerStatus.Running)
            {
                _record.Progress = Stats.Progress;

                collection.Update(_record);

                await periodicTimer.WaitForNextTickAsync();
            }
        }
    }
}
