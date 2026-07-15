using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// レバーなどから開けられるドア。Open で上へスライドして当たり判定を無効化する。
/// </summary>
public class Door : MonoBehaviour
{
    [Tooltip("開く時に上へスライドする距離 (units)")]
    [SerializeField] private float _slideDistance = 3f;

    [Tooltip("スライドにかける時間 (sec)")]
    [SerializeField] private float _slideDuration = 0.8f;

    private bool _isOpen;

    public bool IsOpen => _isOpen;

    /// <summary>開き終わった時に発火する (チュートリアルの達成判定用)。</summary>
    public event Action OnOpened;

    public void Open()
    {
        if (_isOpen)
            return;

        _isOpen = true;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        transform.DOMoveY(transform.position.y + _slideDistance, _slideDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => OnOpened?.Invoke());
    }
}
