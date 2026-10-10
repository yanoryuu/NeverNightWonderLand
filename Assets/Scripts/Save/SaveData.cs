using System;

/// <summary>
/// セーブデータ (JSON シリアライズ)。拠点でのセーブ時に PlayerSaveBridge.Collect() で生成される。
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>セーブしたシーン名。</summary>
    public string sceneName;

    /// <summary>復帰位置 (拠点の位置)。</summary>
    public float posX;
    public float posY;

    /// <summary>取得済みハサミ強化 (ScissorUpgrade の int 値)。</summary>
    public int[] upgrades;

    /// <summary>取得済み移動スキル (PlayerSkill の int 値)。</summary>
    public int[] skills;

    /// <summary>鍛冶強化 (有償) の回数。攻撃力と最大コンボ数に反映される。</summary>
    public int forgeLevel;

    /// <summary>アイテムの ID (ItemDefinition のアセット名)。itemCounts と対になる。</summary>
    public string[] itemIds;

    /// <summary>各アイテムの所持数 (itemIds と同じ並び)。</summary>
    public int[] itemCounts;

    /// <summary>スロット (下/左/右) にセットされたアイテムの ID (空は "")。</summary>
    public string[] slotItemIds;

    /// <summary>素材「糸」の所持数。</summary>
    public int thread;

    /// <summary>コイン (収集品) の枚数。取ったコインの箱は progressFlags の A1_Coin_* で分かる (旧セーブは 0)。</summary>
    public int coins;

    /// <summary>装備中の近接攻撃 (□) の ID (AttackDefinition のアセット名。空は未装備)。</summary>
    public string equippedMeleeId;

    /// <summary>装備中の特殊攻撃 (△) の ID (空は未装備)。</summary>
    public string equippedSpecialId;

    /// <summary>解放済みの攻撃方法の ID。</summary>
    public string[] unlockedAttackIds;

    /// <summary>進行フラグ (中ボス撃破など。GameProgress が管理)。</summary>
    public string[] progressFlags;

    /// <summary>地図の踏破記録 (入ったことのある部屋の ID。MapExploration が管理)。旧セーブは null = 踏破なし。</summary>
    public string[] visitedRooms;

    // ---- スロット一覧の表示用 (ロード画面) ----

    /// <summary>セーブ日時 (ISO 8601。DateTime.ToString("o"))。空なら旧形式 (ファイル更新日時で代用)。</summary>
    public string savedAt;

    /// <summary>累計プレイ時間 (秒)。</summary>
    public double playTimeSeconds;

    /// <summary>章の表示名 (StageInfo.ChapterName)。</summary>
    public string chapterName;

    /// <summary>場所の表示名 (StageInfo の部屋名)。</summary>
    public string placeName;

    /// <summary>
    /// 復帰位置 (posX/posY) を使わずステージの入り口から始める (死亡リトライの引き継ぎ用。ファイルには書かない)。
    /// </summary>
    [NonSerialized] public bool useStageSpawn;

    /// <summary>
    /// このプレイの続き (つづきから・死亡リトライ) として適用するデータ。適用したプレイヤーは以後の進行を
    /// このプレイのセーブへ自動で書き込む (テストプレイの記録の再生などは false = セーブに触れない。ファイルには書かない)。
    /// </summary>
    [NonSerialized] public bool continuesRun;
}
