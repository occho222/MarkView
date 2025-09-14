# リリース手順

## 実装後

### 実装後ビルドと動作確認

実装完了後は必ず以下の手順で動作確認を行う:

1. **ビルド確認**: `dotnet build` を実行し、コードが正常にコンパイルされることを確認する
2. **起動確認**: `dotnet run` を実行し、アプリケーションが正常に起動することを確認する
3. **基本機能確認**: アプリが起動後、以下の基本機能が動作することを確認する:
   * UI要素（メニュー、ツールバー、サイドバー）が正しく表示される
   * エラーダイアログやクラッシュが発生しない
   * 主要機能（ファイル読み込み、表示切り替えなど）が動作する
4. **アプリ終了**: 動作確認が完了したらアプリケーションを自動終了する
   * Windows環境: `taskkill /F /IM dotnet.exe /T` で.NETプロセスツリー全体を強制終了
   * または実行中のターミナルでCtrl+Cを送信
   * プロセス確認: `tasklist | findstr dotnet` でプロセスが残っていないか確認

**注意**: ビルドが成功してもランタイムエラーでアプリが起動しない場合があるため、必ず起動確認まで実行すること

## 実装ガイドライン

### アーキテクチャ設計

#### MVVMベストプラクティス

WPFアプリケーションはMVVMパターンに従って実装する:

**Model層**
- データモデルとビジネスロジック
- `INotifyPropertyChanged`を実装してデータバインディング対応
- 単体テスト可能な純粋なC#クラス

**View層 (XAML)**
- UIの定義のみに専念
- コードビハインドは最小限に抑制
- DataContextでViewModelとバインディング

**ViewModel層**
- ViewとModelの仲介役
- `ICommand`でユーザーアクションを処理
- プロパティ変更通知を適切に実装

```csharp
// 例: ViewModelベースクラス
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

### コード品質管理

#### リファクタリング方針

実装変更時には以下のリファクタリングを必須実行する:

1. **冗長なコード排除**
   - 重複した処理の共通化
   - 不要な変数・メソッドの削除
   - 複雑な条件分岐の簡素化

2. **メソッド分割**
   - 単一責任原則に従ってメソッドを分割
   - 1つのメソッドは20行以内を目標
   - 引数は5個以内に制限

3. **命名改善**
   - 意図を明確に表す変数名・メソッド名
   - C#命名規則に準拠
   - 省略形は避けて完全な単語を使用

#### 保守性向上のための実装方針

**処理の共通化**
- 類似処理はヘルパークラス・メソッドに集約
- 設定値は定数クラスまたはappsettings.jsonで管理
- 例外処理は共通ハンドラーで統一

**不具合防止策**
- null安全性の確保（null条件演算子の活用）
- 型安全性の向上（強い型付け）
- 境界値・異常系のテストケース追加

**依存関係管理**
- DIコンテナの活用（Microsoft.Extensions.DependencyInjection）
- インターフェースベースの設計
- 循環参照の回避

```csharp
// 例: 共通化されたファイル操作ヘルパー
public static class FileHelper
{
    public static async Task<string> SafeReadTextAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return string.Empty;

        try
        {
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            Logger.LogError($"File read error: {filePath}", ex);
            return string.Empty;
        }
    }
}
```

## 自動リリースワークフロー

ユーザーがリリースを要求した場合、以下の手順を自動的に実行する:

1. **バージョン更新** 

   * `${PROJECT_NAME}.csproj` の Version, AssemblyVersion, FileVersion を更新

2. **リリースノート更新**:

   * `RELEASE_NOTES.md` の先頭に新バージョンの変更内容を追加
   * 新機能（✨）、改善（🔧）、修正（🐛）、削除（🗑️）に分類して記載
   * Git履歴を参考にして主要な変更点を網羅

3. **最終リリースビルド**: バージョン更新後に `dotnet build -c Release`

4. **リリースファイルのパッケージ化**:

   ```bash
   mkdir -p "release/${PROJECT_NAME}XXX"  # XXX = ドットを除いたバージョン番号
   cp -r "bin/Release/net6.0-windows/"* "release/${PROJECT_NAME}XXX/"
   cp "RELEASE_NOTES.md" "release/${PROJECT_NAME}XXX/"
   ```

   * パッケージ化したファイルサイズを確認する
   * `du -sh "release/${PROJECT_NAME}XXX/"` でサイズ確認
   * リリースノートを配布パッケージに同梱する

5. **ZIP圧縮**:

   ```bash
   cd "release/${PROJECT_NAME}XXX"
   "C:\Program Files\7-Zip\7z.exe" a -tzip "../${PROJECT_NAME}XXX.zip" *
   cd ../..
   ```

   * 配布用のZIPファイルを生成する（フォルダ直下のファイルがZIPの直下に展開される）
   * パッケージディレクトリと同じ場所にZIPファイルが作成される

6. **Gitにコミット・プッシュ**

   * 全ての変更をステージング: `git add .`
   * バージョン情報を含むコミット
   * リモートリポジトリにプッシュ: `git push origin main`
