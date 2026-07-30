using DG.Tweening;
using UnityEngine;

/// <summary>
/// 移動スキル (落下攻撃/大ジャンプ/横突進) の衝撃でのみ破壊できるブロック。
/// ハサミ強化壁 (UpgradeWall) とは別系統で、通常攻撃・裁断ではダメージが通らず
/// ヒント通知だけを返す。破壊はスキルステートが <see cref="TryBreak"/> を直接呼ぶ。
/// 破壊状態は GameProgress で永続化できる (_flagId 空なら再入場で復活)。
/// Ground レイヤーに置くと足場・攻撃対象として振る舞う。
/// グレーボックス色: 落下=橙 / 大ジャンプ=空色 / 突進=紫。
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
    private float _hintCooldown; // ヒント通知の連発防止

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

    private void Update()
    {
        if (_hintCooldown > 0f)
            _hintCooldown -= Time.deltaTime;
    }

    /// <summary>通常攻撃はダメージが通らず、ヒントを返すだけ。</summary>
    public void TakeDamage(in DamageInfo info)
    {
        if (_broken || _hintCooldown > 0f)
            return;

        var message = string.IsNullOrEmpty(_lockedMessage)
            ? $"刃は通らない。「{_requiredSkill.DisplayName()}」の衝撃なら砕けそうだ"
            : _lockedMessage;
        Notifier.Notify(message);
        _hintCooldown = 2f;
    }

    /// <summary>
    /// スキルの衝撃で破壊を試みる。要求スキルと一致した時のみ砕けて true を返す。
    /// (スキルステートから呼ばれる)
    /// </summary>
    public bool TryBreak(PlayerSkill skill)
    {
        if (_broken || skill != _requiredSkill)
            return false;

        _broken = true;

        if (!string.IsNullOrEmpty(_flagId))
            GameProgress.Set(_flagId);

        DamageEvents.Raise(transform.position, 1, DamageEvents.Kind.Hp);

        // すぐに当たりを消してプレイヤーが突き抜けられるようにする (連鎖破壊)
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        transform.DOScale(Vector3.zero, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
        return true;
    }
}
