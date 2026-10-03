using UnityEngine;

namespace NeverNight.Stage
{
    /// <summary>
    /// 背景の視差スクロール。カメラ (Camera.main) の移動量に係数を掛けて追従する。
    /// 係数 1 = カメラに完全追従 (無限遠の空)、0 = ワールドに固定 (地形と同じ動き)。
    /// 配置位置 (Awake 時の position) は「カメラが _anchor にある時の位置」として扱う。
    /// カメラ (CameraFollow) の LateUpdate の後に動くよう実行順を遅らせている。
    /// 見た目は SpriteRenderer (Tiled 推奨) を同じ GameObject か子に置く。
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("カメラ追従の係数 (x, y)。1=カメラに張り付く、0=ワールド固定")]
        [SerializeField] private Vector2 _factor = new Vector2(0.9f, 1f);

        [Tooltip("基準となるカメラ位置 (ワールド)。カメラがここにある時、配置位置に表示される")]
        [SerializeField] private Vector2 _anchor;

        private Vector3 _basePosition;
        private Camera _camera;

        private void Awake()
        {
            _basePosition = transform.position;
        }

        private void LateUpdate()
        {
            if (_camera == null || !_camera.isActiveAndEnabled)
            {
                _camera = Camera.main;
                if (_camera == null)
                    return;
            }

            var delta = (Vector2)_camera.transform.position - _anchor;
            transform.position = new Vector3(
                _basePosition.x + delta.x * _factor.x,
                _basePosition.y + delta.y * _factor.y,
                _basePosition.z);
        }
    }
}
