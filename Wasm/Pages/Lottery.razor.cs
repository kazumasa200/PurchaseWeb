using System.Collections.Concurrent;
using System.Diagnostics;

namespace PurchaseWeb.Wasm.Pages;

public partial class Lottery
{
    private bool _simulationRunning = false;
    private long _dwCount = 0;
    private long _dwPayment = 0;
    private long _dwPrize = 0;
    private long _dwLoss = 0;
    private int _processingSpeed = 0;
    private bool _tableLoading = false;

    private ThreadLocal<Random> _threadLocalRandom = new ThreadLocal<Random>(() =>
        new Random(Guid.NewGuid().GetHashCode()));

    private CancellationTokenSource _cts;
    private Timer _uiUpdateTimer;

    private long[] _winCounts = new long[9];

    private ConcurrentBag<WinRecord> _winHistory = new ConcurrentBag<WinRecord>();
    private List<WinRecord> _displayWinHistory = new List<WinRecord>();

    public class WinRecord
    {
        public long PurchaseCount { get; set; }
        public string Rank { get; set; }
        public long Amount { get; set; }
        public long TotalBalance { get; set; }
    }

    private long _lastUpdateCount = 0;
    private Stopwatch _speedStopwatch = new Stopwatch();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _uiUpdateTimer = new Timer(UpdateUI, null, Timeout.Infinite, Timeout.Infinite);
    }

    private async Task StartSimulation()
    {
        _simulationRunning = true;
        _cts = new CancellationTokenSource();
        _speedStopwatch.Restart();
        _lastUpdateCount = _dwCount;

        _uiUpdateTimer.Change(0, 200);

        try
        {
            if (Environment.ProcessorCount % 2 == 0)
            {
                await Task.WhenAll(Enumerable.Range(0, Environment.ProcessorCount / 2).Select(_ =>
                    Task.Run(() => SimulationWorker(_cts.Token))));
            }
            else
            {
                await Task.WhenAll(Enumerable.Range(0, Environment.ProcessorCount).Select(_ =>
                    Task.Run(() => SimulationWorker(_cts.Token))));
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _speedStopwatch.Stop();
            _uiUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _simulationRunning = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void SimulationWorker(CancellationToken cancellationToken)
    {
        var random = _threadLocalRandom.Value;

        long localCount = 0;
        long localPayment = 0;
        long localPrize = 0;
        long[] localWinCounts = new long[9];
        List<WinRecord> localWinRecords = new List<WinRecord>();

        const int batchSize = 10000;

        while (!cancellationToken.IsCancellationRequested)
        {
            for (int i = 0; i < batchSize && !cancellationToken.IsCancellationRequested; i++)
            {
                uint r = (uint)random.Next(0, int.MaxValue);
                if (r >= 4290000000) continue;

                localCount++;
                localPayment += 300;

                bool hasWon = false;
                string winRank = "";
                long winAmount = 0;

                if (r % 10000000 == 0) { localPrize += 200000000; localWinCounts[0]++; hasWon = true; winRank = "1等"; winAmount = 200000000; }
                else if (r % 5000000 == 0) { localPrize += 100000000; localWinCounts[1]++; hasWon = true; winRank = "2等"; winAmount = 100000000; }
                else if (r % 500000 == 0) { localPrize += 1000000; localWinCounts[2]++; hasWon = true; winRank = "3等"; winAmount = 1000000; }
                else if (r % 100000 == 0) { localPrize += 500000; localWinCounts[3]++; hasWon = true; winRank = "4等"; winAmount = 500000; }
                else if (r % 1000 == 0) { localPrize += 10000; localWinCounts[4]++; hasWon = true; winRank = "5等"; winAmount = 10000; }
                else if (r % 100 == 0) { localPrize += 3000; localWinCounts[5]++; hasWon = true; winRank = "6等"; winAmount = 3000; }
                else if (r % 10 == 0) { localPrize += 300; localWinCounts[6]++; hasWon = true; winRank = "7等"; winAmount = 300; }

                if (r % 2500000 == 0)
                {
                    localPrize += 50000000; localWinCounts[7]++; hasWon = true;
                    if (string.IsNullOrEmpty(winRank)) { winRank = "前後賞"; winAmount = 50000000; }
                }

                if (r % 50000 == 0 && (r / 50000) % 100 != 0)
                {
                    localPrize += 100000; localWinCounts[8]++; hasWon = true;
                    if (string.IsNullOrEmpty(winRank)) { winRank = "組違い"; winAmount = 100000; }
                }

                if (hasWon && winAmount >= 500000)
                {
                    localWinRecords.Add(new WinRecord
                    {
                        PurchaseCount = Interlocked.Read(ref _dwCount) + localCount,
                        Rank = winRank,
                        Amount = winAmount,
                        TotalBalance = Interlocked.Read(ref _dwLoss) + localPrize - localPayment
                    });
                }
            }

            Interlocked.Add(ref _dwCount, localCount);
            Interlocked.Add(ref _dwPayment, localPayment);
            Interlocked.Add(ref _dwPrize, localPrize);
            Interlocked.Exchange(ref _dwLoss, _dwPrize - _dwPayment);

            for (int i = 0; i < _winCounts.Length; i++)
                Interlocked.Add(ref _winCounts[i], localWinCounts[i]);

            foreach (var record in localWinRecords)
                _winHistory.Add(record);

            localCount = 0;
            localPayment = 0;
            localPrize = 0;
            Array.Clear(localWinCounts, 0, localWinCounts.Length);
            localWinRecords.Clear();
        }
    }

    private async void UpdateUI(object state)
    {
        try
        {
            long currentCount = Interlocked.Read(ref _dwCount);
            long countDiff = currentCount - _lastUpdateCount;
            double elapsedSeconds = _speedStopwatch.ElapsedMilliseconds / 1000.0;

            if (elapsedSeconds > 0)
                _processingSpeed = (int)(countDiff / elapsedSeconds);

            _lastUpdateCount = currentCount;
            _speedStopwatch.Restart();

            _tableLoading = true;
            await InvokeAsync(() =>
            {
                _displayWinHistory = _winHistory.OrderByDescending(w => w.PurchaseCount).Take(1000).ToList();
                _tableLoading = false;
                StateHasChanged();
            });
        }
        catch (Exception)
        {
        }
    }

    private void StopOrResetSimulation()
    {
        if (_simulationRunning)
        {
            _cts?.Cancel();
            _uiUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _simulationRunning = false;
        }
        else
        {
            _dwCount = 0;
            _dwPayment = 0;
            _dwPrize = 0;
            _dwLoss = 0;
            _processingSpeed = 0;

            for (int i = 0; i < _winCounts.Length; i++)
                _winCounts[i] = 0;

            ClearHistory();
        }
    }

    private void ClearHistory()
    {
        _winHistory = new ConcurrentBag<WinRecord>();
        _displayWinHistory.Clear();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _uiUpdateTimer?.Dispose();
        _threadLocalRandom?.Dispose();
    }
}
