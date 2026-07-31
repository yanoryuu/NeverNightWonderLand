using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// インタラクトで一度だけ作動するレバー。作動するとつながれたドアを開ける。
/// チュートリアルでは「インタラクト」の説明用ギミック。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LeverSwitch : MonoBehaviour, IInteractable
{
    [Tooltip("作動時に開けるドア (任意)")]
    [SerializeField] private Door _targetDoor;

    [Tooltip("作動時に立てる進行フラグ (任意, GameProgress)。設定すると作動状態が永続化され、" +
             "シーンをまたいだ FlagDoor の解錠に使える。例: BossDoor1")]
    [SerializeField] private string _progressFlag = "";

    [Tooltip("作動時の通知メッセージ (任意)")]
    [SerializeField] private string _activatedMessage = "";

    [Tooltip("プロンプトに表示するテキスト")]
    [SerializeField] private string _promptText = "レバーを引く";

    [Tooltip("プロンプト表示位置のオフセット (ワールド座標)")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1f, 0f);

    private SpriteRenderer _sprite;
    private bool _activated;

    /// <summary>作動した時に発火する (チュートリアルの達成判定用)。</summary>
    public event Action OnActivated;

    public string PromptText => _promptText;
    public bool CanInteract => !_activated;
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();

        // フラグ永続化レバーは、作動済みなら最初から作動状態で表示し、ドアも開けておく
        if (!string.IsNullOrEmpty(_progressFlag) && GameProgress.Has(_progressFlag))
        {
            _activated = true;
            transform.rotation = Quaternion.Euler(0f, 0f, -40f);
            _sprite.color = new Color(0.5f, 0.5f, 0.5f);

            if (_targetDoor != null)
                _targetDoor.Open(instant: true);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (_activated)
            return;

        _activated = true;

        // 作動の見た目: 傾けて色を落とす (仮素材)
        transform.DORotate(new Vector3(0f, 0f, -40f), 0.2f);
        _sprite.color = new Color(0.5f, 0.5f, 0.5f);

        if (!string.IsNullOrEmpty(_progressFlag))
            GameProgress.Set(_progressFlag);

        if (!string.IsNullOrEmpty(_activatedMessage))
            Notifier.Notify(_activatedMessage);

        if (_targetDoor != null)
            _targetDoor.Open();

        OnActivated?.Invoke();
    }
}
