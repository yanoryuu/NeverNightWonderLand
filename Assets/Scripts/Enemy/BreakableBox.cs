using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 攻撃練習用の壊れる箱。防御値は持たず、HP/防御値どちらのダメージも合算して HP で受ける
/// (どのスタイルでも壊せるようにするため)。
/// _flagId を設定すると破壊状態が GameProgress で永続化され、再訪しても壊れたまま (隠し壁用)。
/// 空なら (ふつうの木箱) 壊れた事を <see cref="BrokenCrateRegistry"/> に記録し、部屋を出入りしても壊れたまま。
/// 拠点 (SavePoint) で休むと全部元に戻る (2026-10-04 PendingChanges 31)。
/// _coinId を設定するとコインの箱になる: 壊すとコイン (<see cref="CoinPiece"/>) が飛び出す。
/// コインを取った後 (GameProgress に _coinId) は箱ごと出現しない。取らずに部屋を出たら、次に来た時は箱は壊れたまま、
/// コインが箱のあった場所に置いてある (コインを失わない。休んだ後は箱が戻り、コインは中に戻る)。
/// プレイヤーの攻撃の当たり判定に入った箱は全部壊れる (PendingChanges 29。1 回に 1 つだけの制限はやめた)。
/// 隠し壁・積んだ箱は 1 セル = 1 個の BreakableBox (同じ _flagId を共有。1 個壊れるとフラグが立ち、再訪では全部消える)。
/// 壊れると破片が飛び散る (<see cref="BreakDebris"/>, PendingChanges 36): 木箱 = 木の破片、隠し壁 (ひび割れタイル) = 岩の破片。
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

    [Tooltip("破壊演出(縮小フェード)の時間 (sec)。破片が出せない時 (絵が無い・None) だけ使う")]
    [SerializeField] private float _breakFadeTime = 0.3f;

    [Tooltip("壊れた時の破片。Auto = 絵の名前から決める (crate → 木 / それ以外 (ひび割れ壁など) → 岩)")]
    [SerializeField] private DebrisType _debris = DebrisType.Auto;

    public enum DebrisType
    {
        Auto,
        None,
        Wood,
        Rock,
    }

    private SpriteRenderer _sprite;
    private Color _baseColor;
    private int _hp;
    private float _flashTimer;
    private bool _broken;
    private string _crateId; // 壊れた記録の ID (シーン名+名前+初期位置)。_flagId のある箱 (隠し壁) は使わない
    private bool _spawnLeftCoin; // 壊れたままのコインの箱: Start でコインだけ置いて箱は消す

    /// <summary>箱の元の色 (被弾フラッシュ前)。破片の色合わせに使う。</summary>
    public Color BaseColor => _baseColor;

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

        // ふつうの木箱: 拠点で休むまで壊れたまま (BrokenCrateRegistry)
        if (string.IsNullOrEmpty(_flagId))
        {
            _crateId = $"{gameObject.scene.name}:{name}:{transform.position.x:F1},{transform.position.y:F1}";
            if (BrokenCrateRegistry.IsBroken(_crateId))
            {
                if (string.IsNullOrEmpty(_coinId))
                {
                    Destroy(gameObject);
                    return;
                }

                // コインの箱を壊してコインを取らずに出た: 箱は壊れたまま、コインを箱の位置に置き直す (Start)
                _broken = true;
                _spawnLeftCoin = true;
                foreach (var col in GetComponentsInChildren<Collider2D>())
                    col.enabled = false;
                if (TryGetComponent<Rigidbody2D>(out var body))
                    body.simulated = false;
                GetComponent<SpriteRenderer>().enabled = false;
                return;
            }
        }

        _sprite = GetComponent<SpriteRenderer>();
        _baseColor = _sprite.color;
        _hp = _maxHp;
    }

    private void Start()
    {
        if (!_spawnLeftCoin)
            return;

        SpawnCoin(Vector2.zero);
        Destroy(gameObject);
    }

    private void Update()
    {
        if (_sprite != null && _flashTimer > 0f)
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
            Break(info);
    }

    private void Break(in DamageInfo info)
    {
        _broken = true;

        if (!string.IsNullOrEmpty(_flagId))
            GameProgress.Set(_flagId);
        else
            BrokenCrateRegistry.MarkBroken(_crateId);

        OnBroken?.Invoke();

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        if (TryGetComponent<Rigidbody2D>(out var rb))
            rb.simulated = false;

        SpawnCoin(new Vector2(UnityEngine.Random.Range(-1.2f, 1.2f), 7.5f));

        // 破片が飛び散る: 箱はその場で消える (破片が箱の代わり)。攻撃した側から離れる向きに飛ばす
        var away = info.Source != null ? (Vector2)(transform.position - info.Source.transform.position) : Vector2.zero;
        away.y = 0f;
        if (TryBurstDebris(away))
        {
            _sprite.enabled = false;
            Destroy(gameObject);
            return;
        }

        transform.DOScale(Vector3.zero, _breakFadeTime)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    private bool TryBurstDebris(Vector2 away)
    {
        var type = _debris;
        if (type == DebrisType.Auto)
        {
            var spriteName = _sprite.sprite != null ? _sprite.sprite.name : "";
            type = spriteName.Contains("crate") || spriteName.Contains("box") ? DebrisType.Wood : DebrisType.Rock;
        }

        if (type == DebrisType.None)
            return false;

        _sprite.color = _baseColor;
        return BreakDebris.Burst(_sprite, type == DebrisType.Wood ? BreakDebris.Kind.Wood : BreakDebris.Kind.Rock, away);
    }

    /// <summary>コインの箱ならコインを velocity で飛び出させる (まだ取っていない時だけ。zero = その場に置く)。</summary>
    private void SpawnCoin(Vector2 velocity)
    {
        if (string.IsNullOrEmpty(_coinId) || _coinPrefab == null || GameProgress.Has(_coinId))
            return;

        var coin = Instantiate(_coinPrefab, LootPiece.FreeSpawnPoint(transform.position), Quaternion.identity, transform.parent);
        coin.CoinId = _coinId;
        coin.Launch(velocity);
    }
}
