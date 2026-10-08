using UnityEngine;

/// <summary>
/// シーンをまたぐ実行時状態。
/// - PendingLoad: シーン読込後に PlayerSaveBridge が適用するセーブデータ (コンティニュー/リトライ用)
/// - 撃破数・開始時刻: リザルト画面の集計用 (エリア開始ごとに ResetRun)
/// </summary>
public static class GameSession
{
    /// <summary>次のシーン読込後に適用するセーブデータ。適用されると null に戻る。</summary>
    public static SaveData PendingLoad;

    /// <summary>PlayerScene 起動時にロードするステージ名 (StageLoader が読む)。読まれると null に戻る。</summary>
    public static string PendingStage;

    /// <summary>今回の挑戦での敵撃破数 (EnemyController が加算)。</summary>
    public static int EnemiesDefeated;

    /// <summary>今回の挑戦の開始時刻 (Time.time)。</summary>
    public static float RunStartTime;

    /// <summary>
    /// 累計プレイ時間 (秒)。ゲーム中 (PlayerScene の StageLoader がある間、ポーズ・メニュー中を除く) に実時間で加算され、
    /// セーブに記録される。はじめからで 0、つづきからでセーブの値に戻る。
    /// </summary>
    public static double PlayTimeSeconds;

    /// <summary>
    /// 現在のスロット (SaveSystem.ActiveSlot) のセーブがこのプレイのものか。つづきから・拠点でのセーブで true、
    /// はじめからで false (選んだスロットに前のプレイのセーブが残っていても、拠点でセーブするまでは別のプレイのもの)。
    /// エディタでステージを直接再生した時も false。
    /// 永続する進行 (能力・キーアイテム・フラグ・コイン・糸など) をその場でセーブへ書き込むのは true の時だけ
    /// (PlayerSaveBridge の自動書き込み, PendingChanges 38/39)。
    /// </summary>
    public static bool SaveBelongsToRun;

    /// <summary>
    /// このプレイの記録先スロット (SaveSystem.ActiveSlot) をプレイヤーが選んだか。タイトルの はじめから・つづきから で true。
    /// エディタでステージを直接再生した時は false (拠点で休んでも勝手にスロットへ書かない。PendingChanges 47)。
    /// </summary>
    public static bool RunSlotChosen;

    /// <summary>
    /// 最後に休んだ拠点 (死亡リトライの復帰先) のステージ名。空なら未設定 (はじめから後にまだ休んでいない / エディタの直接再生)。
    /// 拠点で休んだ時・セーブした時・つづきからで設定され、はじめからで消える (PendingChanges 39)。
    /// </summary>
    public static string RespawnScene;

    /// <summary>最後に休んだ拠点の復帰位置。</summary>
    public static Vector2 RespawnPosition;

    public static bool HasRespawnBench => !string.IsNullOrEmpty(RespawnScene);

    public static void SetRespawnBench(string sceneName, Vector2 position)
    {
        RespawnScene = sceneName;
        RespawnPosition = position;
    }

    public static void ClearRespawnBench() => RespawnScene = null;

    /// <summary>エリア開始時にリザルト集計をリセットする。</summary>
    public static void ResetRun()
    {
        EnemiesDefeated = 0;
        RunStartTime = Time.time;
    }
}
