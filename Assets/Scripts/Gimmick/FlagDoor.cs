using DG.Tweening;
using UnityEngine;

/// <summary>
/// 進行フラグ (GameProgress) が立っていると開く扉。
/// スピーカー破壊や永続レバーと組にして、シーンをまたぐ解錠に使う。
/// シーンロード時にフラグ済みなら即座に開いた状態になり、
/// 同シーン内でフラグが立った場合はスライド演出つきで開く。
/// </summary>
public class FlagDoor : MonoBehaviour
{
    [Tooltip("この進行フラグが立っていると開く")]
    [SerializeField] private string _flagId = "";

    [Tooltip("開く時に上へスライドする距離 (units)")]
    [SerializeField] private float _slideDistance = 4f;

    [Tooltip("スライドにかける時間 (sec)")]
    [SerializeField] private float _slideDuration = 0.8f;

    private bool _isOpen;

    private void Start()
    {
        if (GameProgress.Has(_flagId))
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
        if (flag == _flagId)
            Open(instant: false);
    }

    private void Open(bool instant)
    {
        if (_isOpen)
            return;

        _isOpen = true;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

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
