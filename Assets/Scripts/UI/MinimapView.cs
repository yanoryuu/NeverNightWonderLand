using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// ミニマップ。専用カメラを実行時に生成して RenderTexture に描画し、HUD の RawImage に映す。
/// PlayerUI シーン (HUD プレハブ) に置けるよう、追従ターゲットは実行時に解決する
/// (PlayerRuntime 注入 → シリアライズ参照 → シーン検索 の順)。
/// 生成したカメラは自分と同じシーンに置き、ステージ遷移で破棄されないようにする。
/// </summary>
public class MinimapView : MonoBehaviour
{
    [Tooltip("ミニマップを表示する RawImage")]
    [SerializeField] private RawImage _image;

    [Tooltip("追従するターゲット (任意。未設定なら実行時にプレイヤーを解決する)")]
    [SerializeField] private Transform _target;

    [Tooltip("ミニマップカメラの表示範囲 (orthographicSize)")]
    [SerializeField] private float _viewSize = 14f;

    private Camera _camera;
    private RenderTexture _renderTexture;
    private PlayerRuntime _playerRuntime;
    private PlayerController _foundPlayer; // シーン検索のキャッシュ (破棄されたら引き直す)

    [Inject]
    public void Construct(PlayerRuntime playerRuntime)
    {
        _playerRuntime = playerRuntime;
    }

    /// <summary>追従ターゲット。Additive シーン運用でもシーンをまたいで解決できる。</summary>
    private Transform Target
    {
        get
        {
            if (_playerRuntime != null && _playerRuntime.Current.CurrentValue != null)
                return _playerRuntime.Current.CurrentValue.transform;

            if (_target != null)
                return _target;

            if (_foundPlayer == null)
                _foundPlayer = FindAnyObjectByType<PlayerController>();
            return _foundPlayer != null ? _foundPlayer.transform : null;
        }
    }

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
        // 生成先はアクティブシーン (=ステージ) になるため、自分と同じシーンへ移して
        // ステージ入替で破棄されないようにする
        SceneManager.MoveGameObjectToScene(camGo, gameObject.scene);

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
        var target = Target;
        if (_camera == null || target == null)
            return;

        _camera.transform.position = new Vector3(
            target.position.x, target.position.y + 3f, -10f);
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
