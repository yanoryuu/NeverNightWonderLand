using UnityEngine;

/// <summary>
/// ターゲットを滑らかに追従するシンプルなカメラ。
/// チュートリアルシーンをスクリプトから確実に構築するために Cinemachine の代わりに使う
/// (本編シーンでは Cinemachine に置き換えてよい)。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("追従するターゲット (プレイヤー)")]
    [SerializeField] private Transform _target;

    [Tooltip("ターゲットからのオフセット")]
    [SerializeField] private Vector3 _offset = new(2f, 2f, -10f);

    [Tooltip("追従の滑らかさ (小さいほど機敏)")]
    [SerializeField] private float _smoothTime = 0.15f;

    [Tooltip("カメラ位置の下限 (これより下へは追従しない)")]
    [SerializeField] private float _minY = 2f;

    private Vector3 _velocity;

    /// <summary>
    /// ターゲット位置へ即座に移動する (ステージ遷移でプレイヤーがワープした時用。
    /// 暗転中に呼ぶことで、カメラが移動していく様子を見せない)。
    /// </summary>
    public void SnapToTarget()
    {
        if (_target == null)
            return;

        var desired = _target.position + _offset;
        desired.y = Mathf.Max(desired.y, _minY);
        transform.position = desired;
        _velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        var desired = _target.position + _offset;
        desired.y = Mathf.Max(desired.y, _minY);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, _smoothTime);
    }
}
