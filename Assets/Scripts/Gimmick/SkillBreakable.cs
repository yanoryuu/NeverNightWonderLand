using DG.Tweening;
using UnityEngine;

/// <summary>
/// 移動スキル (落下攻撃/大ジャンプ/横突進) の衝撃でのみ破壊できるブロック。
/// ハサミ強化壁 (UpgradeWall) とは別系統で、通常攻撃・裁断ではダメージが通らず
/// ヒント通知だけを返す。破壊はスキルステートが <see cref="TryBreak"/> を直接呼ぶ。
/// 破壊状態は GameProgress で永続化できる (_flagId 空なら再入場で復活)。
/// Ground レイヤーに置くと足場・攻撃対象として振る舞う。
/// Area1 のブロックは 1 セル = 1 個 (同じ _flagId を共有)。スキルは当たったセルだけを砕く = 体の通る穴が開く。
/// 1 個砕けるとフラグが立ち、再訪では同じフラグのセルが全部消えている。
/// グレーボックス色: 落下=橙 / 大ジャンプ=空色 / 突進=紫。
/// 砕けると破片が飛び散る (<see cref="BreakDebris"/>, PendingChanges 36): 落下=橙の金具と石 / 大ジャンプ・突進=岩の破片をブロックの色で。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkillBreakable : MonoBehaviour, IDamageable
{
    [Tooltip("破壊に必要な移動スキル")]
    [SerializeField] private PlayerSkill _requiredSkill = PlayerSkill.GroundSlam;

    [Tooltip("破壊状態の永続化フラグ (GameProgress)。空なら再入場で復活する")]
    [SerializeField] private string _flagId = "";

    [Tooltip("通常攻撃した時のヒント (空ならスキル名から自動生成)")]
    [SerializeField] private string _lockedMessage = "";

    private bool _broken;

    // ヒント通知の連発防止 (全ブロック共通。1 セル = 1 個に分けたので、隣のセルで出し直さない)。
    // スキルで砕いた直後 (落下攻撃の着地の範囲攻撃が隣のセルに当たる等) も出さない
    private static float s_nextHintTime;

    /// <summary>破壊に必要なスキル (デバッグ表示用)。</summary>
    public PlayerSkill RequiredSkill => _requiredSkill;

    private void Awake()
    {
        if (!string.IsNullOrEmpty(_flagId) && GameProgress.Has(_flagId))
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        transform.DOKill(); // シーン遷移などで破壊演出の途中に消えても警告を出さない
    }

    /// <summary>通常攻撃はダメージが通らず、ヒントを返すだけ。</summary>
    public void TakeDamage(in DamageInfo info)
    {
        if (_broken || Time.time < s_nextHintTime)
            return;

        var message = string.IsNullOrEmpty(_lockedMessage)
            ? $"刃は通らない。「{_requiredSkill.DisplayName()}」の衝撃なら砕けそうだ"
            : _lockedMessage;
        Notifier.Notify(message);
        s_nextHintTime = Time.time + 2f;
    }

    /// <summary>
    /// スキルの衝撃で破壊を試みる。要求スキルと一致した時のみ砕けて true を返す。
    /// (スキルステートから呼ばれる。impactFrom = 当てた位置 (プレイヤー)。破片を離れる向きに飛ばす)
    /// </summary>
    public bool TryBreak(PlayerSkill skill, Vector2? impactFrom = null)
    {
        if (_broken || skill != _requiredSkill)
            return false;

        _broken = true;
        s_nextHintTime = Mathf.Max(s_nextHintTime, Time.time + 1f);

        if (!string.IsNullOrEmpty(_flagId))
            GameProgress.Set(_flagId);

        DamageEvents.Raise(transform.position, 1, DamageEvents.Kind.Hp);

        // すぐに当たりを消してプレイヤーが突き抜けられるようにする (連鎖破壊)
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // 破片が飛び散る (ブロックはその場で消える)。落下攻撃は上と横へ、当てた側から離れる向き
        var kind = _requiredSkill switch
        {
            PlayerSkill.GroundSlam => BreakDebris.Kind.Slam,
            PlayerSkill.ChargeRush => BreakDebris.Kind.Rush,
            _ => BreakDebris.Kind.Super,
        };
        // 当てた側 (プレイヤー) から離れる向きへ寄せる。下向きには寄せない (破片は上へ飛び出してから落ちる)
        var away = impactFrom.HasValue ? (Vector2)transform.position - impactFrom.Value : Vector2.zero;
        away.y = Mathf.Max(0f, away.y);
        if (TryGetComponent<SpriteRenderer>(out var sprite) && BreakDebris.Burst(sprite, kind, away))
        {
            sprite.enabled = false;
            Destroy(gameObject);
            return true;
        }

        transform.DOScale(Vector3.zero, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
        return true;
    }
}
