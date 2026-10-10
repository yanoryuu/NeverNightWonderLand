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
    [Tooltip("部屋名。セーブ画面の「場所」に使う。エリアのシーン (A{n}_) では入室時のタイトルには出さない (エリア名を初回だけ出す)")]
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
        var number = AreaNumberFromSceneName(sceneName);
        return number < 0 ? "" : ChapterTitle(number);
    }

    /// <summary>エリア名の表示済みフラグ (GameProgress) の接頭辞。例 "AreaTitleShown_A1"。</summary>
    public const string AreaTitleFlagPrefix = "AreaTitleShown_A";

    /// <summary>シーン名の接頭辞 "A{n}_" のエリア番号。A{n}_ で始まらなければ -1。</summary>
    public static int AreaNumberFromSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || sceneName.Length < 3 || sceneName[0] != 'A')
            return -1;

        var underscore = sceneName.IndexOf('_');
        if (underscore < 2 || !int.TryParse(sceneName.Substring(1, underscore - 1), out var number))
            return -1;

        return number;
    }

    /// <summary>
    /// ステージに入った時に出すタイトルを決めて返す (表示しないなら空)。AreaTitleView が入室ごとに 1 回呼ぶ。
    /// エリアのシーン (A{n}_ でエリア名が決まっている) では部屋名は出さず、エリア名をそのエリアに初めて入った時だけ返す
    /// (2026-10-03 ユーザー決定。表示済みは GameProgress のフラグ <see cref="AreaTitleFlagPrefix"/>{n} でセーブに残る)。
    /// それ以外のステージは従来どおり毎回 _areaName (部屋名) を返す。
    /// </summary>
    public string ConsumeEntryTitle()
    {
        var number = AreaNumberFromSceneName(gameObject.scene.name);
        var areaTitle = number >= 0 && number < AreaTitles.Length ? AreaTitles[number] : "";
        if (string.IsNullOrEmpty(areaTitle))
            return _areaName;

        var flag = AreaTitleFlagPrefix + number;
        var pending = GameSession.PendingLoad;
        // セーブ復帰・リトライではフラグの復元 (PlayerSaveBridge.TryApplyPendingLoad) がステージ有効化より後なので、復帰データも見る
        var pendingHas = pending?.progressFlags != null && System.Array.IndexOf(pending.progressFlags, flag) >= 0;
        var shown = GameProgress.Has(flag) || pendingHas;

        GameProgress.Set(flag);
        // 復帰データで GameProgress が上書きされても消えないよう、復帰データ側にも足しておく
        if (pending != null && !pendingHas)
        {
            var flags = pending.progressFlags ?? System.Array.Empty<string>();
            System.Array.Resize(ref flags, flags.Length + 1);
            flags[^1] = flag;
            pending.progressFlags = flags;
        }

        if (shown)
            return "";

        return areaTitle;
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
