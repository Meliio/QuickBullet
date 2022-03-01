namespace QuickBullet.Models
{
    public class CheckerStats
    {
        public int Progress { get => _skip + _checked; }
        public int DegreeOfParallelism { get; set; }
        public int ToCheck => _toCheck;
        public int Success => _success;
        public int Custom => _custom;
        public int Failure => _failure;
        public int Retry => _retry;
        public int Ban => _ban;
        public int Error => _error;

        private readonly int _skip;
        private readonly Dictionary<string, Action> _incrementFunctions;

        private int _toCheck;
        private int _success;
        private int _custom;
        private int _failure;
        private int _retry;
        private int _ban;
        private int _error;
        private int _checked;

        public CheckerStats(int skip)
        {
            _skip = skip;
            _incrementFunctions = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
            {
                { "toCheck", IncrementToCheck },
                { "success", IncrementSuccess },
                { "failure", IncrementFailure },
                { "retry", IncrementRetry },
                { "ban", IncrementBan },
                { "error", IncrementError },
                { "checked", IncrementChecked }
            };
        }

        public void Increment(string botStatus)
        {
            if (_incrementFunctions.ContainsKey(botStatus))
            {
                _incrementFunctions[botStatus].Invoke();
            }
            else
            {
                IncrementCustom();
            }
        }

        private void IncrementToCheck() => Interlocked.Increment(ref _toCheck);
        private void IncrementSuccess() => Interlocked.Increment(ref _success);
        private void IncrementCustom() => Interlocked.Increment(ref _custom);
        private void IncrementFailure() => Interlocked.Increment(ref _failure);
        private void IncrementRetry() => Interlocked.Increment(ref _retry);
        private void IncrementBan() => Interlocked.Increment(ref _ban);
        private void IncrementError() => Interlocked.Increment(ref _error);
        private void IncrementChecked() => Interlocked.Increment(ref _checked);
    }
}
