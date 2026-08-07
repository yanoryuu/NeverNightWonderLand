# コマンドリファレンス: UI操作・外部連携

## メッセージウィンドウ操作

複数ウィンドウは AdvMessageWindowManager に登録されたウィンドウ名を使う。通常のセリフでの切り替えは WindowType 列でも可能。

| コマンド | 機能 | Arg1 | Arg2〜Arg6 |
|---|---|---|---|
| ShowMessageWindow | ウィンドウを強制表示（通常はテキスト系コマンドまで表示されない） | — | — |
| HideMessageWindow | ウィンドウを非表示。**次にテキストを表示するページの冒頭で自動的に再表示される**ため、非表示が続くのはテキストを表示しない区間（Wait・演出等）の間だけ | — | — |
| InitMessageWindow | 使用ウィンドウを初期化。複数指定で同時表示 | ウィンドウ名 | ウィンドウ名（追加分。空欄まで） |
| ChangeMessageWindow | アクティブウィンドウを切り替え（非アクティブ名を指定すると交換） | ウィンドウ名 | — |

## メニューボタン表示切替

| コマンド | 機能 |
|---|---|
| HideMenuButton | メニューボタンを非表示（改ページ後も保持） |
| ShowMenuButton | 非表示状態を解除して通常動作に戻す |

## GUI操作（GuiActive / GuiPosition / GuiSize / GuiReset）

対象UIはあらかじめ AdvEngine > UI > **AdvGuiManager** コンポーネントに登録しておく。

| コマンド | 機能 | Arg1 | Arg2 | Arg3 |
|---|---|---|---|---|
| GuiActive | アクティブON/OFF | GUI名（空欄=登録済み全UI） | On/Off | — |
| GuiPosition | 位置変更 | GUI名 | X | Y |
| GuiSize | サイズ変更 | GUI名 | 横 | 縦 |
| GuiReset | 初期状態にリセット | GUI名（空欄=全UI） | — | — |

```csv
GuiActive,MiniMap,TRUE
GuiPosition,MiniMap,100,-50
GuiReset,MiniMap
```

## SendMessage（Unity側プログラム呼び出し）

シナリオから独自C#処理を呼ぶ簡易拡張。受信オブジェクトを AdvScenarioPlayer の「SendMessage」欄に設定しておく。

| コマンド | Arg1 | Arg2〜Arg6 |
|---|---|---|
| SendMessage | 識別名（必須） | 任意の文字列パラメータ |

受信側の実装:

```csharp
// コマンド実行時
void OnDoCommand(AdvCommandSendMessage command)
{
    switch (command.Name) // Arg1
    {
        case "MyCommand":
            // command.Arg2, command.Arg3 ... を使って処理
            break;
    }
}
// 待機が必要な場合（毎フレーム呼ばれる）
void OnWait(AdvCommandSendMessage command)
{
    command.IsWait = true; // trueの間シナリオが待機
}
```

結果をシナリオ変数に反映するには `engine.Param.TrySetParameter("名前", 値)`。パラメーターとして保存すればセーブにも自動対応。

```csv
SendMessage,MyCommand,パラメータ1
```

## SendMessageByName / BroadcastMessageByName（名前指定呼び出し）

事前設定不要で、シーン内のGameObjectを名前検索してメッセージ送信。動的生成オブジェクトにも対応。

| コマンド | Arg1 | Arg2 | Arg3 | Arg4〜 |
|---|---|---|---|---|
| SendMessageByName | GameObject名 | メソッド名 | 任意引数 | 任意引数 |
| BroadcastMessageByName | GameObject名（以下全子に送信） | メソッド名 | 検索対象タイプ | 任意引数 |

BroadcastMessageByName の Arg3（検索対象）: `Default`（全シーン・空欄と同じ）／`UtageObject`（宴の描画オブジェクトのみ・軽量）／`RenderTexture`（RenderTexture下のプレハブ検索）。

受信側の実装（メソッド名＝Arg2）:

```csharp
void Test(AdvCommandSendMessageByName command)
{
    string arg3 = command.ParseCellOptional<string>(AdvColumnName.Arg3, "既定値");
}
// 待機する場合は command.IsWait を true→false に制御
```

注意: FindObject を使うため実行時負荷あり。非アクティブオブジェクトは検索対象外。同名オブジェクトに注意。速度重視なら SendMessage を使う。

```csv
SendMessageByName,MyGameObject,OnCustomEvent,パラメータ1
BroadcastMessageByName,ParentObject,OnCustomEvent,UtageObject
```

