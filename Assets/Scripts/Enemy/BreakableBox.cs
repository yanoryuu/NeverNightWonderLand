using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 攻撃練習用の壊れる箱。防御値は持たず、HP/防御値どちらのダメージも合算して HP で受ける
/// (どのスタイルでも壊せるようにするため)。
/// _flagId を設定すると破壊状態が GameProgress で永続化され、再訪しても壊れたまま (隠し壁用)。
/// 空なら従来どおりシーンに入り直すたびに復活する。
/// _coinId を設定するとコインの箱になる: 壊すとコイン (<see cref="CoinPiece"/>) が飛び出す。
/// コインを取った後 (GameProgress に _coinId) は箱ごと出現しない。取らずに部屋を出たら、次に来た時は箱もコインも元どおり。
/// プレイヤーの 1 回の攻撃で壊れる箱は 1 つだけ (PlayerController.PerformAttackHit が一番近い箱を選ぶ)。
/// 隠し壁・積んだ箱は 1 セル = 1 個の BreakableBox (同じ _flagId を共有。1 個壊れるとフラグが立ち、再訪では全部消える)。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableBox : MonoBehaviour, IDamageable
{
    [Tooltip("耐久値。HPダメージ+防御値ダメージの合算で削られる")]
    [SerializeField] private int _maxHp = 1;

    [Tooltip("破壊状態の永続化フラグ (GameProgress)。空なら再入場で復活する。例: A1_Hidden_U2")]
    [SerializeField] private string _flagId = "";

    [Tooltip("コインの箱: 中のコインの永続化 Id (GameProgress)。空ならコインは入っていない。例: A1_Coin_T3c_1")]
    [SerializeField] private string _coinId = "";

    [Tooltip("コインのプレハブ (_coinId を設定した箱だけ使う)")]
    [SerializeField] private CoinPiece _coinPrefab;

    [Tooltip("被弾フラッシュの時間 (sec)")]
    [SerializeField] private float _hitFlashTime = 0.1f;

    [Tooltip("破壊演出(縮小フェード)の時間 (sec)")]
    [SerializeField] private float _breakFadeTime = 0.3f;

    private SpriteRenderer _sprite;
    private Color _baseColor;
    private int _hp;
    private float _flashTimer;
    private bool _broken;

    /// <summary>破壊された時に発火する (チュートリアルの達成判定用)。</summary>
    public event Action OnBroken;

    private void Awake()
    {
        // 破壊済み (永続化フラグあり) なら出現させない
        if (!string.IsNullOrEmpty(_flagId) && GameProgress.Has(_flagId))
        {
            Destroy(gameObject);
            return;
        }

        // コインを取った後の箱は出さない (一度取ったら二度と出ない)
        if (!string.IsNullOrEmpty(_coinId) && GameProgress.Has(_coinId))
        {
            Destroy(gameObject);
            return;
        }

        _sprite = GetComponent<SpriteRenderer>();
        _baseColor = _sprite.color;
        _hp = _maxHp;
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
            Break();
    }

    private void Break()
    {
        _broken = true;

        if (!string.IsNullOrEmpty(_flagId))
            GameProgress.Set(_flagId);

        OnBroken?.Invoke();

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        if (TryGetComponent<Rigidbody2D>(out var rb))
            rb.simulated = false;

        SpawnCoin();

        transform.DOScale(Vector3.zero, _breakFadeTime)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    /// <summary>コインの箱ならコインを上へ飛び出させる (まだ取っていない時だけ)。</summary>
    private void SpawnCoin()
    {
        if (string.IsNullOrEmpty(_coinId) || _coinPrefab == null || GameProgress.Has(_coinId))
            return;

        var coin = Instantiate(_coinPrefab, LootPiece.FreeSpawnPoint(transform.position), Quaternion.identity, transform.parent);
        coin.CoinId = _coinId;
        coin.Launch(new Vector2(UnityEngine.Random.Range(-1.2f, 1.2f), 7.5f));
    }
}
