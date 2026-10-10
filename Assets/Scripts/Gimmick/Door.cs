using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// レバーなどから開けられるドア。Open で上へスライドして当たり判定を無効化する。
/// 開き戸の絵 (_swingRenderer + _swingFrames) を設定した扉はスライドせず、その場でコマ送りして開いた扉を残す (<see cref="DoorSwing"/>)。
/// </summary>
public class Door : MonoBehaviour
{
    [Tooltip("開く時に上へスライドする距離 (units)")]
    [SerializeField] private float _slideDistance = 3f;

    [Tooltip("スライドにかける時間 (sec)")]
    [SerializeField] private float _slideDuration = 0.8f;

    [Tooltip("開き戸の絵を描く SpriteRenderer (任意)。_swingFrames と組で使う")]
    [SerializeField] private SpriteRenderer _swingRenderer;

    [Tooltip("開き戸のコマ (閉 → 開 の順)。設定するとスライドせず、_slideDuration 秒でコマ送りして開く")]
    [SerializeField] private Sprite[] _swingFrames = new Sprite[0];

    private bool _isOpen;

    public bool IsOpen => _isOpen;

    /// <summary>開き終わった時に発火する (チュートリアルの達成判定用)。</summary>
    public event Action OnOpened;

    public void Open() => Open(instant: false);

    /// <summary>
    /// ドアを開ける。instant はシーンロード時の復元用で、演出なしで即座に開いた状態にする。
    /// </summary>
    public void Open(bool instant)
    {
        if (_isOpen)
            return;

        _isOpen = true;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        if (DoorSwing.Has(_swingRenderer, _swingFrames))
        {
            if (instant)
            {
                DoorSwing.ShowOpen(_swingRenderer, _swingFrames);
                OnOpened?.Invoke();
                return;
            }

            DoorSwing.Play(_swingRenderer, _swingFrames, _slideDuration).OnComplete(() => OnOpened?.Invoke());
            return;
        }

        if (instant)
        {
            transform.position += Vector3.up * _slideDistance;
            // 開ききった扉は見えなくする (グレーボックスでは邪魔なだけのため)
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.enabled = false;
            OnOpened?.Invoke();
            return;
        }

        transform.DOMoveY(transform.position.y + _slideDistance, _slideDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => OnOpened?.Invoke());
    }
}
