using TMPro;
using UnityEngine;

/// <summary>
/// エリア進入時のエリア名タイトル表示。フェードイン→ホールド→フェードアウトする。
/// 置き方は 2 通りあり、置かれたシーンで自動的に切り替わる:
/// - UI シーン (StageUI) に置いた場合 (現行): ステージ側の <see cref="StageInfo"/> がステージに入るたびに
///   <see cref="StageInfo.ConsumeEntryTitle"/> の名前を表示する (エリアのシーンはエリア名を初回だけ、それ以外は部屋名)。StageInfo を持たないステージ (GameField_old) では何も表示しない
///   (旧ステージは自前の SceneUI がタイトルを出すので二重表示にならない)。
/// - ステージに直接置いた場合 (旧方式: GameField_old の SceneUI): シーン開始時に _areaName を表示し、
///   シーン開始処理 (ポーズ解除・リザルト集計リセット) も兼ねる。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class AreaTitleView : MonoBehaviour
{
    private const float FadeIn = 0.5f;
    private const float Hold = 1.8f;
    private const float FadeOut = 0.8f;
    private const float MaxStep = 0.1f;

    [Tooltip("エリア名 (旧方式でステージに直接置いた時だけ使う。UI シーンでは StageInfo の名前を表示する)")]
    [SerializeField] private string _areaName = "";

    [Tooltip("エリア名のテキスト")]
    [SerializeField] private TMP_Text _label;

    /// <summary>旧方式でステージに直接置いた時のエリア名 (セーブ画面の「場所」の代用に使う)。</summary>
    public string AreaName => _areaName;

    private CanvasGroup _group;
    private float _elapsed;
    private bool _inUIScene;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;

        _inUIScene = gameObject.scene.name.EndsWith("UI");
        if (_inUIScene)
        {
            enabled = false; // ステージに入るまで表示しない
            StageInfo.Entered += OnStageEntered;

            // ステージが UI シーンより先にロードされていた場合 (ステージを直接開いて再生) は、ここで表示する
            // (無効のままだと Start が呼ばれないので Awake で行う)
            if (StageInfo.Current != null)
                Play(StageInfo.Current.ConsumeEntryTitle());
            return;
        }

        // 旧方式: シーン開始のリセット処理
        GamePause.Reset();
        GameSession.ResetRun();
    }

    private void OnDestroy()
    {
        StageInfo.Entered -= OnStageEntered;
    }

    private void Start()
    {
        if (_inUIScene)
            return;

        // 旧方式: Awake から経過時間を数えて表示する (従来と同じ動き)
        if (_label != null)
            _label.text = _areaName;
    }

    private void OnStageEntered(StageInfo stage)
    {
        Play(stage != null ? stage.ConsumeEntryTitle() : "");
    }

    /// <summary>エリア名を表示する (表示中なら最初からやり直す)。名前が空なら何もしない。</summary>
    public void Play(string areaName)
    {
        if (string.IsNullOrEmpty(areaName))
            return;

        if (_label != null)
            _label.text = areaName;

        _elapsed = 0f;
        _group.alpha = 0f;
        enabled = true;
    }

    private void Update()
    {
        // ロード直後の 1 フレームは unscaledDeltaTime が大きく、表示が飛ばされることがあるので上限を付ける
        _elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStep);

        if (_elapsed < FadeIn)
            _group.alpha = _elapsed / FadeIn;
        else if (_elapsed < FadeIn + Hold)
            _group.alpha = 1f;
        else if (_elapsed < FadeIn + Hold + FadeOut)
            _group.alpha = 1f - (_elapsed - FadeIn - Hold) / FadeOut;
        else
        {
            _group.alpha = 0f;
            enabled = false; // 表示が終わったら更新停止
        }
    }
}
