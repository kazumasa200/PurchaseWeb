# AIチャット機能付き会計WebAPP
本プログラムはデータベースを用いて基本的な商品マスタ機能、購入機能、購入履歴機能を閲覧できるアプリである。

## 実際に動いているものを見るには
https://noeleve.net

## 動作方法
1. developブランチをプルする
2. DBにあるdocker-compose.ymlを```docker compose```する
3. VS2022やVSCodeでデバッグをする。

## 備考
- パスワードは環境変数で設定するようになっています。appsettings.jsonに```StorePassword```というフィールドがあると思うのでそこで指定します。

