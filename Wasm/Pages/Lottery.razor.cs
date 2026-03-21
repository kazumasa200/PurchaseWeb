using System.Collections.Concurrent;
using System.Diagnostics;

namespace PurchaseWeb.Wasm.Pages;

/// <summary>
/// 宝くじシミュレーター
/// Blazor WASM はシングルスレッドのため、Task.Run/ThreadLocal は使用不可。
/// バッチ処理 + await Task.Yield() でUIスレッドを解放しながらシミュレーションを継続する。
/// </summary>
public partial class Lottery : IDisposable
{
    private bool _simulationRunning = false;
    private long _dwCount = 0;
    private long _dwPayment = 0;
    private long _dwPrize = 0;
    private long _dwLoss = 0;
    private int _processingSpeed = 0;

    private CancellationTokenSource? _cts;
    private long _lastUpdateCount = 0;
    private readonly Stopwatch _speedStopwatch = new();

    private long[] _winCounts = new long[9];

    private ConcurrentBag<WinRecord> _winHistory = new();
    private List<WinRecord> _displayWinHistory = new();

    public class WinRecord
    {
        public long PurchaseCount { get; set; }
        public string Rank { get; set; } = "";
        public long Amount { get; set; }
        public long TotalBalance { get; set; }
    }

    private async Task StartSimulation()
    {
        _simulationRunning = true;
        _cts = new CancellationTokenSource();
        _speedStopwatch.Restart();
        _lastUpdateCount = _dwCount;

        var uiTimer = Stopwatch.StartNew();
        var rng = new Random();

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                // 1バッチ分のシミュレーションを同期実行
                RunBatch(rng, _cts.Token);

                // 200ms ごとに統計表示を更新
                if (uiTimer.ElapsedMilliseconds >= 200)
                {
                    UpdateDisplay();
                    StateHasChanged();
                    uiTimer.Restart();
                }

                // JSイベントループに制御を返し、ボタン操作などのUIイベントを処理可能にする
                await Task.Yield();
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            UpdateDisplay();
            _simulationRunning = false;
            StateHasChanged();
        }
    }

    /// <summary>1バッチ（10,000回）のくじ引きを同期実行して集計フィールドに加算する</summary>
    private void RunBatch(Random rng, CancellationToken ct)
    {
        long localCount = 0;
        long localPayment = 0;
        long localPrize = 0;
        var localWinCounts = new long[9];
        List<WinRecord>? localWinRecords = null;

        const int batchSize = 10000;

        for (int i = 0; i < batchSize && !ct.IsCancellationRequested; i++)
        {
            uint r = (uint)rng.Next(0, int.MaxValue);
            if (r >= 4290000000u) continue;

            localCount++;
            localPayment += 300;

            bool hasWon = false;
            string winRank = "";
            long winAmount = 0;

            if      (r % 10000000 == 0) { localPrize += 200000000; localWinCounts[0]++; hasWon = true; winRank = "1等"; winAmount = 200000000; }
            else if (r % 5000000  == 0) { localPrize += 100000000; localWinCounts[1]++; hasWon = true; winRank = "2等"; winAmount = 100000000; }
            else if (r % 500000   == 0) { localPrize +=   1000000; localWinCounts[2]++; hasWon = true; winRank = "3等"; winAmount =   1000000; }
            else if (r % 100000   == 0) { localPrize +=    500000; localWinCounts[3]++; hasWon = true; winRank = "4等"; winAmount =    500000; }
            else if (r % 1000     == 0) { localPrize +=     10000; localWinCounts[4]++; hasWon = true; winRank = "5等"; winAmount =     10000; }
            else if (r % 100      == 0) { localPrize +=      3000; localWinCounts[5]++; hasWon = true; winRank = "6等"; winAmount =      3000; }
            else if (r % 10       == 0) { localPrize +=       300; localWinCounts[6]++; hasWon = true; winRank = "7等"; winAmount =       300; }

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

            // 50万円以上の当選を履歴に記録
            if (hasWon && winAmount >= 500000)
            {
                localWinRecords ??= new List<WinRecord>();
                localWinRecords.Add(new WinRecord
                {
                    PurchaseCount = _dwCount + localCount,
                    Rank          = winRank,
                    Amount        = winAmount,
                    TotalBalance  = _dwLoss + localPrize - localPayment
                });
            }
        }

        // バッチ結果をグローバル集計に反映（シングルスレッドなのでインターロック不要）
        _dwCount   += localCount;
        _dwPayment += localPayment;
        _dwPrize   += localPrize;
        _dwLoss     = _dwPrize - _dwPayment;

        for (int i = 0; i < _winCounts.Length; i++)
            _winCounts[i] += localWinCounts[i];

        if (localWinRecords != null)
            foreach (var record in localWinRecords)
                _winHistory.Add(record);
    }

    /// <summary>処理速度と当選履歴の表示用データを更新する</summary>
    private void UpdateDisplay()
    {
        long currentCount = _dwCount;
        double elapsedSeconds = _speedStopwatch.ElapsedMilliseconds / 1000.0;

        if (elapsedSeconds > 0)
            _processingSpeed = (int)((currentCount - _lastUpdateCount) / elapsedSeconds);

        _lastUpdateCount = currentCount;
        _speedStopwatch.Restart();

        // 最新1,000件のみ表示（全件リスト化するとメモリ・描画コスト大）
        _displayWinHistory = _winHistory.OrderByDescending(w => w.PurchaseCount).Take(1000).ToList();
    }

    private void StopOrResetSimulation()
    {
        if (_simulationRunning)
        {
            // キャンセルのみ。_simulationRunning の false 化は StartSimulation の finally が担う
            _cts?.Cancel();
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
    }
}
