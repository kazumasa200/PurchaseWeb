using System.Collections.Concurrent;
using System.Diagnostics;

namespace PurchaseWeb.Pages;

public partial class Lottery
{
    private bool _simulationRunning = false;
    private long _dwCount = 0;
    private long _dwPayment = 0;
    private long _dwPrize = 0;
    private long _dwLoss = 0;
    private int _processingSpeed = 0;
    private bool _tableLoading = false;

    // スレッドセーフな乱数生成器
    private ThreadLocal<Random> _threadLocalRandom = new ThreadLocal<Random>(() =>
        new Random(Guid.NewGuid().GetHashCode()));

    private CancellationTokenSource _cts;
    private Timer _uiUpdateTimer;

    // 当選回数カウント用配列（1等、2等、...、前後賞、組違い）
    private long[] _winCounts = new long[9];

    // 当選履歴を保存するスレッドセーフなコレクション
    private ConcurrentBag<WinRecord> _winHistory = new ConcurrentBag<WinRecord>();

    // 表示用の当選履歴
    private List<WinRecord> _displayWinHistory = new List<WinRecord>();

    // 当選記録クラス
    public class WinRecord
    {
        public long PurchaseCount { get; set; }  // 購入回数
        public string Rank { get; set; }         // 等級
        public long Amount { get; set; }         // 当選金額
        public long TotalBalance { get; set; }   // 累計損益
    }

    // 最適化のための一時変数
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

        // UIの更新タイマーを開始（200ミリ秒ごとに更新）
        _uiUpdateTimer.Change(0, 200);

        try
        {
            if (Environment.ProcessorCount % 2 == 0)
            {
                // 複数のタスクを並列実行
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
            // キャンセルされた場合は何もしない
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
        // ローカルな乱数生成器を取得
        var random = _threadLocalRandom.Value;

        // ローカル変数で集計して後でまとめて加算（スレッド間の競合を減らす）
        long localCount = 0;
        long localPayment = 0;
        long localPrize = 0;
        long[] localWinCounts = new long[9];
        List<WinRecord> localWinRecords = new List<WinRecord>();

        const int batchSize = 10000; // 一度に処理する回数

        while (!cancellationToken.IsCancellationRequested)
        {
            for (int i = 0; i < batchSize && !cancellationToken.IsCancellationRequested; i++)
            {
                // 乱数生成（最適化）
                uint r = (uint)random.Next(0, int.MaxValue);
                if (r >= 4290000000) continue;

                localCount++;
                localPayment += 300;

                bool hasWon = false;
                string winRank = "";
                long winAmount = 0;

                // 当選判定（最適化：if-elseで排他的に処理）
                if (r % 10000000 == 0) // 1等 200,000,000円
                {
                    localPrize += 200000000;
                    localWinCounts[0]++;
                    hasWon = true;
                    winRank = "1等";
                    winAmount = 200000000;
                }
                else if (r % 5000000 == 0) // 2等 100,000,000円
                {
                    localPrize += 100000000;
                    localWinCounts[1]++;
                    hasWon = true;
                    winRank = "2等";
                    winAmount = 100000000;
                }
                else if (r % 500000 == 0) // 3等 1,000,000円
                {
                    localPrize += 1000000;
                    localWinCounts[2]++;
                    hasWon = true;
                    winRank = "3等";
                    winAmount = 1000000;
                }
                else if (r % 100000 == 0) // 4等 500,000円
                {
                    localPrize += 500000;
                    localWinCounts[3]++;
                    hasWon = true;
                    winRank = "4等";
                    winAmount = 500000;
                }
                else if (r % 1000 == 0) // 5等 10,000円
                {
                    localPrize += 10000;
                    localWinCounts[4]++;
                    hasWon = true;
                    winRank = "5等";
                    winAmount = 10000;
                }
                else if (r % 100 == 0) // 6等 3,000円
                {
                    localPrize += 3000;
                    localWinCounts[5]++;
                    hasWon = true;
                    winRank = "6等";
                    winAmount = 3000;
                }
                else if (r % 10 == 0) // 7等 300円
                {
                    localPrize += 300;
                    localWinCounts[6]++;
                    hasWon = true;
                    winRank = "7等";
                    winAmount = 300;
                }

                // 特殊な当選は別途判定
                if (r % 2500000 == 0) // 1等前後 50,000,000円
                {
                    localPrize += 50000000;
                    localWinCounts[7]++;
                    hasWon = true;

                    // 既に他の当選がある場合は記録を上書きしない
                    if (string.IsNullOrEmpty(winRank))
                    {
                        winRank = "前後賞";
                        winAmount = 50000000;
                    }
                }

                if (r % 50000 == 0 && (r / 50000) % 100 != 0) // 1等組違い 100,000円
                {
                    localPrize += 100000;
                    localWinCounts[8]++;
                    hasWon = true;

                    // 既に他の当選がある場合は記録を上書きしない
                    if (string.IsNullOrEmpty(winRank))
                    {
                        winRank = "組違い";
                        winAmount = 100000;
                    }
                }

                // 当選した場合は履歴に追加（4等以上のみ）
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

            // バッチ処理後、グローバル変数に加算（スレッド間の競合を減らす）
            Interlocked.Add(ref _dwCount, localCount);
            Interlocked.Add(ref _dwPayment, localPayment);
            Interlocked.Add(ref _dwPrize, localPrize);
            Interlocked.Exchange(ref _dwLoss, _dwPrize - _dwPayment);

            // 当選カウントを更新
            for (int i = 0; i < _winCounts.Length; i++)
            {
                Interlocked.Add(ref _winCounts[i], localWinCounts[i]);
            }

            // 当選履歴を追加
            foreach (var record in localWinRecords)
            {
                _winHistory.Add(record);
            }

            // ローカル変数をリセット
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
            // 処理速度を計算
            long currentCount = Interlocked.Read(ref _dwCount);
            long countDiff = currentCount - _lastUpdateCount;
            double elapsedSeconds = _speedStopwatch.ElapsedMilliseconds / 1000.0;

            if (elapsedSeconds > 0)
            {
                _processingSpeed = (int)(countDiff / elapsedSeconds);
            }

            _lastUpdateCount = currentCount;
            _speedStopwatch.Restart();

            // 表示用の当選履歴を更新
            _tableLoading = true;
            await InvokeAsync(() => {
                _displayWinHistory = _winHistory.OrderByDescending(w => w.PurchaseCount).Take(1000).ToList();
                _tableLoading = false;
                StateHasChanged();
            });
        }
        catch (Exception)
        {
            // エラー処理
        }
    }

    private void StopOrResetSimulation()
    {
        if (_simulationRunning)
        {
            // シミュレーション停止
            _cts?.Cancel();
            _uiUpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _simulationRunning = false;
        }
        else
        {
            // リセット
            _dwCount = 0;
            _dwPayment = 0;
            _dwPrize = 0;
            _dwLoss = 0;
            _processingSpeed = 0;

            // 当選回数もリセット
            for (int i = 0; i < _winCounts.Length; i++)
            {
                _winCounts[i] = 0;
            }

            // 当選履歴もクリア
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