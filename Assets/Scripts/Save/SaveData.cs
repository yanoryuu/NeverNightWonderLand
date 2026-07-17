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

    /// <summary>アイテムの ID (ItemDefinition のアセット名)。itemCounts と対になる。</summary>
    public string[] itemIds;

    /// <summary>各アイテムの所持数 (itemIds と同じ並び)。</summary>
    public int[] itemCounts;

    /// <summary>スロット (下/左/右) にセットされたアイテムの ID (空は "")。</summary>
    public string[] slotItemIds;

    /// <summary>素材「糸」の所持数。</summary>
    public int thread;

    /// <summary>装備中の近接攻撃 (□) の ID (AttackDefinition のアセット名。空は未装備)。</summary>
    public string equippedMeleeId;

    /// <summary>装備中の特殊攻撃 (△) の ID (空は未装備)。</summary>
    public string equippedSpecialId;

    /// <summary>解放済みの攻撃方法の ID。</summary>
    public string[] unlockedAttackIds;
}
