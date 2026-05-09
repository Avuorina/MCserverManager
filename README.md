# MCServerManager

MCServerManagerは、Minecraft Java Editionのサーバー管理を簡単にするために作られたWindowsデスクトップアプリケーションよ。
WPF (Windows Presentation Foundation) と .NET 8 をベースに構築されており、シンプルなMVVMアーキテクチャを採用しているわ。

## 主な機能

- **サーバーの一覧管理**: 複数のサーバー（フォルダパス、起動バッチファイル、ポート番号）をリストで一元管理できます。
- **ワンボタン起動 (魔法)**: 「起動」ボタンを押すだけで、以下の処理を全自動で行います。
  - `server.properties` からのポート番号自動検出
  - **Windowsファイアウォールの自動開放**（ルールが未設定の時だけUACプロンプトを出して管理者権限で `netsh` を実行し、2回目以降はパスする賢い設計よ）
  - **UPnPによるルーターのポートマッピング自動追加**（`SharpOpenNat` を使用）
  - コマンドプロンプトでのサーバーバッチファイル（`.bat`）実行
- **状態の保存**: サーバーのリストは `%AppData%\MCServerManager\servers.json` に自動保存・読み込みされます。

## 使い方

1. アプリを起動するとサーバーリストが表示されるわ。
2. **「➕ サーバー追加」**ボタンでMinecraftサーバーのフォルダと起動用`.bat`ファイルを登録してね。
3. リストから対象のサーバーを選び、**「起動」**ボタンをクリック。
4. 初回（ファイアウォールのルールがない場合）のみ、UAC（管理者権限）の確認が出るわ。
5. 自動でポート開放とUPnPマッピングが行われ、サーバーが起動するわよ！

### 注意事項
- ファイアウォール開放には初回のみ管理者権限が必要よ。
- UPnPポート開放はルーターが対応していないと失敗するわ（その場合は手動でポート開放してね）。
- `.bat`ファイルはサーバーフォルダ内で `start.bat` や `run.bat` など、正常にサーバーを起動できるものを使用してちょうだい。

## プロジェクトとファイル構成

```text
MCServerManager/
│  .gitattributes              # Gitの改行コード設定（CRLF/LF問題の解決）
│  .gitignore                  # Git管理から除外するファイル（.vs, bin, obj等）
│  MCServerManager.sln         # Visual Studio ソリューションファイル
│  README.md                   # このドキュメントよ
│
└─MCServerManager/             # メインプロジェクトフォルダ
    │  MCServerManager.csproj  # プロジェクト設定（.NET 8 WPF, SharpOpenNatパッケージ参照）
    │  App.xaml / .cs          # アプリケーションのエントリポイント
    │  MainWindow.xaml / .cs   # メイン画面のUIとコードビハインド
    │  AddServerWindow.xaml / .cs # サーバー追加用のモーダル画面
    │
    ├─Models/
    │      ServerInfo.cs       # サーバー情報を保持するデータモデル
    │
    ├─ViewModels/
    │      MainViewModel.cs    # メイン画面のロジック（リスト管理、保存/読込、起動コマンド）
    │      AddServerViewModel.cs # サーバー追加画面のロジック（フォルダ・ファイル選択）
    │
    ├─Services/
    │      ServerManagerService.cs # 魔法の心臓部。プロセス起動、ポート検出、ファイアウォール、UPnP処理を担当
    │
    └─Mvvm/
           RelayCommand.cs     # MVVMアーキテクチャのためのICommand実装クラス
```

## 使用している技術・ライブラリ
- **.NET 8.0 WPF**: UIフレームワーク
- **SharpOpenNat (4.0.17)**: UPnPデバイスの検出とポートマッピングを非同期で行うためのライブラリ
- **System.Text.Json**: アプリケーション設定（サーバーリスト）のJSONシリアライズ/デシリアライズ

## ビルド方法
Visual Studioで `.sln` ファイルを開いてビルドするか、以下のコマンドで単一の実行ファイル（Single-file EXE）として出力できます。

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
