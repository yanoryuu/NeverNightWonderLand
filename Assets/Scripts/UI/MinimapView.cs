using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ミニマップ。専用カメラを実行時に生成して RenderTexture に描画し、HUD の RawImage に映す。
/// </summary>
public class MinimapView : MonoBehaviour
{
    [Tooltip("ミニマップを表示する RawImage")]
    [SerializeField] private RawImage _image;

    [Tooltip("追従するターゲット (プレイヤー)")]
    [SerializeField] private Transform _target;

    [Tooltip("ミニマップカメラの表示範囲 (orthographicSize)")]
    [SerializeField] private float _viewSize = 14f;

    private Camera _camera;
    private RenderTexture _renderTexture;

    private void Start()
    {
        if (_image == null)
        {
            Debug.LogError($"[{nameof(MinimapView)}] RawImage が設定されていません。", this);
            enabled = false;
            return;
        }

        _renderTexture = new RenderTexture(256, 256, 16);

        var camGo = new GameObject("MinimapCamera");
        _camera = camGo.AddComponent<Camera>();
        _camera.orthographic = true;
        _camera.orthographicSize = _viewSize;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        _camera.targetTexture = _renderTexture;
        camGo.AddComponent<AudioListener>().enabled = false; // AudioListener の重複を避ける

        _image.texture = _renderTexture;
    }

    private void LateUpdate()
    {
        if (_camera == null || _target == null)
            return;

        _camera.transform.position = new Vector3(
            _target.position.x, _target.position.y + 3f, -10f);
    }

    private void OnDestroy()
    {
        if (_camera != null)
            _camera.targetTexture = null;

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }
}
