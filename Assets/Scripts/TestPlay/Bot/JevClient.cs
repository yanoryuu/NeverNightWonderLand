#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Jev (TypeSafe System One) の 1 回分の回答。</summary>
public class JevAnswer
{
    public bool Ok;
    public string Error;                 // 失敗時の理由 (キーは含めない)
    public string Choice;                // "action" の選択肢
    public float Confidence;
    public Dictionary<string, float> Probabilities = new();
    public Dictionary<string, float> Nouls = new();   // noul の質問キー → yes の確率
    public int InputTokens;
    public int OutputTokens;
    public float LatencySeconds;         // 実時間 (再試行の待ちを含む)
    public int Attempts;
}

/// <summary>
/// TypeSafe の Jev を呼ぶクライアント (エディタ/Development Build のみ)。UnityWebRequest を毎フレーム Tick で見張る
/// (メインスレッドのまま・timeScale 0 でも進む)。同時に 1 リクエストだけ。
///
/// API キー: 環境変数 TYPESAFE_API_KEY (プロセス → Windows のユーザー環境変数) → 追跡しないローカルファイル
/// (エディタ: UserSettings/TypeSafeApiKey.txt、Development Build: persistentDataPath/TypeSafeApiKey.txt)。
/// キーはログ・レポート・画面に一切出さない。
/// 401/422 は再試行しても直らないので即失敗。429/529/5xx/通信エラーは指数バックオフ (0.5, 1, 2, 4 秒) で最大 4 回再試行。
/// </summary>
public class JevClient
{
    public const string Endpoint = "https://api.typesafe.ai/v1/systemone";
    public const string EnvVar = "TYPESAFE_API_KEY";
    public const string KeyFileName = "TypeSafeApiKey.txt";

    public string Model = "jev-latest";
    public float TimeoutSeconds = 10f;
    public int MaxRetries = 4;

    // 集計 (レポート用)
    public int Calls;          // 成功した呼び出し
    public int Failures;       // 最終的に失敗した呼び出し
    public int Retries;
    public long InputTokens;
    public long OutputTokens;
    public float TotalLatency;
    public string LastError;
    public string ModelVersion; // 応答の model (例 jev-1.13.0)

    private string _key;
    private bool _keyResolved;
    private string _keySource;

    private UnityWebRequest _request;
    private byte[] _body;
    private Action<JevAnswer> _callback;
    private float _startedAt;
    private float _retryAt = -1f;
    private int _attempt;

    public bool Busy => _callback != null;

    /// <summary>キーの在りか (表示用。値は出さない)。</summary>
    public string KeySource
    {
        get
        {
            ResolveKey();
            return _keySource;
        }
    }

    public bool HasKey
    {
        get
        {
            ResolveKey();
            return !string.IsNullOrEmpty(_key);
        }
    }

    /// <summary>キーの置き場所 (ファイル)。</summary>
    public static string KeyFilePath =>
#if UNITY_EDITOR
        Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "UserSettings", KeyFileName);
#else
        Path.Combine(Application.persistentDataPath, KeyFileName);
#endif

    public static string MissingKeyMessage =>
        $"Jev の API キーが見つかりません。環境変数 {EnvVar} を設定するか、{KeyFilePath} にキーだけを 1 行で保存してください " +
        "(UserSettings は git の追跡対象外)。キーが無い間はプログラムの推奨 (ローカル方針) で動きます。";

    public void ResetStats()
    {
        Calls = Failures = Retries = 0;
        InputTokens = OutputTokens = 0;
        TotalLatency = 0f;
        LastError = null;
    }

    /// <summary>キーを読み直す (ファイルを置いた後など)。</summary>
    public void ReloadKey()
    {
        _keyResolved = false;
        ResolveKey();
    }

    private void ResolveKey()
    {
        if (_keyResolved)
            return;
        _keyResolved = true;
        _key = null;
        _keySource = "なし";

        try
        {
            var env = Environment.GetEnvironmentVariable(EnvVar);
            if (!string.IsNullOrWhiteSpace(env))
            {
                _key = env.Trim();
                _keySource = "環境変数 (プロセス)";
                return;
            }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // エディタを先に起動してから環境変数を設定した場合でも読めるよう、ユーザー環境変数 (レジストリ) も見る
            env = Environment.GetEnvironmentVariable(EnvVar, EnvironmentVariableTarget.User);
            if (!string.IsNullOrWhiteSpace(env))
            {
                _key = env.Trim();
                _keySource = "環境変数 (ユーザー)";
                return;
            }
#endif
        }
        catch (Exception)
        {
            // 環境変数が読めない環境はファイルへ
        }

        try
        {
            var path = KeyFilePath;
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path).Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    _key = text;
                    _keySource = "ファイル " + path;
                }
            }
        }
        catch (Exception)
        {
            // 読めなければキー無し
        }
    }

    /// <summary>
    /// 問い合わせる。state / questions は BotJson で直列化できる値 (Dictionary / List / 数値 / 文字列)。
    /// 完了 (成功/失敗) すると callback が Tick の中で呼ばれる。実行中なら false。
    /// </summary>
    public bool Ask(object state, Dictionary<string, object> questions, Action<JevAnswer> callback)
    {
        if (Busy)
            return false;

        ResolveKey();
        if (string.IsNullOrEmpty(_key))
        {
            callback?.Invoke(new JevAnswer { Ok = false, Error = "no_api_key" });
            return true;
        }

        var payload = new Dictionary<string, object>
        {
            ["model"] = Model,
            ["state"] = state,
            ["questions"] = questions,
        };
        _body = Encoding.UTF8.GetBytes(BotJson.Serialize(payload));
        _callback = callback;
        _attempt = 0;
        _startedAt = Time.realtimeSinceStartup;
        Send();
        return true;
    }

    /// <summary>直近の送信内容 (デバッグ表示用。キーは含まない)。</summary>
    public string LastRequestBody => _body != null ? Encoding.UTF8.GetString(_body) : null;

    private void Send()
    {
        _attempt++;
        _retryAt = -1f;
        _request = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(_body) { contentType = "application/json" },
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = Mathf.CeilToInt(TimeoutSeconds),
        };
        _request.SetRequestHeader("Authorization", "Bearer " + _key);
        _request.SetRequestHeader("Content-Type", "application/json");
        _request.SendWebRequest();
    }

    /// <summary>毎フレーム呼ぶ (実時間で進む)。</summary>
    public void Tick()
    {
        if (_callback == null)
            return;

        if (_request == null)
        {
            if (_retryAt >= 0f && Time.realtimeSinceStartup >= _retryAt)
                Send();
            return;
        }

        if (!_request.isDone)
            return;

        var code = _request.responseCode;
        var text = _request.downloadHandler != null ? _request.downloadHandler.text : null;
        var networkError = _request.result == UnityWebRequest.Result.ConnectionError;
        _request.Dispose();
        _request = null;

        if (code == 200 && !string.IsNullOrEmpty(text))
        {
            Finish(ParseAnswer(text));
            return;
        }

        var retryable = networkError || code == 0 || code == 429 || code == 529 || code >= 500;
        var reason = networkError || code == 0 ? "通信エラー/タイムアウト" : $"HTTP {code}";
        if (code == 401) reason = "HTTP 401 (キーが無効)";
        else if (code == 422) reason = "HTTP 422 (リクエストの形式が不正)";

        if (retryable && _attempt <= MaxRetries)
        {
            Retries++;
            _retryAt = Time.realtimeSinceStartup + 0.5f * Mathf.Pow(2f, _attempt - 1);
            return;
        }

        Finish(new JevAnswer { Ok = false, Error = reason + (code == 422 ? ": " + Truncate(text, 300) : "") });
    }

    /// <summary>実行中の問い合わせを捨てる (callback は呼ばない)。</summary>
    public void Cancel()
    {
        _request?.Abort();
        _request?.Dispose();
        _request = null;
        _callback = null;
        _retryAt = -1f;
    }

    private void Finish(JevAnswer answer)
    {
        answer.LatencySeconds = Time.realtimeSinceStartup - _startedAt;
        answer.Attempts = _attempt;
        if (answer.Ok)
        {
            Calls++;
            InputTokens += answer.InputTokens;
            OutputTokens += answer.OutputTokens;
            TotalLatency += answer.LatencySeconds;
        }
        else
        {
            Failures++;
            LastError = answer.Error;
        }

        var callback = _callback;
        _callback = null;
        callback?.Invoke(answer);
    }

    private JevAnswer ParseAnswer(string text)
    {
        var answer = new JevAnswer();
        object root;
        try
        {
            root = BotJson.Parse(text);
        }
        catch (Exception e)
        {
            return new JevAnswer { Ok = false, Error = "応答の解析に失敗: " + e.Message };
        }

        ModelVersion = BotJson.GetString(root, "model") ?? ModelVersion;
        var usage = BotJson.Get(root, "usage");
        answer.InputTokens = (int)BotJson.GetNumber(usage, "input_tokens");
        answer.OutputTokens = (int)BotJson.GetNumber(usage, "output_tokens");

        if (BotJson.Get(root, "answers") is Dictionary<string, object> answers)
        {
            foreach (var pair in answers)
            {
                var type = BotJson.GetString(pair.Value, "type");
                if (type == "noul")
                {
                    answer.Nouls[pair.Key] = (float)BotJson.GetNumber(pair.Value, "noul");
                    continue;
                }

                if (pair.Key != "action")
                    continue;

                answer.Choice = BotJson.GetString(pair.Value, "choice");
                answer.Confidence = (float)BotJson.GetNumber(pair.Value, "confidence");
                if (BotJson.Get(pair.Value, "probabilities") is Dictionary<string, object> probs)
                {
                    foreach (var p in probs)
                        answer.Probabilities[p.Key] = p.Value is double d ? (float)d : 0f;
                }
            }
        }

        answer.Ok = !string.IsNullOrEmpty(answer.Choice);
        if (!answer.Ok)
            answer.Error = "応答に action の選択が無い: " + Truncate(text, 200);
        return answer;
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s.Substring(0, max) + "…";
}
#endif
