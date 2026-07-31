using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 戦闘パラメータの調整ウィンドウ (メニュー: NeverNight/Combat Tuner)。
/// 外出しされた攻撃パラメータ (PlayerConsts の各プロファイル / Assets/Attacks の装備攻撃) を
/// 一箇所で編集し、ウィンドウ内の専用プレビュー画面 (PreviewRenderUtility) で
/// プレイヤーのスプライト/アニメーションと当たり判定を重ねて確認できる。
/// スプライトとアニメーションは自由に差し替え可能 (選択は EditorPrefs に保存)。
/// 「プレビュー再生」で発生 (HitDelay) → 持続 → 硬直のタイミングを再生する。プレイモード不要。
/// </summary>
public class CombatTunerWindow : EditorWindow
{
    private enum EntryKind { MeleeProfile, Ranged, SkillBox, Parry }

    /// <summary>編集/プレビュー対象1件 (プロファイル / 遠距離攻撃 / スキル判定 / パリィ)。</summary>
    private class Entry
    {
        public string Label;
        public EntryKind Kind;
        public Object Asset;
        public string ProfilePath; // Kind=MeleeProfile の時の AttackProfile プロパティパス
        public string OffsetPath;  // Kind=SkillBox の時のオフセット (Vector2) パス
        public string SizePath;    // Kind=SkillBox の時のサイズ (Vector2) パス
        public Color BoxColor;     // Kind=SkillBox の表示色
        public bool FlipOffsetX;   // オフセット x を向きで反転するか
    }

    private const string SpritePrefKey = "NeverNight.CombatTuner.Sprite";
    private const string ClipPrefKey = "NeverNight.CombatTuner.Clip";
    private const float FeetOffset = -0.92f; // プレイヤーの足元 (基準からのオフセット)

    private readonly List<Entry> _entries = new();
    private int _selected;
    private Vector2 _scroll;

    // SerializedObject は毎フレーム生成すると kDontSaveInEditor のアサートを誘発するためキャッシュする
    private readonly Dictionary<Object, SerializedObject> _serializedCache = new();

    // プレビュー設定
    private int _facing = 1;
    private bool _showAllProfiles;
    private bool _skillFoldout;
    private Sprite _playerSprite;
    private AnimationClip _clip;
    private bool _syncAnimToDuration = true;
    private float _zoom = 2.4f;
    private float _scrub; // 停止中のタイムラインつまみ (sec)

    // 再生状態
    private bool _isPlaying;
    private bool _comboPlay;
    private float _playT;
    private double _lastTick;

    private PlayerConsts _consts;

    // 専用プレビュー描画
    private PreviewRenderUtility _preview;
    private GameObject _previewGo;
    private SpriteRenderer _previewSprite;
    private AnimationClip _cachedClip;
    private ObjectReferenceKeyframe[] _spriteKeys; // クリップ内のスプライト差し替えキー

    [MenuItem("NeverNight/Combat Tuner")]
    private static void Open()
    {
        var window = GetWindow<CombatTunerWindow>("Combat Tuner");
        window.minSize = new Vector2(340f, 560f);
    }

    private void OnEnable()
    {
        RefreshEntries();
        LoadPreviewAssets();
        EditorApplication.update += Tick;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Tick;
        _preview?.Cleanup();
        _preview = null;
        DisposeSerializedCache();
    }

    private void DisposeSerializedCache()
    {
        foreach (var so in _serializedCache.Values)
            so?.Dispose();
        _serializedCache.Clear();
    }

    /// <summary>アセットの SerializedObject をキャッシュから取得する (無ければ生成)。</summary>
    private SerializedObject GetSerialized(Object asset)
    {
        if (_serializedCache.TryGetValue(asset, out var so) && so != null && so.targetObject != null)
            return so;

        so = new SerializedObject(asset);
        _serializedCache[asset] = so;
        return so;
    }

    // ------------------------------------------------------------ 対象収集

    private void RefreshEntries()
    {
        _entries.Clear();

        _consts = FindAsset<PlayerConsts>();
        if (_consts != null)
        {
            _entries.Add(new Entry { Label = "基本: 二刀流 (DualAttack)", Kind = EntryKind.MeleeProfile, Asset = _consts, ProfilePath = "_dualAttack" });
            _entries.Add(new Entry { Label = "基本: 両手持ち (HeavyAttack)", Kind = EntryKind.MeleeProfile, Asset = _consts, ProfilePath = "_heavyAttack" });
            _entries.Add(new Entry { Label = "切替: 分割の一閃", Kind = EntryKind.MeleeProfile, Asset = _consts, ProfilePath = "_splitSwitchAttack" });
            _entries.Add(new Entry { Label = "切替: 合体の振り下ろし", Kind = EntryKind.MeleeProfile, Asset = _consts, ProfilePath = "_mergeSwitchAttack" });
            _entries.Add(new Entry { Label = "裁断 (Finisher)", Kind = EntryKind.MeleeProfile, Asset = _consts, ProfilePath = "_finisherProfile" });
            _entries.Add(new Entry { Label = "落下攻撃の着地衝撃 (Slam)", Kind = EntryKind.MeleeProfile, Asset = _consts, ProfilePath = "_slamAttack" });
        }

        foreach (var melee in FindAssets<MeleeAttackDefinition>())
            _entries.Add(new Entry { Label = $"装備: {melee.DisplayName} ({melee.name})", Kind = EntryKind.MeleeProfile, Asset = melee, ProfilePath = "_profile" });

        foreach (var ranged in FindAssets<RangedSpecialDefinition>())
            _entries.Add(new Entry { Label = $"遠距離: {ranged.DisplayName} ({ranged.name})", Kind = EntryKind.Ranged, Asset = ranged });

        if (_consts != null)
        {
            _entries.Add(new Entry { Label = "パリィ (受付/硬直)", Kind = EntryKind.Parry, Asset = _consts });
            _entries.Add(new Entry
            {
                Label = "スキル: 落下攻撃の破壊判定", Kind = EntryKind.SkillBox, Asset = _consts,
                OffsetPath = "_slamBreakOffset", SizePath = "_slamBreakSize",
                BoxColor = new Color(0.9f, 0.55f, 0.2f), FlipOffsetX = false,
            });
            _entries.Add(new Entry
            {
                Label = "スキル: 大ジャンプの頭上判定", Kind = EntryKind.SkillBox, Asset = _consts,
                OffsetPath = "_superJumpHitOffset", SizePath = "_superJumpHitSize",
                BoxColor = new Color(0.4f, 0.75f, 0.95f), FlipOffsetX = false,
            });
            _entries.Add(new Entry
            {
                Label = "スキル: 横突進の正面判定", Kind = EntryKind.SkillBox, Asset = _consts,
                OffsetPath = "_chargeRushHitOffset", SizePath = "_chargeRushHitSize",
                BoxColor = new Color(0.65f, 0.4f, 0.85f), FlipOffsetX = true,
            });
        }

        _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, _entries.Count - 1));
    }

    private static T FindAsset<T>() where T : Object =>
        FindAssets<T>().FirstOrDefault();

    private static IEnumerable<T> FindAssets<T>() where T : Object =>
        AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a => a != null);

    /// <summary>スプライト/アニメの選択を復元する。初回はプレイヤープレハブと Attack クリップを既定にする。</summary>
    private void LoadPreviewAssets()
    {
        _playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EditorPrefs.GetString(SpritePrefKey, ""));
        _clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(EditorPrefs.GetString(ClipPrefKey, ""));

        if (_playerSprite == null)
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            _playerSprite = playerPrefab != null ? playerPrefab.GetComponent<SpriteRenderer>()?.sprite : null;
        }

        if (_clip == null)
        {
            _clip = AssetDatabase.FindAssets("t:AnimationClip Attack")
                .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(c => c != null && c.name == "Attack");
        }
    }

    // ------------------------------------------------------------ ウィンドウ GUI

    private void OnGUI()
    {
        if (_entries.Count == 0)
            RefreshEntries();
        if (_entries.Count == 0)
        {
            EditorGUILayout.HelpBox("PlayerConsts / 攻撃アセットが見つかりません。", MessageType.Warning);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // 対象選択
        EditorGUILayout.LabelField("編集対象", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            _selected = EditorGUILayout.Popup(_selected, _entries.Select(e => e.Label).ToArray());
            if (GUILayout.Button("再スキャン", GUILayout.Width(70f)))
                RefreshEntries();
        }
        var entry = _entries[Mathf.Clamp(_selected, 0, _entries.Count - 1)];

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Project で選択", GUILayout.Width(110f)))
                Selection.activeObject = entry.Asset;
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(entry.Asset), EditorStyles.miniLabel);
        }

        // プレビュー画面
        EditorGUILayout.Space(4f);
        DrawPreviewSection(entry);

        // 編集フィールド
        EditorGUILayout.Space(4f);
        var so = GetSerialized(entry.Asset);
        so.Update();
        switch (entry.Kind)
        {
            case EntryKind.MeleeProfile: DrawMeleeEditor(so, entry); break;
            case EntryKind.Ranged: DrawRangedEditor(so); break;
            case EntryKind.SkillBox: DrawSkillBoxEditor(so, entry); break;
            case EntryKind.Parry: DrawParryEditor(so); break;
        }
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        DrawSkillConstsSection();

        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------ プレビュー (専用レンダリング)

    private void DrawPreviewSection(Entry entry)
    {
        EditorGUILayout.LabelField("プレビュー", EditorStyles.boldLabel);

        // スプライト/アニメの差し替え (自由に変更可・選択は保存される)
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            _playerSprite = (Sprite)EditorGUILayout.ObjectField("プレイヤースプライト", _playerSprite, typeof(Sprite), false);
            _clip = (AnimationClip)EditorGUILayout.ObjectField("アニメーション", _clip, typeof(AnimationClip), false);
            if (check.changed)
            {
                EditorPrefs.SetString(SpritePrefKey, AssetDatabase.GetAssetPath(_playerSprite));
                EditorPrefs.SetString(ClipPrefKey, AssetDatabase.GetAssetPath(_clip));
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            _syncAnimToDuration = EditorGUILayout.ToggleLeft("アニメを攻撃時間に同期", _syncAnimToDuration, GUILayout.Width(160f));
            EditorGUILayout.LabelField("向き", GUILayout.Width(28f));
            if (GUILayout.Toggle(_facing > 0, "右", EditorStyles.miniButtonLeft, GUILayout.Width(30f))) _facing = 1;
            if (GUILayout.Toggle(_facing < 0, "左", EditorStyles.miniButtonRight, GUILayout.Width(30f))) _facing = -1;
            EditorGUILayout.LabelField("ズーム", GUILayout.Width(38f));
            _zoom = GUILayout.HorizontalSlider(_zoom, 1.2f, 5f);
        }
        _showAllProfiles = EditorGUILayout.ToggleLeft("全プロファイルをまとめて表示 (色分け)", _showAllProfiles);

        // 描画領域
        var rect = GUILayoutUtility.GetRect(200f, 280f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            RenderPreviewTexture(rect, entry);
            DrawOverlays(rect, entry);
        }

        // 再生コントロール + タイムラインつまみ
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(entry.Kind == EntryKind.SkillBox))
            {
                if (GUILayout.Button(_isPlaying && !_comboPlay ? "■ 停止" : "▶ 単発", GUILayout.Width(70f)))
                    TogglePlay(combo: false);
            }

            using (new EditorGUI.DisabledScope(entry.Kind != EntryKind.MeleeProfile))
            {
                var comboCount = _consts != null ? _consts.BaseMaxCombo : 3;
                if (GUILayout.Button(_isPlaying && _comboPlay ? "■ 停止" : $"▶ コンボ({comboCount})", GUILayout.Width(90f)))
                    TogglePlay(combo: true);
            }

            using (new EditorGUI.DisabledScope(_isPlaying))
            {
                var total = Mathf.Max(GetPlayTotal(entry), 0.0001f);
                _scrub = GUILayout.HorizontalSlider(Mathf.Min(_scrub, total), 0f, total);
            }
        }
        EditorGUILayout.LabelField("判定色: 灰=発生前 / 赤=ヒット瞬間 / 青=硬直。停止中はスライダーで任意の瞬間を確認", EditorStyles.miniLabel);
    }

    private float CurrentPlayT() => _isPlaying ? _playT : _scrub;

    private void EnsurePreview()
    {
        if (_preview != null)
            return;

        _preview = new PreviewRenderUtility();
        var cam = _preview.camera;
        cam.orthographic = true;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.14f, 0.14f, 0.17f);

        _previewGo = new GameObject("PreviewPlayer") { hideFlags = HideFlags.HideAndDontSave };
        _previewSprite = _previewGo.AddComponent<SpriteRenderer>();
        // URP のスプライトマテリアルはプレビューカメラで描けないことがあるため組み込みシェーダを使う
        _previewSprite.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
        {
            hideFlags = HideFlags.HideAndDontSave,
        };
        _preview.AddSingleGO(_previewGo);
    }

    /// <summary>プレビューシーンをレンダリングして rect に描く (スプライト+アニメーション)。</summary>
    private void RenderPreviewTexture(Rect rect, Entry entry)
    {
        EnsurePreview();

        // アニメーションのサンプリング (クリップ未指定なら静止スプライト)。
        // AnimationClip.SampleAnimation はスプライト差し替え (オブジェクト参照カーブ) を
        // 反映しないため、キーフレームを直接読んで該当時刻のスプライトを割り当てる。
        var t = CurrentPlayT();
        Sprite frame = null;
        if (_clip != null)
        {
            if (_cachedClip != _clip)
                CacheClipSpriteKeys();

            var animT = t;
            if (_syncAnimToDuration && entry.Kind == EntryKind.MeleeProfile)
            {
                var duration = Mathf.Max(GetProfileValues(entry).duration, 0.0001f);
                animT = Mathf.Repeat(t, duration) / duration * _clip.length;
            }
            else
            {
                animT = _clip.length > 0f ? Mathf.Repeat(t, _clip.length) : 0f;
            }
            frame = SampleClipSprite(animT);
        }

        _previewSprite.sprite = frame != null ? frame : _playerSprite;

        _previewGo.transform.position = Vector3.zero;
        _previewGo.transform.localScale = new Vector3(_facing, 1f, 1f);

        var cam = _preview.camera;
        cam.orthographicSize = _zoom;
        cam.transform.position = new Vector3(CameraCenter(rect).x, CameraCenter(rect).y, -10f);

        _preview.BeginPreview(rect, GUIStyle.none);
        _preview.Render(false);
        var tex = _preview.EndPreview();
        GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
    }

    private Vector2 CameraCenter(Rect rect) => new Vector2(_facing * 0.9f, 0.4f);

    /// <summary>クリップからスプライト差し替えキー (SpriteRenderer.m_Sprite) を取り出してキャッシュする。</summary>
    private void CacheClipSpriteKeys()
    {
        _cachedClip = _clip;
        _spriteKeys = null;
        if (_clip == null)
            return;

        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(_clip))
        {
            if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
            {
                _spriteKeys = AnimationUtility.GetObjectReferenceCurve(_clip, binding);
                break;
            }
        }
    }

    /// <summary>指定時刻に表示されるスプライトをキーフレームから求める。</summary>
    private Sprite SampleClipSprite(float time)
    {
        if (_spriteKeys == null || _spriteKeys.Length == 0)
            return null;

        var result = _spriteKeys[0].value as Sprite;
        foreach (var key in _spriteKeys)
        {
            if (key.time <= time)
                result = key.value as Sprite;
            else
                break;
        }
        return result;
    }

    /// <summary>ワールド座標 (プレビュー基準 = プレイヤー原点) → プレビュー画面上のGUI座標。</summary>
    private Vector2 WorldToGui(Rect rect, Vector2 world)
    {
        var center = CameraCenter(rect);
        var halfH = _zoom;
        var halfW = _zoom * (rect.width / Mathf.Max(rect.height, 1f));
        var x = rect.x + (world.x - (center.x - halfW)) / (2f * halfW) * rect.width;
        var y = rect.y + ((center.y + halfH) - world.y) / (2f * halfH) * rect.height;
        return new Vector2(x, y);
    }

    /// <summary>ワールド矩形をGUI矩形へ変換して塗り+枠線で描く。</summary>
    private void DrawWorldRect(Rect rect, Vector2 worldCenter, Vector2 worldSize, Color fill, Color outline)
    {
        var min = WorldToGui(rect, worldCenter - worldSize / 2f);
        var max = WorldToGui(rect, worldCenter + worldSize / 2f);
        var gui = Rect.MinMaxRect(
            Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
            Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));

        // プレビュー領域からはみ出す部分は切る
        gui = Rect.MinMaxRect(
            Mathf.Max(gui.xMin, rect.xMin), Mathf.Max(gui.yMin, rect.yMin),
            Mathf.Min(gui.xMax, rect.xMax), Mathf.Min(gui.yMax, rect.yMax));
        if (gui.width <= 0f || gui.height <= 0f)
            return;

        EditorGUI.DrawRect(gui, fill);
        EditorGUI.DrawRect(new Rect(gui.x, gui.y, gui.width, 1f), outline);
        EditorGUI.DrawRect(new Rect(gui.x, gui.yMax - 1f, gui.width, 1f), outline);
        EditorGUI.DrawRect(new Rect(gui.x, gui.y, 1f, gui.height), outline);
        EditorGUI.DrawRect(new Rect(gui.xMax - 1f, gui.y, 1f, gui.height), outline);
    }

    /// <summary>当たり判定・地面・射線などのオーバーレイをプレビュー画面へ描く。</summary>
    private void DrawOverlays(Rect rect, Entry entry)
    {
        // 地面ライン (足元 = 基準-0.92)
        var groundL = WorldToGui(rect, new Vector2(-100f, FeetOffset));
        var groundR = WorldToGui(rect, new Vector2(100f, FeetOffset));
        var groundY = Mathf.Clamp(groundL.y, rect.yMin, rect.yMax);
        EditorGUI.DrawRect(new Rect(rect.xMin, groundY, rect.width, 1f), new Color(1f, 1f, 1f, 0.35f));

        // プレイヤーの当たり体格 (カプセル 0.7×1.84) の目安
        DrawWorldRect(rect, Vector2.zero, new Vector2(0.7f, 1.84f), Color.clear, new Color(1f, 1f, 1f, 0.35f));

        if (_showAllProfiles)
        {
            var hue = 0f;
            foreach (var e in _entries.Where(e => e.Kind == EntryKind.MeleeProfile))
            {
                var color = Color.HSVToRGB(hue, 0.7f, 1f);
                var v = GetProfileValues(e);
                DrawWorldRect(rect, new Vector2(v.offset.x * _facing, v.offset.y), v.size,
                    new Color(color.r, color.g, color.b, 0.07f), color);
                hue = Mathf.Repeat(hue + 0.13f, 1f);
            }
        }

        switch (entry.Kind)
        {
            case EntryKind.MeleeProfile: DrawMeleeOverlay(rect, entry); break;
            case EntryKind.Ranged: DrawRangedOverlay(rect, entry); break;
            case EntryKind.SkillBox: DrawSkillBoxOverlay(rect, entry); break;
            case EntryKind.Parry: DrawParryOverlay(rect); break;
        }
    }

    /// <summary>スキル (落下攻撃/大ジャンプ/横突進) の判定ボックスを描く。</summary>
    private void DrawSkillBoxOverlay(Rect rect, Entry entry)
    {
        var so = GetSerialized(entry.Asset);
        var offset = so.FindProperty(entry.OffsetPath).vector2Value;
        var size = so.FindProperty(entry.SizePath).vector2Value;
        var center = new Vector2((entry.FlipOffsetX ? _facing : 1) * offset.x, offset.y);

        var c = entry.BoxColor;
        DrawWorldRect(rect, center, size, new Color(c.r, c.g, c.b, 0.15f), c);

        // 大ジャンプの頭上判定は上昇速度でさらに上へ伸びることを薄い枠で示す
        if (entry.OffsetPath == "_superJumpHitOffset")
        {
            var extended = Mathf.Max(size.y, 1.5f);
            DrawWorldRect(rect,
                new Vector2(center.x, offset.y + extended / 2f), new Vector2(size.x, extended),
                Color.clear, new Color(c.r, c.g, c.b, 0.35f));
        }

        GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 18f),
            $"{entry.Label}  offset({offset.x:F2},{offset.y:F2}) size({size.x:F2}×{size.y:F2})",
            EditorStyles.whiteMiniLabel);
    }

    /// <summary>パリィ: 全身が判定。受付中=水色 / 硬直中=灰色 で体を塗って再生に追従する。</summary>
    private void DrawParryOverlay(Rect rect)
    {
        if (_consts == null)
            return;

        var window = _consts.ParryWindow;
        var recovery = _consts.ParryRecovery;
        var t = CurrentPlayT();

        Color color;
        string phase;
        if (t < window)
        {
            color = new Color(0.5f, 0.9f, 1f);
            phase = $"受付中 ({Mathf.RoundToInt((window - t) * 60f)}F 残り)";
        }
        else if (t < window + recovery)
        {
            color = new Color(0.55f, 0.55f, 0.55f);
            phase = $"硬直中 ({Mathf.RoundToInt((window + recovery - t) * 60f)}F 残り)";
        }
        else
        {
            color = new Color(0.5f, 0.9f, 1f, 0.4f);
            phase = "終了";
        }

        // パリィで防げる範囲 (ヒット位置がこの中なら無効化) を塗る
        var offset = _consts.ParryRangeOffset;
        var center = new Vector2(offset.x * _facing, offset.y);
        DrawWorldRect(rect, center, _consts.ParryRangeSize,
            new Color(color.r, color.g, color.b, 0.22f), color);

        GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 18f),
            $"パリィ {phase}  受付{Mathf.RoundToInt(window * 60f)}F / 硬直{Mathf.RoundToInt(recovery * 60f)}F / 成功時無敵{Mathf.RoundToInt(_consts.ParrySuccessInvincible * 60f)}F",
            EditorStyles.whiteMiniLabel);
    }

    private void DrawMeleeOverlay(Rect rect, Entry entry)
    {
        var v = GetProfileValues(entry);
        var center = new Vector2(v.offset.x * _facing, v.offset.y);

        var outline = Color.yellow;
        var fillAlpha = 0.12f;
        if (v.duration > 0f)
        {
            var swingT = Mathf.Repeat(CurrentPlayT(), v.duration);
            if (swingT < v.hitDelay)
                outline = Color.gray;
            else if (swingT < v.hitDelay + 0.07f)
            {
                outline = Color.red;
                fillAlpha = 0.35f;
            }
            else
                outline = new Color(0.35f, 0.6f, 1f);
        }

        DrawWorldRect(rect, center, v.size, new Color(outline.r, outline.g, outline.b, fillAlpha), outline);

        var label = $"HP{v.hp}/G{v.guard}  発生{Mathf.RoundToInt(v.hitDelay * 60f)}F/全体{Mathf.RoundToInt(v.duration * 60f)}F";
        GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 18f), label, EditorStyles.whiteMiniLabel);
    }

    private void DrawRangedOverlay(Rect rect, Entry entry)
    {
        var so = GetSerialized(entry.Asset);
        var speed = so.FindProperty("_speed")?.floatValue ?? 0f;
        var lifetime = so.FindProperty("_lifetime")?.floatValue ?? 0f;
        var useDelay = so.FindProperty("_useDelay")?.floatValue ?? 0f;
        var fireHeight = so.FindProperty("_fireHeightOffset")?.floatValue ?? 0f;
        var bulletSize = new Vector2(0.5f, 0.36f); // NeedleShot.prefab の当たり

        var origin = new Vector2(0.5f * _facing, fireHeight);
        var end = origin + new Vector2(_facing * speed * lifetime, 0f);

        // 射線
        var a = WorldToGui(rect, origin);
        var b = WorldToGui(rect, end);
        var lineRect = Rect.MinMaxRect(
            Mathf.Clamp(Mathf.Min(a.x, b.x), rect.xMin, rect.xMax), a.y - 1f,
            Mathf.Clamp(Mathf.Max(a.x, b.x), rect.xMin, rect.xMax), a.y + 1f);
        EditorGUI.DrawRect(lineRect, new Color(0.95f, 0.9f, 0.5f, 0.7f));

        // 発射位置と最大射程の弾
        DrawWorldRect(rect, origin, new Vector2(0.15f, 0.15f), Color.clear, new Color(0.95f, 0.9f, 0.5f));
        DrawWorldRect(rect, end, bulletSize, Color.clear, new Color(0.95f, 0.9f, 0.5f, 0.5f));

        // 再生中の弾
        var t = CurrentPlayT();
        if (t >= useDelay)
        {
            var flyT = Mathf.Min(t - useDelay, lifetime);
            var pos = origin + new Vector2(_facing * speed * flyT, 0f);
            DrawWorldRect(rect, pos, bulletSize, new Color(1f, 0.3f, 0.3f, 0.3f), Color.red);
        }

        GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 18f),
            $"射程 {speed * lifetime:F1}  発射まで {Mathf.RoundToInt(useDelay * 60f)}F", EditorStyles.whiteMiniLabel);
    }

    // ------------------------------------------------------------ 編集フィールド

    private void DrawMeleeEditor(SerializedObject so, Entry entry)
    {
        var profile = so.FindProperty(entry.ProfilePath);
        if (profile == null)
        {
            EditorGUILayout.HelpBox($"プロパティ {entry.ProfilePath} が見つかりません。", MessageType.Error);
            return;
        }

        // 折りたたみに隠さず、全パラメータ (フレーム/ダメージ/範囲) を直接編集できるように並べる
        foreach (var (name, label) in new[]
        {
            ("_duration", "全体時間 (sec)"),
            ("_hitDelay", "発生までの時間 (sec)"),
            ("_hpDamage", "HPダメージ"),
            ("_guardDamage", "防御値ダメージ"),
            ("_offset", "攻撃範囲: 中心オフセット"),
            ("_boxSize", "攻撃範囲: サイズ"),
        })
        {
            var prop = profile.FindPropertyRelative(name);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label));
        }
        EditorGUILayout.LabelField("オフセット x は向いている方向へ反転します", EditorStyles.miniLabel);

        var duration = profile.FindPropertyRelative("_duration").floatValue;
        var hitDelay = profile.FindPropertyRelative("_hitDelay").floatValue;
        EditorGUILayout.LabelField(
            $"60fps換算: 発生 {Mathf.RoundToInt(hitDelay * 60f)}F / 全体 {Mathf.RoundToInt(duration * 60f)}F",
            EditorStyles.miniLabel);

        DrawTimelineBar(duration, hitDelay);
    }

    private void DrawRangedEditor(SerializedObject so)
    {
        foreach (var name in new[] { "_useDuration", "_useDelay", "_cooldown", "_speed", "_lifetime", "_hpDamage", "_guardDamage", "_fireHeightOffset" })
        {
            var prop = so.FindProperty(name);
            if (prop != null)
                EditorGUILayout.PropertyField(prop);
        }

        var speed = so.FindProperty("_speed")?.floatValue ?? 0f;
        var lifetime = so.FindProperty("_lifetime")?.floatValue ?? 0f;
        var useDelay = so.FindProperty("_useDelay")?.floatValue ?? 0f;
        EditorGUILayout.LabelField(
            $"射程 = 速度×寿命 = {speed * lifetime:F1} units / 発射まで {Mathf.RoundToInt(useDelay * 60f)}F",
            EditorStyles.miniLabel);

        DrawTimelineBar(so.FindProperty("_useDuration")?.floatValue ?? 0.3f, useDelay);
    }

    private void DrawSkillBoxEditor(SerializedObject so, Entry entry)
    {
        EditorGUILayout.PropertyField(so.FindProperty(entry.OffsetPath), new GUIContent("中心オフセット"));
        EditorGUILayout.PropertyField(so.FindProperty(entry.SizePath), new GUIContent("サイズ"));

        if (entry.OffsetPath == "_superJumpHitOffset")
            EditorGUILayout.LabelField("上昇速度に応じて縦へ自動で伸びます (薄い枠が伸長の目安)", EditorStyles.miniLabel);
        if (entry.FlipOffsetX)
            EditorGUILayout.LabelField("オフセット x は向いている方向へ反転します", EditorStyles.miniLabel);
    }

    private void DrawParryEditor(SerializedObject so)
    {
        foreach (var name in new[]
        {
            "_parryWindow", "_parryRecovery", "_parrySuccessInvincible",
            "_parryRangeOffset", "_parryRangeSize",
        })
        {
            var prop = so.FindProperty(name);
            if (prop != null)
                EditorGUILayout.PropertyField(prop);
        }
        EditorGUILayout.LabelField("範囲: 攻撃のヒット位置がこの範囲内なら無効化。オフセット x は向きで反転", EditorStyles.miniLabel);

        var window = so.FindProperty("_parryWindow")?.floatValue ?? 0f;
        var recovery = so.FindProperty("_parryRecovery")?.floatValue ?? 0f;
        EditorGUILayout.LabelField(
            $"60fps換算: 受付 {Mathf.RoundToInt(window * 60f)}F → 硬直 {Mathf.RoundToInt(recovery * 60f)}F",
            EditorStyles.miniLabel);

        // 受付=水色 / 硬直=灰 の帯 + 再生カーソル
        var total = Mathf.Max(window + recovery, 0.0001f);
        var rect = GUILayoutUtility.GetRect(10f, 18f, GUILayout.ExpandWidth(true));
        rect = new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, rect.height - 4f);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * (window / total), rect.height), new Color(0.5f, 0.9f, 1f));
        EditorGUI.DrawRect(new Rect(rect.x + rect.width * (window / total), rect.y, rect.width * (recovery / total), rect.height), new Color(0.45f, 0.45f, 0.45f));
        var cursor = Mathf.Clamp01(CurrentPlayT() / total);
        EditorGUI.DrawRect(new Rect(rect.x + rect.width * cursor - 1f, rect.y, 2f, rect.height), Color.white);
    }

    /// <summary>Duration の帯に HitDelay (発生) マーカーと再生カーソルを描くタイムラインバー。</summary>
    private void DrawTimelineBar(float duration, float hitDelay)
    {
        var rect = GUILayoutUtility.GetRect(10f, 18f, GUILayout.ExpandWidth(true));
        rect = new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, rect.height - 4f);
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

        if (duration <= 0f)
            return;

        // 発生前 (溜め) = 灰 / 発生以降 = 青
        var delayRatio = Mathf.Clamp01(hitDelay / duration);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * delayRatio, rect.height), new Color(0.45f, 0.45f, 0.45f));
        EditorGUI.DrawRect(new Rect(rect.x + rect.width * delayRatio, rect.y, rect.width * (1f - delayRatio), rect.height), new Color(0.25f, 0.45f, 0.7f));
        // 発生マーカー
        EditorGUI.DrawRect(new Rect(rect.x + rect.width * delayRatio - 1f, rect.y, 2f, rect.height), Color.red);
        // 再生カーソル
        var cursorT = Mathf.Repeat(CurrentPlayT(), duration);
        EditorGUI.DrawRect(new Rect(rect.x + rect.width * Mathf.Clamp01(cursorT / duration) - 1f, rect.y, 2f, rect.height), Color.white);
    }

    private void TogglePlay(bool combo)
    {
        if (_isPlaying && _comboPlay == combo)
        {
            _isPlaying = false;
            return;
        }

        _isPlaying = true;
        _comboPlay = combo;
        _playT = 0f;
        _lastTick = EditorApplication.timeSinceStartup;
    }

    /// <summary>パリィ/コンボ/スキルの共通定数もこのウィンドウから編集できるようにする。</summary>
    private void DrawSkillConstsSection()
    {
        if (_consts == null)
            return;

        _skillFoldout = EditorGUILayout.Foldout(_skillFoldout, "パリィ / コンボ / スキル定数 (PlayerConsts)", true, EditorStyles.foldoutHeader);
        if (!_skillFoldout)
            return;

        var so = GetSerialized(_consts);
        so.Update();
        foreach (var name in new[]
        {
            "_parryWindow", "_parryRecovery", "_parrySuccessInvincible",
            "_baseMaxCombo", "_maxComboCap", "_forgeAttackBonus",
            "_slamFallSpeed", "_skillChargeTime", "_superJumpHeight", "_chargeRushSpeed",
        })
        {
            var prop = so.FindProperty(name);
            if (prop != null)
                EditorGUILayout.PropertyField(prop);
        }
        so.ApplyModifiedProperties();

        // パリィのタイミング帯 (水色=受付 / 灰=硬直)
        var window = _consts.ParryWindow;
        var recovery = _consts.ParryRecovery;
        var total = Mathf.Max(window + recovery, 0.0001f);
        var rect = GUILayoutUtility.GetRect(10f, 14f, GUILayout.ExpandWidth(true));
        rect = new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, rect.height - 4f);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * (window / total), rect.height), new Color(0.5f, 0.9f, 1f));
        EditorGUI.DrawRect(new Rect(rect.x + rect.width * (window / total), rect.y, rect.width * (recovery / total), rect.height), new Color(0.45f, 0.45f, 0.45f));
        EditorGUILayout.LabelField(
            $"パリィ: 受付 {Mathf.RoundToInt(window * 60f)}F → 硬直 {Mathf.RoundToInt(recovery * 60f)}F",
            EditorStyles.miniLabel);
    }

    // ------------------------------------------------------------ 再生の進行

    private void Tick()
    {
        if (!_isPlaying)
            return;

        var now = EditorApplication.timeSinceStartup;
        _playT += (float)(now - _lastTick);
        _lastTick = now;

        // 再生終了判定
        var entry = _entries.ElementAtOrDefault(_selected);
        if (entry != null)
        {
            var total = GetPlayTotal(entry);
            if (_playT >= total)
            {
                _isPlaying = false;
                _scrub = 0f;
            }
        }

        Repaint();
    }

    private float GetPlayTotal(Entry entry)
    {
        switch (entry.Kind)
        {
            case EntryKind.Ranged:
            {
                var so = GetSerialized(entry.Asset);
                return (so.FindProperty("_useDelay")?.floatValue ?? 0f) + (so.FindProperty("_lifetime")?.floatValue ?? 0f);
            }
            case EntryKind.Parry:
                return _consts != null ? _consts.ParryWindow + _consts.ParryRecovery : 0.5f;
            case EntryKind.SkillBox:
                return 0.0001f; // 時間の概念がない (再生ボタンは無効化される)
        }

        var duration = GetProfileValues(entry).duration;
        var combo = _comboPlay && _consts != null ? _consts.BaseMaxCombo : 1;
        return duration * combo;
    }

    private (float duration, float hitDelay, int hp, int guard, Vector2 offset, Vector2 size) GetProfileValues(Entry entry)
    {
        var so = GetSerialized(entry.Asset);
        var p = so.FindProperty(entry.ProfilePath);
        return (
            p.FindPropertyRelative("_duration").floatValue,
            p.FindPropertyRelative("_hitDelay").floatValue,
            p.FindPropertyRelative("_hpDamage").intValue,
            p.FindPropertyRelative("_guardDamage").intValue,
            p.FindPropertyRelative("_offset").vector2Value,
            p.FindPropertyRelative("_boxSize").vector2Value);
    }
}
