using System;
using UnityEngine;

/// <summary>
/// ステージ側に置くステージ情報 (データだけ、UI は持たない)。
/// UI は UI シーン (StageUI) に分離されており、StageUI の AreaTitleView が
/// <see cref="Entered"/> / <see cref="Current"/> を見てエリア名を表示する。
/// シーン開始のリセット処理 (ポーズ解除・リザルト集計リセット) もここで行う
/// (旧方式で SceneUI の AreaTitleView が Awake で行っていたものと同じタイミング = ステージのロード時)。
/// GameField_old の旧ステージはこれを持たず、従来どおり自前の SceneUI がタイトル表示とリセットを行う。
/// </summary>
public class StageInfo : MonoBehaviour
{
    [Tooltip("エリア (部屋) 名。ステージに入った時のタイトル表示とセーブ画面の「場所」に使う")]
    [SerializeField] private string _areaName = "";

    [Tooltip("章の表示名 (セーブ画面に出る)。空ならシーン名の接頭辞 A{n}_ から「第n章 (エリア名)」を作る")]
    [SerializeField] private string _chapterName = "";

    /// <summary>ロード済みの (最後に入った) ステージ情報。無ければ null。</summary>
    public static StageInfo Current { get; private set; }

    /// <summary>ステージに入った (StageInfo が有効になった) 時に呼ばれる。</summary>
    public static event Action<StageInfo> Entered;

    public string AreaName => _areaName;

    /// <summary>章の表示名。未設定ならシーン名 (A1_xx → 第1章) から作る。分からなければ空。</summary>
    public string ChapterName => !string.IsNullOrEmpty(_chapterName) ? _chapterName : ChapterFromSceneName(gameObject.scene.name);

    /// <summary>エリア番号ごとの章の名前 (ユーザー決定: エリア1 =「鳴りやまぬ遊園地」)。無い番号は「第n章」だけ。</summary>
    private static readonly string[] AreaTitles = { "", "鳴りやまぬ遊園地" };

    /// <summary>章の表示名: 「第n章 エリア名」(エリア名が決まっていなければ「第n章」)。</summary>
    public static string ChapterTitle(int areaNumber)
    {
        var title = areaNumber >= 0 && areaNumber < AreaTitles.Length ? AreaTitles[areaNumber] : "";
        return string.IsNullOrEmpty(title) ? $"第{areaNumber}章" : $"第{areaNumber}章 {title}";
    }

    /// <summary>シーン名の接頭辞 "A{n}_" から「第n章 エリア名」を作る。A{n}_ で始まらなければ空。</summary>
    public static string ChapterFromSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || sceneName.Length < 3 || sceneName[0] != 'A')
            return "";

        var underscore = sceneName.IndexOf('_');
        if (underscore < 2 || !int.TryParse(sceneName.Substring(1, underscore - 1), out var number))
            return "";

        return ChapterTitle(number);
    }

    private void Awake()
    {
        // シーン開始のリセット処理
        GamePause.Reset();
        GameSession.ResetRun();
    }

    private void OnEnable()
    {
        Current = this;
        Entered?.Invoke(this);
    }

    private void OnDisable()
    {
        // 遷移中は新ステージが先に有効になるので、自分が現在の時だけ外す
        if (Current == this)
            Current = null;
    }
}
