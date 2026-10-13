using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// アニメ・エフェクト調整ウィンドウ (メニュー: NeverNight/Anim &amp; Effect Tuner)。
/// 調整シーン (AnimEffectTuningScene) を再生中に、キャラの選択・アニメの再生 / 一時停止 / シーク、
/// エフェクトの追加と「オフセット (X / Y)・開始タイミング・大きさ・回転」の調整を行う。
/// オフセットはシーンビューの丸いハンドルをドラッグしても動かせる。開始タイミングはタイムラインの印をドラッグしても動かせる。
/// 値は AnimEffectCueSet アセットに直接書くので、再生を止めても残る。
/// </summary>
public class AnimEffectTunerWindow : EditorWindow
{
    private const string ScenePath = "Assets/Scenes/Test/AnimEffectTuningScene.unity";
    private const float FrameStep = 1f / 30f;

    private Vector2 _clipScroll;
    private Vector2 _cueScroll;
    private string _clipFilter = "";
    private int _dragCue = -1;
    private bool _dragPlayhead;

    [MenuItem("NeverNight/Anim & Effect Tuner")]
    public static void Open() => GetWindow<AnimEffectTunerWindow>("Anim & Effect Tuner");

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (EditorApplication.isPlaying)
            Repaint();
    }

    private static AnimTuningStage FindStage() => FindFirstObjectByType<AnimTuningStage>();

    private void OnGUI()
    {
        var stage = FindStage();
        if (!EditorApplication.isPlaying || stage == null)
        {
            DrawIdle(stage);
            return;
        }

        DrawCharacter(stage);
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Max(200f, position.width * 0.33f))))
                DrawClipList(stage);
            using (new EditorGUILayout.VerticalScope())
            {
                DrawPlayback(stage);
                EditorGUILayout.Space(4);
                DrawTimeline(stage);
                EditorGUILayout.Space(4);
                DrawCues(stage);
            }
        }
    }

    #region 再生前

    private static void DrawIdle(AnimTuningStage stage)
    {
        EditorGUILayout.HelpBox(
            "調整シーンを再生している間に使います。\n" +
            "キャラとエフェクトの一覧は AnimTuningCatalog、調整した値は AnimEffectCueSet に入ります。", MessageType.Info);

        if (GUILayout.Button("調整シーンを開いて再生", GUILayout.Height(28)))
        {
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
            else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
                Application.runInBackground = true;
                EditorApplication.isPlaying = true;
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("カタログを選択"))
                Selection.activeObject = stage != null ? stage.Catalog : FindAsset<AnimTuningCatalog>();
            if (GUILayout.Button("エフェクト設定を選択"))
                Selection.activeObject = stage != null ? stage.CueSet : FindAsset<AnimEffectCueSet>();
        }
    }

    private static T FindAsset<T>() where T : Object
    {
        var guid = AssetDatabase.FindAssets($"t:{typeof(T).Name}").FirstOrDefault();
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    }

    #endregion

    #region キャラ・アニメ

    private static void DrawCharacter(AnimTuningStage stage)
    {
        var catalog = stage.Catalog;
        if (catalog == null || catalog.Characters.Count == 0)
        {
            EditorGUILayout.HelpBox("カタログにキャラが登録されていません。", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            var names = catalog.Characters.Select(c => c.Id).ToArray();
            var next = EditorGUILayout.Popup("キャラ", Mathf.Max(stage.CharacterIndex, 0), names);
            if (next != stage.CharacterIndex)
                stage.SelectCharacter(next);
            if (GUILayout.Button("◀", GUILayout.Width(26)))
                stage.SelectCharacter((stage.CharacterIndex - 1 + names.Length) % names.Length);
            if (GUILayout.Button("▶", GUILayout.Width(26)))
                stage.SelectCharacter((stage.CharacterIndex + 1) % names.Length);
            if (GUILayout.Button("カメラを合わせる", GUILayout.Width(110)))
                stage.FrameCamera();
        }
    }

    private void DrawClipList(AnimTuningStage stage)
    {
        EditorGUILayout.LabelField("アニメーション", EditorStyles.boldLabel);
        _clipFilter = EditorGUILayout.TextField(_clipFilter, EditorStyles.toolbarSearchField);

        if (stage.Character == null || stage.Character.Clips.Count == 0)
        {
            EditorGUILayout.HelpBox("このキャラには再生できるアニメがありません。\n" +
                                    "コードで絵を差し替えている場合は、カタログの Sprite Clips に連番を登録してください。", MessageType.None);
            return;
        }

        var cueSet = stage.CueSet;
        var charId = stage.CharacterEntry?.Id;
        _clipScroll = EditorGUILayout.BeginScrollView(_clipScroll);
        for (var i = 0; i < stage.Character.Clips.Count; i++)
        {
            var clip = stage.Character.Clips[i];
            if (_clipFilter.Length > 0 && clip.Name.IndexOf(_clipFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            var cueCount = cueSet != null ? cueSet.Find(charId, clip.Name)?.Cues.Count ?? 0 : 0;
            var label = $"{clip.Name}  ({clip.Duration:0.00}s){(cueCount > 0 ? $"  ✦{cueCount}" : "")}";
            var selected = i == stage.ClipIndex;
            if (GUILayout.Toggle(selected, label, "Button") && !selected)
            {
                stage.SelectClip(i);
                stage.Playing = true;
            }
        }
        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region 再生操作・タイムライン

    private static void DrawPlayback(AnimTuningStage stage)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(stage.Playing ? "❚❚ 一時停止" : "▶ 再生", GUILayout.Width(90)))
            {
                if (!stage.Playing && !stage.Loop && stage.Time >= stage.TimelineLength)
                    stage.Time = 0f;
                stage.Playing = !stage.Playing;
            }
            if (GUILayout.Button("⏮ 最初から", GUILayout.Width(80)))
            {
                stage.Time = 0f;
                stage.Playing = true;
            }
            if (GUILayout.Button("◀|", GUILayout.Width(32)))
                Step(stage, -FrameStep);
            if (GUILayout.Button("|▶", GUILayout.Width(32)))
                Step(stage, FrameStep);
            stage.Loop = GUILayout.Toggle(stage.Loop, "ループ", GUILayout.Width(60));
            stage.FacingRight = GUILayout.Toggle(stage.FacingRight, "右向き", GUILayout.Width(60));
        }
        stage.Speed = EditorGUILayout.Slider("再生速度", stage.Speed, 0.05f, 2f);
        stage.LoopGap = EditorGUILayout.Slider("ループの間 (秒)", stage.LoopGap, 0f, 2f);
    }

    private static void Step(AnimTuningStage stage, float delta)
    {
        stage.Playing = false;
        stage.Time = Mathf.Clamp(stage.Time + delta, 0f, stage.TimelineLength);
    }

    private void DrawTimeline(AnimTuningStage stage)
    {
        var length = Mathf.Max(stage.TimelineLength, 0.01f);
        var animLength = stage.Clip?.Duration ?? 0f;
        EditorGUILayout.LabelField($"時間 {stage.Time:0.000} / アニメ {animLength:0.000}s  (フレーム {Mathf.FloorToInt(stage.Time * 30f)} @30fps)");

        var rect = GUILayoutUtility.GetRect(10f, 44f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.15f));
        float ToX(float t) => rect.x + rect.width * Mathf.Clamp01(t / length);
        float ToT(float x) => Mathf.Clamp01((x - rect.x) / rect.width) * length;

        // アニメの長さ
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + 4, ToX(animLength) - rect.x, 12), new Color(0.3f, 0.5f, 0.8f, 0.8f));
        // 0.1 秒ごとの目盛り
        for (var t = 0f; t <= length; t += 0.1f)
        {
            var major = Mathf.Abs(t - Mathf.Round(t)) < 0.001f;
            EditorGUI.DrawRect(new Rect(ToX(t), rect.yMax - (major ? 10 : 5), 1, major ? 10 : 5), new Color(1, 1, 1, 0.35f));
        }

        // エフェクトの開始位置と長さ
        var cues = stage.CurrentCues;
        if (cues != null)
        {
            for (var i = 0; i < cues.Cues.Count; i++)
            {
                var cue = cues.Cues[i];
                var x = ToX(cue.Delay);
                var col = i == stage.SelectedCue ? Color.yellow : new Color(0.3f, 0.9f, 1f);
                EditorGUI.DrawRect(new Rect(x - 3, rect.y + 18, 6, 14), col);
                GUI.Label(new Rect(x + 4, rect.y + 17, 120, 16), cue.EffectId, EditorStyles.miniLabel);
            }
        }

        // 再生位置
        EditorGUI.DrawRect(new Rect(ToX(stage.Time) - 1, rect.y, 2, rect.height), Color.red);

        // マウス: エフェクトの印をつかむと開始タイミング、それ以外は再生位置
        var e = Event.current;
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            _dragCue = -1;
            if (cues != null)
            {
                for (var i = 0; i < cues.Cues.Count; i++)
                    if (Mathf.Abs(ToX(cues.Cues[i].Delay) - e.mousePosition.x) < 5f && e.mousePosition.y > rect.y + 16)
                        _dragCue = i;
            }
            _dragPlayhead = _dragCue < 0;
            if (_dragCue >= 0)
            {
                stage.SelectedCue = _dragCue;
                Undo.RecordObject(stage.CueSet, "Move Effect Timing");
            }
            e.Use();
        }
        if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
        {
            if (_dragPlayhead)
            {
                stage.Playing = false;
                stage.Time = ToT(e.mousePosition.x);
                e.Use();
            }
            else if (_dragCue >= 0 && cues != null && _dragCue < cues.Cues.Count)
            {
                cues.Cues[_dragCue].Delay = Snap(ToT(e.mousePosition.x));
                EditorUtility.SetDirty(stage.CueSet);
                e.Use();
            }
        }
        if (e.type == EventType.MouseUp)
        {
            _dragCue = -1;
            _dragPlayhead = false;
        }
    }

    /// <summary>1/60 秒単位にそろえる (Shift で自由)。</summary>
    private static float Snap(float t) => Event.current.shift ? t : Mathf.Round(t * 60f) / 60f;

    #endregion

    #region エフェクト設定

    private void DrawCues(AnimTuningStage stage)
    {
        var cueSet = stage.CueSet;
        var catalog = stage.Catalog;
        if (cueSet == null || catalog == null || stage.CharacterEntry == null || stage.Clip == null)
            return;

        EditorGUILayout.LabelField("エフェクト", EditorStyles.boldLabel);
        var effectIds = catalog.Effects.Select(e => e.Id).ToArray();
        if (effectIds.Length == 0)
        {
            EditorGUILayout.HelpBox("カタログにエフェクトが登録されていません (Effects に追加してください)。", MessageType.Warning);
            return;
        }

        var cues = stage.CurrentCues;
        var animLength = stage.Clip.Duration;
        _cueScroll = EditorGUILayout.BeginScrollView(_cueScroll);
        if (cues != null)
        {
            for (var i = 0; i < cues.Cues.Count; i++)
            {
                var cue = cues.Cues[i];
                var selected = i == stage.SelectedCue;
                using (new EditorGUILayout.VerticalScope(selected ? "SelectionRect" : "HelpBox"))
                {
                    EditorGUI.BeginChangeCheck();
                    string effectId;
                    Vector2 offset;
                    float delay, scale, rotation;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(selected ? "●" : "○", GUILayout.Width(24)))
                            stage.SelectedCue = selected ? -1 : i;
                        var current = System.Array.IndexOf(effectIds, cue.EffectId);
                        var picked = EditorGUILayout.Popup(current, effectIds);
                        effectId = picked >= 0 ? effectIds[picked] : cue.EffectId;
                    }
                    offset = EditorGUILayout.Vector2Field("オフセット", cue.Offset);
                    offset.x = EditorGUILayout.Slider("  X", offset.x, -6f, 6f);
                    offset.y = EditorGUILayout.Slider("  Y", offset.y, -2f, 8f);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        delay = EditorGUILayout.Slider("開始 (秒)", cue.Delay, 0f, Mathf.Max(animLength, cue.Delay, 0.1f));
                        if (GUILayout.Button("今の時間", GUILayout.Width(64)))
                            delay = stage.Time;
                    }
                    scale = EditorGUILayout.Slider("大きさ", cue.Scale, 0.05f, 5f);
                    rotation = EditorGUILayout.Slider("回転", cue.Rotation, -180f, 180f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(cueSet, "Edit Effect Cue");
                        var rebuild = effectId != cue.EffectId;
                        cue.EffectId = effectId;
                        cue.Offset = offset;
                        cue.Delay = Mathf.Max(0f, delay);
                        cue.Scale = scale;
                        cue.Rotation = rotation;
                        stage.SelectedCue = i;
                        EditorUtility.SetDirty(cueSet);
                        if (rebuild)
                            stage.RebuildEffects();
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("複製", GUILayout.Width(50)))
                        {
                            Undo.RecordObject(cueSet, "Duplicate Effect Cue");
                            cues.Cues.Insert(i + 1, JsonUtility.FromJson<AnimEffectCueSet.Cue>(JsonUtility.ToJson(cue)));
                            EditorUtility.SetDirty(cueSet);
                            stage.RebuildEffects();
                            GUIUtility.ExitGUI();
                        }
                        if (GUILayout.Button("削除", GUILayout.Width(50)))
                        {
                            Undo.RecordObject(cueSet, "Remove Effect Cue");
                            cues.Cues.RemoveAt(i);
                            EditorUtility.SetDirty(cueSet);
                            stage.SelectedCue = -1;
                            stage.RebuildEffects();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("＋ エフェクトを追加 (今の時間に)", GUILayout.Height(24)))
            {
                Undo.RecordObject(cueSet, "Add Effect Cue");
                var target = cueSet.Find(stage.CharacterEntry.Id, stage.Clip.Name, create: true);
                target.Cues.Add(new AnimEffectCueSet.Cue
                {
                    EffectId = effectIds[0],
                    Offset = new Vector2(0f, 1f),
                    Delay = Mathf.Clamp(stage.Time, 0f, animLength),
                });
                stage.SelectedCue = target.Cues.Count - 1;
                EditorUtility.SetDirty(cueSet);
                stage.RebuildEffects();
            }
            if (GUILayout.Button("保存", GUILayout.Width(60), GUILayout.Height(24)))
                AssetDatabase.SaveAssetIfDirty(cueSet);
        }
    }

    #endregion

    #region シーンビュー

    private void OnSceneGUI(SceneView view)
    {
        if (!EditorApplication.isPlaying)
            return;
        var stage = FindStage();
        var cues = stage != null ? stage.CurrentCues : null;
        if (cues == null || stage.Anchor == null)
            return;

        for (var i = 0; i < cues.Cues.Count; i++)
        {
            var cue = cues.Cues[i];
            var world = stage.Anchor.TransformPoint(cue.Offset);
            var size = HandleUtility.GetHandleSize(world) * (i == stage.SelectedCue ? 0.12f : 0.08f);
            Handles.color = i == stage.SelectedCue ? Color.yellow : new Color(0.3f, 0.9f, 1f);
            Handles.Label(world + Vector3.up * size * 2f, $"{cue.EffectId} @{cue.Delay:0.00}s");

            EditorGUI.BeginChangeCheck();
            var moved = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.CircleHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(stage.CueSet, "Move Effect Offset");
                var local = (Vector2)stage.Anchor.InverseTransformPoint(moved);
                cue.Offset = new Vector2(Mathf.Round(local.x * 100f) / 100f, Mathf.Round(local.y * 100f) / 100f);
                stage.SelectedCue = i;
                EditorUtility.SetDirty(stage.CueSet);
                Repaint();
            }
        }
    }

    #endregion
}
