using UnityEngine;

/// <summary>アイテム使用時のモーション種別。</summary>
public enum ItemUseMotion
{
    /// <summary>投擲/設置モーション (ItemThrowState)。UseDelay 経過時に Activate が呼ばれる。</summary>
    Throw,

    /// <summary>突進モーション (ItemDashState)。パラメータは DashItemDefinition が持つ。</summary>
    Dash,
}

/// <summary>
/// 携帯アイテムの定義 (抽象 ScriptableObject)。
/// 表示名・最大所持数・使用モーション・効果の発動をアイテム自身が持ち、
/// 新しいアイテムはこのクラスを継承したクラス+アセットを追加するだけで増やせる。
/// プレイヤー側 (PlayerItemInventory / ItemThrowState) はこの抽象にのみ依存する。
/// </summary>
public abstract class ItemDefinition : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("表示名")]
    [SerializeField] private string _displayName = "アイテム";

    [Tooltip("説明 (メニュー表示用)")]
    [SerializeField, TextArea] private string _description = "";

    [Tooltip("最大所持数")]
    [SerializeField] private int _maxCount = 10;

    [Tooltip("アイコンの色 (仮素材)")]
    [SerializeField] private Color _iconColor = Color.white;

    [Header("使用モーション")]
    [Tooltip("使用モーションの継続時間 (sec)")]
    [SerializeField] private float _useDuration = 0.25f;

    [Tooltip("モーション開始から効果発動までの時間 (sec)")]
    [SerializeField] private float _useDelay = 0.08f;

    public string DisplayName => _displayName;
    public string Description => _description;
    public int MaxCount => _maxCount;
    public Color IconColor => _iconColor;
    public float UseDuration => _useDuration;
    public float UseDelay => _useDelay;

    /// <summary>このアイテムの使用モーション。</summary>
    public abstract ItemUseMotion Motion { get; }

    /// <summary>
    /// 効果を発動する (Throw 系: ItemThrowState が UseDelay 経過時に呼ぶ)。
    /// origin は壁めり込み補正済みの発射位置、facing は向き (1=右, -1=左)。
    /// Dash 系は ItemDashState がパラメータを直接参照するため、通常オーバーライド不要。
    /// </summary>
    public virtual void Activate(PlayerController player, Vector2 origin, int facing) { }

    /// <summary>曲射の初速を計算する共通ヘルパー。</summary>
    protected static Vector2 ArcVelocity(float speed, float angleDeg, int facing)
    {
        var rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(facing * Mathf.Cos(rad) * speed, Mathf.Sin(rad) * speed);
    }
}
