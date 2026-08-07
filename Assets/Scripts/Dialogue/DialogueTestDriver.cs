using System;
using UnityEngine;

/// <summary>
/// 会話デバッグ専用シーン (DialogueTestScene) のドライバ。
/// 画面左にシナリオラベルのボタン一覧を出し、クリックで会話を再生する。
/// ラベル一覧は起動時に DialogueScene をロードして、インポート済みのシナリオデータから取得する
/// (xls/tsv どちらで書かれた会話でも、実際に再生できるラベルだけが並ぶ)。
/// </summary>
public class DialogueTestDriver : MonoBehaviour
{
    private string[] _scenarioLabels = Array.Empty<string>();
    private bool _labelsLoading;
    private string _customLabel = "";
    private string _playingLabel = "";
    private Vector2 _scroll;

    private void Start()
    {
        _labelsLoading = true;
        DialogueService.LoadScenarioLabels(labels =>
        {
            _scenarioLabels = labels;
            _labelsLoading = false;
        });
    }

    private void OnGUI()
    {
        const float width = 340f;
        GUILayout.BeginArea(new Rect(10f, 10f, width, Screen.height - 20f), GUI.skin.box);

        GUILayout.Label("<b>会話デバッグ</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
        if (_labelsLoading)
            GUILayout.Label("ラベル一覧を読み込み中...");
        else if (DialogueService.IsPlaying)
            GUILayout.Label($"再生中: {_playingLabel} (クリックで文字送り)");
        else
            GUILayout.Label("ラベルを選んで再生してください");
        GUILayout.Space(4f);

        GUI.enabled = !DialogueService.IsPlaying;

        _scroll = GUILayout.BeginScrollView(_scroll);
        foreach (var label in _scenarioLabels)
        {
            if (GUILayout.Button(label, GUILayout.Height(30f)))
                Play(label);
        }
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        _customLabel = GUILayout.TextField(_customLabel, GUILayout.Height(24f));
        if (GUILayout.Button("再生", GUILayout.Width(60f), GUILayout.Height(24f)) &&
            !string.IsNullOrWhiteSpace(_customLabel))
        {
            Play(_customLabel.TrimStart('*').Trim());
        }
        GUILayout.EndHorizontal();

        GUI.enabled = true;
        GUILayout.EndArea();
    }

    private void Play(string label)
    {
        _playingLabel = label;
        DialogueService.Play(label, () => _playingLabel = "");
    }
}
