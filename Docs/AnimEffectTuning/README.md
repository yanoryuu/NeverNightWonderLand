# アニメ・エフェクト調整シーン

キャラのアニメを再生しながら、エフェクトを出す位置 (X / Y のオフセット) と出すタイミングを調整するためのツール用シーン。
調整した値は、キャラのプレハブに `AnimEffectEmitter` を付けるとゲームでもそのまま使われる。

## 使い方

1. メニュー `NeverNight/Anim & Effect Tuner` を開き、「調整シーンを開いて再生」を押す
   (`Assets/Scenes/Test/AnimEffectTuningScene.unity` を開いて再生する)
2. 「キャラ」でキャラを選ぶ。左の一覧からアニメを選ぶと再生される
   - Spine の左右の向き違い (`L/名前_L` と `R/名前_R`) は 1 つにまとめて出す。片方で調整すれば両方に効く
3. 再生の操作: 再生 / 一時停止、最初から、1 コマ戻る / 進む (1/30 秒)、ループ、右向き、再生速度、ループの間
   - タイムラインをクリック / ドラッグするとその時間に移動する (エフェクトも同じ時間の見た目になる)
4. 「＋ エフェクトを追加 (今の時間に)」でエフェクトを足し、次の値を調整する
   - **オフセット**: キャラの原点 (プレハブのルート) からのずれ。右向きを基準にした値で、左を向くと X が反転する。シーンビューの丸いハンドルをドラッグしても動かせる
   - **開始 (秒)**: アニメの開始から何秒後に出すか。タイムラインの水色の印をドラッグしても動かせる (1/60 秒単位、Shift で自由)。「今の時間」で再生位置の値を入れる
   - **大きさ / 回転**
   - **キャラに付いて動く**: ON なら出した後もキャラに付いて動く (オーラなど)。OFF なら出した場所に残る (土煙など)。調整シーンではキャラが動かないので見た目は変わらない
5. 値は `Assets/Data/AnimTuning/AnimEffectCueSet.asset` に直接書かれるので、再生を止めても残る (「保存」でディスクに書き出す)
6. ゲームで出すには、エフェクト欄の上に出る「このキャラのプレハブに付ける」を押す (再生を止めた時にプレハブへ `AnimEffectEmitter` が付き、キャラ Id と CueSet が設定される。再生中にプレハブを書き換えると Unity が警告を出すので、押した時は予約だけする)

## ゲームでの出し方 (AnimEffectEmitter)

`Assets/Scripts/Common/AnimEffect/AnimEffectEmitter.cs`。キャラのプレハブのルートに付ける。
アニメが始まったのをつかみ、調整した開始時刻が来たらエフェクトを出す。

| キャラの動かし方 | アニメの開始のつかみ方 | 時間の数え方 |
|---|---|---|
| Spine (グラミネなど) | トラック 0 の TrackEntry が変わった時。向き違い (L / R) への切り替えは続きとして扱い、出し直さない | TrackEntry.TrackTime |
| Animator (野良ウマ系) | レイヤー 0 のステートが変わった時 (遷移中は遷移先)。同じステートに入り直した時も開始 | normalizedTime × クリップの長さ |
| コードで絵を差し替えているもの (ポップコーン爆弾) | コードから `AnimEffectEmitter.Play("アニメ名")` を呼ぶ | Time.deltaTime の積み上げ |

- 時間はアニメの再生位置で数えるので、ヒットストップ・スロー・再生速度が変わっても調整シーンと同じタイミングで出る
- ループするアニメは 1 周ごとに出す
- アニメが途中で別のアニメに切り替わったら、まだ出していないエフェクトは出さない
- 向きは `EnemyController.FacingRight` (EnemyController が無ければルートの左右の大きさ) で決める
- エフェクトは種類ごとにプールして使い回す (`AnimEffectPlayer`)
- ポップコーン爆弾 (`EnemyLobBomb`) は、投げた時 `飛行`・爆発した時 `破裂`・斬られた時 `ひび` で `Play` を呼ぶ (プレハブに Emitter を付けた時だけ動く)
- シーンに置いた敵を個別に拡大・縮小している場合、エフェクトの位置はその大きさに合わせては変わらない (オフセットはワールドの長さ)

## 登録されているもの

キャラの一覧は `Assets/Data/AnimTuning/AnimTuningCatalog.asset`、エフェクトの一覧は `AnimEffectCueSet.asset` の `Effects`。Inspector で追加・変更できる。

| 種類 | 登録のしかた | アニメの出方 |
|---|---|---|
| Spine のキャラ | `Spine` に骨格データ (SkeletonDataAsset)、または Spine 付きのプレハブ | 骨格のアニメが全部出る |
| Animator で動くキャラ | プレハブ | コントローラーのクリップを直接再生する (遷移は使わない) |
| SpriteFlipbook で動くもの | プレハブ | コマ送りが 1 本出る |
| コードで絵を差し替えているキャラ | プレハブ + `Sprite Clips` に連番の絵 | 登録した連番が出る (例: ポップコーン爆弾の 飛行 / ひび / 破裂) |
| エフェクト | CueSet の `Effects` にプレハブ (パーティクル・Spine・Animator)、または連番の絵 (`Frames` と `Fps`) | パーティクルは開始からの経過時間でシミュレートするので、一時停止・シークしても同じ形になる |

- プレハブは出したあと、Spine 以外のスクリプト (AI・攻撃など) と物理を外し、見た目だけの人形にする。Spine で描く敵は仮の四角 (ルートの SpriteRenderer) を隠す
- プレハブのルートの大きさ (Boss_Gramine・Boss_Horse は 2 倍) はそのまま使うので、ゲームと同じ大きさで見える
- 識別は `Id` (キャラ名・エフェクト名) で行うので、エフェクトを設定した後に Id を変えない
- ゲームで使うのは「敵: 〜」のプレハブの Id。骨格データだけの登録 (慈) はゲームでは出ない
- 専用のエフェクト素材は今のところ「ポップコーン破裂 (popbomb の連番)」だけ。回復の光・スピーカーの爆発・裁断の斬撃はコードで組み立てていて専用の絵が無いので一覧に入れていない。エフェクトの絵が届いたら、プレハブか連番で `Effects` に足す

## 作り直し

```
unity command run_script --file Docs/AnimEffectTuning/build_AnimEffectTuningScene.cs --entry BuildAnimEffectTuning.Run
```

シーンは作り直す。キャラとエフェクトは Id ごとに上書き / 追加する (手で足した分は残る。`Assets/Prefabs/Enemies` の敵は自動で入る)。調整した値 (CueSet の `Entries`) は触らない。

## コード

| ファイル | 役割 |
|---|---|
| `Assets/Scripts/Common/AnimEffect/AnimEffectCueSet.cs` | エフェクトの一覧と調整した値 (キャラ × アニメ → エフェクト・オフセット・開始・大きさ・回転・追従) |
| `Assets/Scripts/Common/AnimEffect/AnimEffectEmitter.cs` | ゲームでアニメの開始をつかみ、調整した値でエフェクトを出す |
| `Assets/Scripts/Common/AnimEffect/AnimEffectPlayer.cs` | エフェクト 1 つぶんの再生とプール |
| `Assets/Scripts/Tools/AnimTuning/AnimTuningCatalog.cs` | 調整シーンで選べるキャラの一覧 |
| `Assets/Scripts/Tools/AnimTuning/AnimTuningPuppet.cs` | プレハブ / 骨格を見た目だけの人形にし、時間を指定してアニメを動かす |
| `Assets/Scripts/Tools/AnimTuning/AnimTuningStage.cs` | シーンの本体 (時間を進める・エフェクトを重ねる・カメラ合わせ) |
| `Assets/Scripts/Editor/AnimEffectTunerWindow.cs` | 操作ウィンドウ・シーンビューのハンドル・プレハブへの Emitter の取り付け |
