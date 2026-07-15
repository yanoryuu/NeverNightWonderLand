using R3;
using TMPro;
using UnityEngine;

/// <summary>
/// HUD の素材「糸」所持数表示。
/// </summary>
public class ThreadCountView : MonoBehaviour
{
    [Tooltip("参照するインベントリ")]
    [SerializeField] private PlayerItemInventory _inventory;

    [Tooltip("表示テキスト")]
    [SerializeField] private TMP_Text _label;

    private System.IDisposable _subscription;

    private void Start()
    {
        if (_inventory == null || _label == null)
        {
            Debug.LogError($"[{nameof(ThreadCountView)}] 参照が設定されていません。", this);
            return;
        }

        _subscription = _inventory.Thread.Subscribe(thread => _label.text = $"糸 x{thread}");
    }

    private void OnDestroy()
    {
        _subscription?.Dispose();
    }
}
