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
    /// 死亡リトライで拠点から再開するのと、能力の取得をセーブへ書き足すのは true の時だけ (PendingChanges 38)。
    /// </summary>
    public static bool SaveBelongsToRun;

    /// <summary>エリア開始時にリザルト集計をリセットする。</summary>
    public static void ResetRun()
    {
        EnemiesDefeated = 0;
        RunStartTime = Time.time;
    }
}
