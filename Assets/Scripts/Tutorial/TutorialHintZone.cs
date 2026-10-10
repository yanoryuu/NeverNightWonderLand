using UnityEngine;

/// <summary>
/// プレイヤーが入ると操作ヒントを表示するゾーン (マルチシーン・チュートリアル用)。
/// TutorialManager の逐次シーケンスと違い、ゾーン単位で独立して動くため
/// シーンをまたぐチュートリアルに使える。案内文はキーボード用とパッド用を持ち、
/// InputDeviceTracker の最終入力デバイスに追従して出し直す。
/// 表示先 (TutorialMessageView) は UI シーン (StageUI) に常駐している
/// (GameField_old の旧ステージは自前の SceneUI のものを優先する。UIViewLookup 参照)。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TutorialHintZone : MonoBehaviour
{
    [Tooltip("キーボード操作時の案内文")]
    [SerializeField, TextArea] private string _keyboardText;

    [Tooltip("パッド操作時の案内文 (空ならキーボード用と同じ)")]
    [SerializeField, TextArea] private string _gamepadText;

    [Tooltip("表示先のメッセージビュー。未設定なら同じシーン → UI シーン (StageUI) の順に検索する")]
    [SerializeField] private TutorialMessageView _message;

    [Tooltip("ゾーンから出たらメッセージを隠すか")]
    [SerializeField] private bool _hideOnExit = true;

    private bool _playerInside;

    /// <summary>
    /// 表示先を解決する。UI シーンはステージより後にロードされることがある
    /// (ステージを直接開いて再生した場合) ので、Awake ではなく使う時に探す。
    /// </summary>
    private TutorialMessageView Message
    {
        get
        {
            if (_message == null)
                _message = UIViewLookup.Find<TutorialMessageView>(this);
            return _message;
        }
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnEnable()
    {
        InputDeviceTracker.OnChanged += OnDeviceChanged;
    }

    private void OnDisable()
    {
        InputDeviceTracker.OnChanged -= OnDeviceChanged;

        // ステージの入れ替えで消える時は、UI シーンに残る表示を片付ける
        if (_playerInside && _message != null)
            _message.Hide();
        _playerInside = false;
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
        if (_hideOnExit && Message != null)
            Message.Hide();
    }

    private void OnDeviceChanged(InputDeviceKind _)
    {
        if (_playerInside)
            Render();
    }

    private void Render()
    {
        var message = Message;
        if (message == null)
            return;

        var useGamepad = InputDeviceTracker.Current == InputDeviceKind.Gamepad
                         && !string.IsNullOrEmpty(_gamepadText);
        message.Show(useGamepad ? _gamepadText : _keyboardText);
    }
}
