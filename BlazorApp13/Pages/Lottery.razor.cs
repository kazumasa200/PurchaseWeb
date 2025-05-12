namespace PurchaseWeb.Pages
{
    public partial class Lottery
    {
        private bool _simulationRunning = false;
        private long _dwCount = 0;
        private long _dwPayment = 0;
        private long _dwPrize = 0;
        private long _dwLoss = 0;
        private Random _random = new();
        public required CancellationTokenSource _cts;

        // 当選回数カウント用配列（1等、2等、...、前後賞、組違い）
        private int[] _winCounts = new int[9];

        // 当選履歴を保存するリスト
        private List<WinRecord> _winHistory = [];

        // 当選記録クラス
        public class WinRecord
        {
            public long PurchaseCount { get; set; }  // 購入回数
            public string Rank { get; set; } = string.Empty;         // 等級
            public long Amount { get; set; }         // 当選金額
            public long TotalBalance { get; set; }   // 累計損益
        }

        private async Task StartSimulation()
        {
            _simulationRunning = true;
            _cts = new CancellationTokenSource();

            try
            {
                await Task.Run(async () =>
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        // 乱数生成（元のコードのmt19937arに相当）
                        uint r = (uint)_random.Next(0, int.MaxValue) + (uint)_random.Next(0, 2) * int.MaxValue;
                        if (r >= 4290000000) continue;

                        _dwCount++;
                        _dwPayment += 300;

                        bool hasWon = false;
                        string winRank = "";
                        long winAmount = 0;

                        if (r % 5000000 == 0) // 1等 200,000,000円
                        {
                            _dwPrize += 200000000;
                            _winCounts[0]++;
                            hasWon = true;
                            winRank = "1等";
                            winAmount = 200000000;
                        }

                        if (r % 10000000 == 0) // 2等 100,000,000円
                        {
                            _dwPrize += 100000000;
                            _winCounts[1]++;
                            hasWon = true;
                            winRank = "2等";
                            winAmount = 100000000;
                        }

                        if (r % 500000 == 0) // 3等 1,000,000円
                        {
                            _dwPrize += 1000000;
                            _winCounts[2]++;
                            hasWon = true;
                            winRank = "3等";
                            winAmount = 1000000;
                        }

                        if (r % 100000 == 0) // 4等 500,000円
                        {
                            _dwPrize += 500000;
                            _winCounts[3]++;
                            hasWon = true;
                            winRank = "4等";
                            winAmount = 500000;
                        }

                        if (r % 1000 == 0) // 5等 10,000円
                        {
                            _dwPrize += 10000;
                            _winCounts[4]++;
                            hasWon = true;
                            winRank = "5等";
                            winAmount = 10000;
                        }

                        if (r % 100 == 0) // 6等 3,000円
                        {
                            _dwPrize += 3000;
                            _winCounts[5]++;
                            hasWon = true;
                            winRank = "6等";
                            winAmount = 3000;
                        }

                        if (r % 10 == 0) // 7等 300円
                        {
                            _dwPrize += 300;
                            _winCounts[6]++;
                            hasWon = true;
                            winRank = "7等";
                            winAmount = 300;
                        }

                        if (r % 2500000 == 0) // 1等前後 50,000,000円
                        {
                            _dwPrize += 50000000;
                            _winCounts[7]++;
                            hasWon = true;
                            winRank = "前後賞";
                            winAmount = 50000000;
                        }

                        if (r % 50000 == 0 && (r / 50000) % 100 != 0) // 1等組違い 100,000円
                        {
                            _dwPrize += 100000;
                            _winCounts[8]++;
                            hasWon = true;
                            winRank = "組違い";
                            winAmount = 100000;
                        }

                        _dwLoss = _dwPrize - _dwPayment;

                        // 当選した場合は履歴に追加
                        if (hasWon && winAmount >= 10000) // 5等以上のみ記録
                        {
                            await InvokeAsync(() =>
                            {
                                _winHistory.Add(new WinRecord
                                {
                                    PurchaseCount = _dwCount,
                                    Rank = winRank,
                                    Amount = winAmount,
                                    TotalBalance = _dwLoss
                                });
                            });
                        }

                        // UIの更新
                        await InvokeAsync(StateHasChanged);
                    }
                }, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // キャンセルされた場合は何もしない
            }
            finally
            {
                _simulationRunning = false;
                StateHasChanged();
            }
        }

        private void StopOrResetSimulation()
        {
            if (_simulationRunning)
            {
                // シミュレーション停止
                _cts?.Cancel();
                _simulationRunning = false;
            }
            else
            {
                // リセット
                _dwCount = 0;
                _dwPayment = 0;
                _dwPrize = 0;
                _dwLoss = 0;

                // 当選回数もリセット
                for (int i = 0; i < _winCounts.Length; i++)
                {
                    _winCounts[i] = 0;
                }

                // 当選履歴もクリア
                _winHistory.Clear();
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}