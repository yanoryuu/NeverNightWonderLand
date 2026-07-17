using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ステージシーンから UI シーン群 (HUD の PlayerUI、ポーズの PauseUI など) を
/// Additive でロードするブートストラップ。既にロード済みのシーンは何もしないので、
/// エディタでどのシーンから再生しても UI が乗る。
/// UI シーン側の LifetimeScope はルート (GameLifetimeScope) に自動接続されるため、
/// シーンをまたいだ参照設定は不要。
/// </summary>
public class UISceneBootstrap : MonoBehaviour
{
    [Tooltip("Additive でロードする UI シーン名 (Build Settings に登録しておくこと)")]
    [SerializeField] private string[] _uiSceneNames = { "PlayerUI", "PauseUI", "HomeUI", "GameOverUI", "ResultUI" };

    private void Awake()
    {
        if (_uiSceneNames == null)
            return;

        foreach (var sceneName in _uiSceneNames)
        {
            if (string.IsNullOrEmpty(sceneName))
                continue;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
                continue;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"[{nameof(UISceneBootstrap)}] シーン '{sceneName}' が Build Settings に見つかりません。", this);
                continue;
            }

            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }
}
