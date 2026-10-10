using System;
using UnityEngine;

/// <summary>
/// 中ボスが守る破壊可能なスピーカー。破壊すると進行フラグが立ち、
/// ボス街道 (回転軸の内部) の入口を塞ぐ FlagDoor が1枚開く。
/// 破壊状態は GameProgress で永続化される。壊れた瞬間は <see cref="SpeakerBreakEffect"/> (カメラの寄せ・スロー・爆発・揺れ)、
/// その後は鳴っている絵を消して煙を出し続ける (壊れた絵はシーン側の FlagActivator が同じフラグで出す)。
/// 破壊済みで入った時も、鳴っている絵を消して煙だけ出した状態で始まる。
/// HP/防御値どちらのダメージも合算して受ける (BreakableBox と同じ受け方)。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableSpeaker : MonoBehaviour, IDamageable
{
    [Tooltip("破壊時に立てる進行フラグ (GameProgress)。例: SpeakerGate")]
    [SerializeField] private string _flagId = "";

    [Tooltip("耐久値")]
    [SerializeField] private int _maxHp = 3;

    [Tooltip("破壊時の通知メッセージ")]
    [SerializeField] private string _breakMessage = "スピーカーを破壊した! どこかで扉が開いた音がする";

    [Tooltip("被弾フラッシュの時間 (sec)")]
    [SerializeField] private float _hitFlashTime = 0.1f;

    [Tooltip("爆発・煙のパーティクルの素材 (URP の ParticlesUnlit。ビルダーの SpeakerAt が設定)。空ならパーティクルは出さない")]
    [SerializeField] private Material _particleMaterial;

    [Tooltip("壊れた後の煙の出口 (絵の下端からの高さ = 絵の高さに対する割合)")]
    [SerializeField] private float _smokeHeight = 0.75f;

    private SpriteRenderer _sprite;
    private Color _baseColor;
    private int _hp;
    private float _flashTimer;
    private bool _broken;

    /// <summary>破壊された時に発火する。</summary>
    public event Action OnBroken;

    /// <summary>壊れているか (この場で壊れた、または破壊済みフラグが立っている)。</summary>
    public bool IsBroken => _broken || (!string.IsNullOrEmpty(_flagId) && GameProgress.Has(_flagId));

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _baseColor = _sprite.color;
        _hp = _maxHp;
    }

    private void OnEnable()
    {
        GameProgress.Changed += OnFlagChanged;
    }

    private void OnDisable()
    {
        GameProgress.Changed -= OnFlagChanged;
    }

    private void Start()
    {
        // 破壊済み: 鳴っている絵と当たりを消し、煙だけ出し続ける (壊れた絵は FlagActivator が出す)
        ApplyBrokenFlag();
    }

    // セーブ復帰のフラグ復元はステージ有効化より後に来ることがあるので、後から立った時もその場で壊れた状態にする
    private void OnFlagChanged(string flag)
    {
        if (flag == _flagId)
            ApplyBrokenFlag();
    }

    private void ApplyBrokenFlag()
    {
        if (_broken || string.IsNullOrEmpty(_flagId) || !GameProgress.Has(_flagId))
            return;
        _broken = true;
        _hp = 0;
        ShowBroken(true);
    }

    private void Update()
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && !_broken)
                _sprite.color = _baseColor;
        }
    }

    public void TakeDamage(in DamageInfo info)
    {
        if (_broken)
            return;

        var damage = info.HpDamage + info.GuardDamage;
        if (damage <= 0)
            return;

        var applied = Mathf.Min(_hp, damage);
        _hp -= applied;

        DamageEvents.Raise(info.HitPoint, applied, DamageEvents.Kind.Hp);

        _sprite.color = Color.white;
        _flashTimer = _hitFlashTime;

        if (_hp <= 0)
        {
            var away = info.Source != null ? (Vector2)(transform.position - info.Source.transform.position) : Vector2.zero;
            Break(null, new Vector2(Mathf.Sign(away.x) * (away.x == 0f ? 0f : 1f), 0f));
        }
    }

    /// <summary>
    /// 耐久値に関係なくその場で壊す (ボスの流れ弾など、プレイヤー以外が壊す演出用)。
    /// フラグ・通知・<see cref="OnBroken"/> はプレイヤーが壊した時と同じ。message を渡すと通知文だけ差し替える (null/空なら既定の通知)。
    /// 既に壊れていれば false。
    /// </summary>
    public bool ForceBreak(string message = null)
    {
        if (_broken)
            return false;

        DamageEvents.Raise(transform.position, Mathf.Max(1, _hp), DamageEvents.Kind.Hp);
        _hp = 0;
        Break(message);
        return true;
    }

    private void Break(string messageOverride = null, Vector2 away = default)
    {
        _broken = true;
        _flashTimer = 0f;
        _sprite.color = _baseColor;

        // 演出 (寄せ・スロー・爆発・破片) は鳴っている絵を基準に出すので、絵を消す前に呼ぶ
        SpeakerBreakEffect.Play(_sprite, _particleMaterial, away);
        ShowBroken(false);

        // フラグを立てると壊れた絵 (FlagActivator) が出て、大扉の灯・ジップライン等の連動も動く
        if (!string.IsNullOrEmpty(_flagId))
            GameProgress.Set(_flagId);

        Notifier.Notify(string.IsNullOrEmpty(messageOverride) ? _breakMessage : messageOverride);
        OnBroken?.Invoke();
    }

    /// <summary>鳴っている絵・コマ送り・当たりを消し、煙を出す。revisit = 再訪時 (煙を最初から立ち上らせる)。</summary>
    private void ShowBroken(bool revisit)
    {
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        if (TryGetComponent<SpriteFlipbook>(out var flipbook))
            flipbook.enabled = false;

        var bounds = _sprite.bounds;
        var top = new Vector2(bounds.center.x, bounds.min.y + bounds.size.y * _smokeHeight);
        var layer = _sprite.sortingLayerID;
        var order = _sprite.sortingOrder;
        _sprite.enabled = false;

        if (revisit)
            SpeakerBreakEffect.StartSmoke(transform, top, bounds.size.x, _particleMaterial, layer, order, true);
        else
            StartCoroutine(StartSmokeLater(top, bounds.size.x, layer, order));
    }

    private System.Collections.IEnumerator StartSmokeLater(Vector2 top, float width, int layer, int order)
    {
        yield return new WaitForSecondsRealtime(SpeakerBreakEffect.SmokeDelay);
        SpeakerBreakEffect.StartSmoke(transform, top, width, _particleMaterial, layer, order, false);
    }
}
