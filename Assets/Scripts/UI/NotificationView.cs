using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 取得通知・案内・簡易会話のトースト表示。Notifier の発行を購読してキュー順に表示する。
/// ポーズ中も動くよう unscaled 時間で駆動する。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class NotificationView : MonoBehaviour
{
    private const float ShowDuration = 2.4f;
    private const float FadeDuration = 0.25f;

    [Tooltip("メッセージ本文")]
    [SerializeField] private TMP_Text _label;

    private CanvasGroup _group;
    private readonly Queue<string> _queue = new();
    private float _timer;
    private bool _showing;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
    }

    private void OnEnable()
    {
        Notifier.OnNotify += Enqueue;
    }

    private void OnDisable()
    {
        Notifier.OnNotify -= Enqueue;
    }

    private void Enqueue(string message)
    {
        _queue.Enqueue(message);
    }

    private void Update()
    {
        var dt = Time.unscaledDeltaTime;

        if (_showing)
        {
            _timer -= dt;

            // フェードイン/アウト
            if (_timer > ShowDuration - FadeDuration)
                _group.alpha = Mathf.Clamp01((ShowDuration - _timer) / FadeDuration);
            else if (_timer < FadeDuration)
                _group.alpha = Mathf.Clamp01(_timer / FadeDuration);
            else
                _group.alpha = 1f;

            if (_timer <= 0f)
            {
                _showing = false;
                _group.alpha = 0f;
            }

            return;
        }

        if (_queue.Count > 0)
        {
            if (_label != null)
                _label.text = _queue.Dequeue();
            _timer = ShowDuration;
            _showing = true;
        }
    }
}
