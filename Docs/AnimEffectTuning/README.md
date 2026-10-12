# アニメ・エフェクト調整シーン

キャラのアニメを再生しながら、エフェクトを出す位置 (X / Y のオフセット) と出すタイミングを調整するためのツール用シーン。

## 使い方

1. メニュー `NeverNight/Anim & Effect Tuner` を開き、「調整シーンを開いて再生」を押す
   (`Assets/Scenes/Test/AnimEffectTuningScene.unity` を開いて再生する)
2. 「キャラ」でキャラを選ぶ。左の一覧からアニメを選ぶと再生される
3. 再生の操作: 再生 / 一時停止、最初から、1 コマ戻る / 進む (1/30 秒)、ループ、右向き、再生速度、ループの間
   - タイムラインをクリック / ドラッグするとその時間に移動する (エフェクトも同じ時間の見た目になる)
4. 「＋ エフェクトを追加 (今の時間に)」でエフェクトを足し、次の値を調整する
   - **オフセット**: キャラの足元からのずれ。右向きを基準にした値で、左を向くと X が反転する。シーンビューの丸いハンドルをドラッグしても動かせる
   - **開始 (秒)**: アニメの開始から何秒後に出すか。タイムラインの水色の印をドラッグしても動かせる (1/60 秒単位、Shift で自由)。「今の時間」で再生位置の値を入れる
   - **大きさ / 回転**
5. 値は `Assets/Data/AnimTuning/AnimEffectCueSet.asset` に直接書かれるので、再生を止めても残る (「保存」でディスクに書き出す)

## 登録されているもの

一覧は `Assets/Data/AnimTuning/AnimTuningCatalog.asset`。Inspector で追加・変更できる。

| 種類 | 登録のしかた | アニメの出方 |
|---|---|---|
| Spine のキャラ | `Spine` に骨格データ (SkeletonDataAsset)、または Spine 付きのプレハブ | 骨格のアニメが全部出る |
| Animator で動くキャラ | プレハブ | コントローラーのクリップを直接再生する (遷移は使わない) |
| SpriteFlipbook で動くもの | プレハブ | コマ送りが 1 本出る |
| コードで絵を差し替えているキャラ | プレハブ + `Sprite Clips` に連番の絵 | 登録した連番が出る (例: ポップコーン爆弾の 飛行 / ひび / 破裂) |
| エフェクト | `Effects` にプレハブ (パーティクル・Spine・Animator・Flipbook)、または連番の絵 | パーティクルは開始からの経過時間でシミュレートするので、一時停止・シークしても同じ形になる |

- プレハブは出したあと、Spine 以外のスクリプト (AI・攻撃など) と物理を外し、見た目だけの人形にする
- 識別は `Id` (キャラ名・エフェクト名) で行うので、エフェクトを設定した後に Id を変えない
- 今のところ、専用のエフェクト素材は「ポップコーン破裂 (popbomb の連番)」だけ。回復の光・スピーカーの爆発などはコードで組み立てているのでプレハブが無く、まだ一覧に出せない

## 作り直し

```
unity command run_script --file Docs/AnimEffectTuning/build_AnimEffectTuningScene.cs --entry BuildAnimEffectTuning.Run
```

シーンは作り直す。カタログは Id ごとに上書き / 追加する (手で足した分は残る)。エフェクト設定 (CueSet) は既にあれば触らない。

## コード

| ファイル | 役割 |
|---|---|
| `Assets/Scripts/Tools/AnimTuning/AnimTuningCatalog.cs` | キャラ・エフェクトの一覧 |
| `Assets/Scripts/Tools/AnimTuning/AnimEffectCueSet.cs` | 調整した値 (キャラ × アニメ → エフェクト・オフセット・開始・大きさ・回転) |
| `Assets/Scripts/Tools/AnimTuning/AnimTuningPuppet.cs` | プレハブ / 骨格を見た目だけの人形にし、時間を指定してアニメを動かす |
| `Assets/Scripts/Tools/AnimTuning/AnimTuningStage.cs` | シーンの本体 (時間を進める・エフェクトを重ねる・カメラ合わせ) |
| `Assets/Scripts/Editor/AnimEffectTunerWindow.cs` | 操作ウィンドウとシーンビューのハンドル |

ゲーム本編はまだ `AnimEffectCueSet` を読んでいない (調整用の値を残すところまで)。
