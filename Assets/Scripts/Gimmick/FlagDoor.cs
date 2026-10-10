using DG.Tweening;
using UnityEngine;

/// <summary>
/// 進行フラグ (GameProgress) が立っていると開く扉。
/// スピーカー破壊や永続レバーと組にして、シーンをまたぐ解錠に使う。
/// シーンロード時にフラグ済みなら即座に開いた状態になり、
/// 同シーン内でフラグが立った場合はスライド演出つきで開く。
/// _additionalFlags を設定すると、_flagId と合わせて全てのフラグが立った時だけ開く
/// (スピーカー3つで開く大扉など。空なら従来どおり _flagId だけで開く)。
/// 開き戸の絵 (_swingRenderer + _swingFrames) を設定した扉はスライドせず、その場でコマ送りして開いた扉を残す (<see cref="DoorSwing"/>)。
/// </summary>
public class FlagDoor : MonoBehaviour
{
    [Tooltip("この進行フラグが立っていると開く")]
    [SerializeField] private string _flagId = "";

    [Tooltip("追加で必要な進行フラグ (任意)。_flagId と合わせて全て立った時に開く")]
    [SerializeField] private string[] _additionalFlags = new string[0];

    [Tooltip("開く時に上へスライドする距離 (units)")]
    [SerializeField] private float _slideDistance = 4f;

    [Tooltip("スライドにかける時間 (sec)")]
    [SerializeField] private float _slideDuration = 0.8f;

    [Tooltip("開き戸の絵を描く SpriteRenderer (任意)。_swingFrames と組で使う")]
    [SerializeField] private SpriteRenderer _swingRenderer;

    [Tooltip("開き戸のコマ (閉 → 開 の順)。設定するとスライドせず、_slideDuration 秒でコマ送りして開く")]
    [SerializeField] private Sprite[] _swingFrames = new Sprite[0];

    private bool _isOpen;

    private void Start()
    {
        if (AllFlagsSet())
            Open(instant: true);
    }

    private void OnEnable()
    {
        GameProgress.Changed += OnFlagChanged;
    }

    private void OnDisable()
    {
        GameProgress.Changed -= OnFlagChanged;
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    private void OnFlagChanged(string flag)
    {
        if (IsRelevant(flag) && AllFlagsSet())
            Open(instant: false);
    }

    /// <summary>開くのに必要なフラグが全て立っているか (_flagId が空なら追加フラグだけで判定)。</summary>
    private bool AllFlagsSet()
    {
        var any = false;

        if (!string.IsNullOrEmpty(_flagId))
        {
            if (!GameProgress.Has(_flagId))
                return false;
            any = true;
        }

        if (_additionalFlags != null)
        {
            foreach (var flag in _additionalFlags)
            {
                if (string.IsNullOrEmpty(flag))
                    continue;
                if (!GameProgress.Has(flag))
                    return false;
                any = true;
            }
        }

        // フラグが1つも設定されていない扉は開かない (従来と同じ)
        return any;
    }

    private bool IsRelevant(string flag) =>
        flag == _flagId || (_additionalFlags != null && System.Array.IndexOf(_additionalFlags, flag) >= 0);

    private void Open(bool instant)
    {
        if (_isOpen)
            return;

        _isOpen = true;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        if (DoorSwing.Has(_swingRenderer, _swingFrames))
        {
            if (instant)
                DoorSwing.ShowOpen(_swingRenderer, _swingFrames);
            else
                DoorSwing.Play(_swingRenderer, _swingFrames, _slideDuration);
            return;
        }

        if (instant)
        {
            transform.position += Vector3.up * _slideDistance;
            // 開ききった扉は見えなくする (グレーボックスでは邪魔なだけのため)
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.enabled = false;
        }
        else
        {
            transform.DOMoveY(transform.position.y + _slideDistance, _slideDuration)
                .SetEase(Ease.InOutQuad);
        }
    }
}
