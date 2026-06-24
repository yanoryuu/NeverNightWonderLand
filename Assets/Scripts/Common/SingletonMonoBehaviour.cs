using UnityEngine;

public class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
{
    #region Fields

    private static T _instance;
    private static readonly object _lock = new();
    private static bool _applicationIsQuitting;

    #endregion

    #region Properties

    public static bool HasInstance => _instance != null;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance of '{typeof(T)}' already destroyed on application quit. Returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance != null)
                    return _instance;

                var instances = FindObjectsByType<T>(FindObjectsSortMode.None);

                if (instances.Length > 1)
                {
                    Debug.LogError($"[Singleton] Multiple instances of {typeof(T)} found.");
                    _instance = instances[0];
                    return _instance;
                }

                if (instances.Length == 1)
                {
                    _instance = instances[0];
                    return _instance;
                }

                var go = new GameObject($"{typeof(T)} (Singleton)");
                _instance = go.AddComponent<T>();
                DontDestroyOnLoad(go);
                Debug.Log($"[Singleton] An instance of {typeof(T)} was created automatically.");

                return _instance;
            }
        }
    }

    #endregion

    #region Unity Callbacks

    protected virtual void Awake()
    {
        if (_applicationIsQuitting)
            return;

        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            OnAwakeSingleton();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _applicationIsQuitting = true;
    }

    #endregion

    #region Virtual Methods

    /// <summary>
    /// シングルトンインスタンスとして登録された直後に呼ばれる。
    /// 継承先で初期化処理を行いたい場合はオーバーライドする。
    /// </summary>
    protected virtual void OnAwakeSingleton() { }

    #endregion
}
