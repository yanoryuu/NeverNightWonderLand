using UnityEngine;

/// <summary>
/// プレイヤーが入ると操作ヒントを表示するゾーン (マルチシーン・チュートリアル用)。
/// TutorialManager の逐次シーケンスと違い、ゾーン単位で独立して動くため
/// シーンをまたぐチュートリアルに使える。案内文はキーボード用とパッド用を持ち、
/// InputDeviceTracker の最終入力デバイスに追従して出し直す。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TutorialHintZone : MonoBehaviour
{
    [Tooltip("キーボード操作時の案内文")]
    [SerializeField, TextArea] private string _keyboardText;

    [Tooltip("パッド操作時の案内文 (空ならキーボード用と同じ)")]
    [SerializeField, TextArea] private string _gamepadText;

    [Tooltip("表示先のメッセージビュー。未設定ならシーンから検索する")]
    [SerializeField] private TutorialMessageView _message;

    [Tooltip("ゾーンから出たらメッセージを隠すか")]
    [SerializeField] private bool _hideOnExit = true;

    private bool _playerInside;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (_message == null)
            _message = FindFirstObjectByType<TutorialMessageView>();
    }

    private void OnEnable()
    {
        InputDeviceTracker.OnChanged += OnDeviceChanged;
    }

    private void OnDisable()
    {
        InputDeviceTracker.OnChanged -= OnDeviceChanged;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        _playerInside = true;
        Render();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_playerInside || other.GetComponentInParent<PlayerController>() == null)
            return;

        _playerInside = false;
        if (_hideOnExit && _message != null)
            _message.Hide();
    }

    private void OnDeviceChanged(InputDeviceKind _)
    {
        if (_playerInside)
            Render();
    }

    private void Render()
    {
        if (_message == null)
            return;

        var useGamepad = InputDeviceTracker.Current == InputDeviceKind.Gamepad
                         && !string.IsNullOrEmpty(_gamepadText);
        _message.Show(useGamepad ? _gamepadText : _keyboardText);
    }
}
