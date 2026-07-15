using TMPro;
using UnityEngine;

/// <summary>
/// インタラクトプロンプト (「[E] レバーを引く」)。
/// PlayerInteractor の現在対象を毎フレーム確認し、対象の PromptAnchor 位置に表示する。
/// ワールド空間の TextMeshPro を想定。シーンに1つ置く。
/// </summary>
public class InteractPromptView : MonoBehaviour
{
    [Tooltip("参照するプレイヤーのインタラクタ")]
    [SerializeField] private PlayerInteractor _interactor;

    [Tooltip("プロンプトのテキスト")]
    [SerializeField] private TMP_Text _label;

    [Tooltip("表示の親 (表示切替の対象)")]
    [SerializeField] private GameObject _root;

    private void Start()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_interactor == null || _root == null)
            return;

        var current = _interactor.Current.CurrentValue;
        var visible = current != null && current.CanInteract;
        _root.SetActive(visible);

        if (visible)
        {
            transform.position = current.PromptAnchor;
            if (_label != null)
            {
                // 最後に使ったデバイスに合わせてボタン表記を切り替える
                var button = InputDeviceTracker.Current == InputDeviceKind.Gamepad ? "[↑]" : "[E]";
                _label.text = $"{button} {current.PromptText}";
            }
        }
    }
}
