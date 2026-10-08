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

    [Tooltip("レバーの柄 (任意)。設定すると本体ではなく柄だけを傾け (_handleOffAngle → _handleOnAngle)、色は変えない")]
    [SerializeField] private Transform _handle;

    [Tooltip("作動前の柄の傾き (度, 反時計回りが正)")]
    [SerializeField] private float _handleOffAngle = 30f;

    [Tooltip("作動後の柄の傾き (度)")]
    [SerializeField] private float _handleOnAngle = -30f;

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
        if (_handle != null)
            _handle.localRotation = Quaternion.Euler(0f, 0f, _handleOffAngle);

        // フラグ永続化レバーは、作動済みなら最初から作動状態で表示し、ドアも開けておく
        if (!string.IsNullOrEmpty(_progressFlag) && GameProgress.Has(_progressFlag))
        {
            _activated = true;
            ShowActivated(instant: true);

            if (_targetDoor != null)
                _targetDoor.Open(instant: true);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (_activated)
            return;

        _activated = true;

        ShowActivated(instant: false);

        if (!string.IsNullOrEmpty(_progressFlag))
            GameProgress.Set(_progressFlag);

        if (!string.IsNullOrEmpty(_activatedMessage))
            Notifier.Notify(_activatedMessage);

        if (_targetDoor != null)
            _targetDoor.Open();

        OnActivated?.Invoke();
    }

    /// <summary>作動の見た目。柄があれば柄を傾ける。無ければ本体を傾けて色を落とす (仮素材)。</summary>
    private void ShowActivated(bool instant)
    {
        if (_handle != null)
        {
            var rot = new Vector3(0f, 0f, _handleOnAngle);
            if (instant)
                _handle.localRotation = Quaternion.Euler(rot);
            else
                _handle.DOLocalRotate(rot, 0.25f).SetEase(Ease.OutBack).SetLink(_handle.gameObject);
            return;
        }

        if (instant)
            transform.rotation = Quaternion.Euler(0f, 0f, -40f);
        else
            transform.DORotate(new Vector3(0f, 0f, -40f), 0.2f);
        _sprite.color = new Color(0.5f, 0.5f, 0.5f);
    }
}
