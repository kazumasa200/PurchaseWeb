# AIチャット機能付き会計WebAPP

## 実際に動いているものを見るには
https://noeleve.net

## 動作方法
1. developブランチをプルする
2. DBにあるdocker-compose.ymlを```docker compose```する
3. VS2022やVSCodeでデバッグをする。

## 備考
- パスワードは環境変数で設定するようになっています。appsettings.jsonに```StorePassword```というフィールドがあると思うのでそこで指定します。
- AIチャットはローカルLLMのみに対応しています。ローカルLLMのサーバーが立っていない場合は```オフラインのようです```とエラーが出ます。
- AI機能を使うにはInfra.Repositories.LMStudioService.cs内のBaseUrlを自身のローカルLLMのAPIエンドポイントにする必要があります。
- また、LMStudioのOpenAI互換APIでの動作を想定しているのでOllama等では動作しない可能性があります。
- 以上を踏まえ、AI機能の確認をする場合は作成者にご一報いただければ幸いです。
