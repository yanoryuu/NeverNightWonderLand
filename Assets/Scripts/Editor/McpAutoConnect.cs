using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

/// <summary>
/// エディタ起動時に MCP for Unity のブリッジを自動接続する。
/// (既定ではウィンドウの Connect を手動で押すまで接続されないため、
///  Claude Code などの MCP クライアントから常に操作できるようにする)
/// </summary>
[InitializeOnLoad]
public static class McpAutoConnect
{
    static McpAutoConnect()
    {
        // 起動直後はサービスの初期化が済んでいないことがあるので1フレーム遅らせる
        EditorApplication.delayCall += TryConnect;
    }

    [MenuItem("NeverNight/MCP/Connect Bridge")]
    private static void TryConnect()
    {
        ConnectAsync();
    }

    private static async void ConnectAsync()
    {
        try
        {
            var ok = await MCPServiceLocator.Bridge.StartAsync();
            Debug.Log($"[McpAutoConnect] MCP ブリッジ接続: {(ok ? "成功" : "失敗")}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[McpAutoConnect] MCP ブリッジ接続に失敗: {e.Message}");
        }
    }
}
