# MCServerManager

MCServerManagerは、Minecraft Java Editionのサーバー管理を簡単にするために作られたWindowsデスクトップアプリケーションです。
WPF (Windows Presentation Foundation) と .NET 8 をベースに構築されており、シンプルなMVVMアーキテクチャを採用しています。

## 主な機能

- **サーバーの一覧管理**: 複数のサーバー（フォルダパス、起動バッチファイル、ポート番号）をリストで一元管理できます。
- **ワンボタン起動**: 「起動」ボタンを押すだけで、以下の処理を全自動で行います。
  - `server.properties` からのポート番号自動検出
  - **Windowsファイアウォールの自動開放**（ルールが未設定の時だけUACプロンプトを出して管理者権限で `netsh` を実行し、2回目以降はスキップします）
  - **UPnPによるルーターのポートマッピング自動追加**（`SharpOpenNat` を使用）
  - コマンドプロンプトでのサーバーバッチファイル（`.bat`）実行
- **グローバルIPのワンタッチコピー**: サーバーのIPアドレスをワンクリックでクリップボードにコピーし、すぐに共有できます。
- **プレイヤー数とステータスの自動更新**: 10秒ごとのオートリフレッシュ機能により、サーバーの稼働状況と現在のプレイヤー数をリアルタイムで確認できます。
- **状態の保存**: サーバーのリストは `%AppData%\MCServerManager\servers.json` に自動保存・読み込みされます。

## 使い方

1. アプリを起動するとサーバーリストが表示されます。
2. **「➕ サーバー追加」**ボタンでMinecraftサーバーのフォルダと起動用`.bat`ファイルを登録します。
3. リストから対象のサーバーを選び、**「起動」**ボタンをクリックします。
4. 初回（ファイアウォールのルールがない場合）のみ、UAC（管理者権限）の確認画面が表示されます。
5. 自動でポート開放とUPnPマッピングが行われ、サーバーが起動します。
6. リスト上の「IPコピー」ボタンを使用すると、自身のグローバルIPアドレスを簡単にコピーできます。

### 注意事項
- ファイアウォール開放には初回のみ管理者権限が必要です。
- UPnPポート開放はルーターが対応していないと失敗する場合があります（その場合は手動でポート開放を行ってください）。
- `.bat`ファイルはサーバーフォルダ内で `start.bat` や `run.bat` など、正常にサーバーを起動できるものを使用してください。

## プロジェクトとファイル構成

```text
MCServerManager/
│  .gitattributes              # Gitの改行コード設定（CRLF/LF問題の解決）
│  .gitignore                  # Git管理から除外するファイル（.vs, bin, obj等）
│  MCServerManager.sln         # Visual Studio ソリューションファイル
│  README.md                   # 本ドキュメント
│
└─MCServerManager/             # メインプロジェクトフォルダ
    │  MCServerManager.csproj  # プロジェクト設定（.NET 8 WPF, パッケージ参照）
    │  appicon.ico             # アプリケーションアイコン
    │  App.xaml / .cs          # アプリケーションのエントリポイント
    │  MainWindow.xaml / .cs   # メイン画面のUIとコードビハインド
    │  AddServerWindow.xaml / .cs # サーバー追加用のモーダル画面
    │
    ├─Models/
    │      ServerInfo.cs       # サーバー情報を保持するデータモデル
    │
    ├─ViewModels/
    │      MainViewModel.cs    # メイン画面のロジック（リスト管理、ポーリング、起動）
    │      AddServerViewModel.cs # サーバー追加画面のロジック
    │
    ├─Services/
    │      ServerManagerService.cs # プロセス起動、ポート検出、ファイアウォール、UPnP処理を担当
    │
    └─Mvvm/
           RelayCommand.cs     # MVVMアーキテクチャのためのICommand実装クラス
```

## 使用している技術・ライブラリ
- **.NET 8.0 WPF**: UIフレームワーク
- **SharpOpenNat (4.0.17)**: UPnPデバイスの検出とポートマッピングを非同期で行うためのライブラリ
- **MineStat (3.1.2)**: MinecraftサーバーへのPing送信およびプレイヤー数取得用のライブラリ
- **System.Text.Json**: アプリケーション設定（サーバーリスト）のJSONシリアライズ/デシリアライズ

## ビルド方法
Visual Studioで `.sln` ファイルを開いてビルドするか、以下のコマンドで単一の実行ファイル（Single-file EXE）として出力できます。

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
