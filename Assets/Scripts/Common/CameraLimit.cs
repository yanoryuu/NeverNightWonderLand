using UnityEngine;

/// <summary>
/// ステージシーンに置くと、そのステージにいる間だけカメラの下限 (CameraFollow._minY) を
/// 上書きする。下層エリアを持つステージ (例: ピストンの回廊の落下攻撃穴の下) で使う。
/// ステージのアンロードで自動的に解除される。
/// </summary>
public class CameraLimit : MonoBehaviour
{
    [Tooltip("このステージでのカメラ位置の下限 (これより下へは追従しない)")]
    [SerializeField] private float _minY = 2f;

    private static CameraLimit _active;

    /// <summary>現在アクティブな上書き値 (無ければ null → CameraFollow の既定値を使う)。</summary>
    public static float? OverrideMinY => _active != null ? _active._minY : (float?)null;

    private void OnEnable()
    {
        _active = this;
    }

    private void OnDisable()
    {
        if (_active == this)
            _active = null;
    }
}
