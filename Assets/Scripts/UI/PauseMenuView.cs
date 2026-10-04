using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

/// <summary>
/// ポーズメニュー。Esc / Menu ボタンで開閉し、ゲームを一時停止する。
/// 完成形 pause/ui_アートボード 1 のコピー 4.png の通りに事前配置したボタン 5 つ:
/// 再開 / セーブ (拠点 = SavePoint の近くでのみ有効) / 設定 (未実装のため無効) / メインメニューに戻る (確認あり) / ゲームを終了。
/// メニュー UI を Additive シーンに分離できるよう、プレイヤーは実行時に解決する
/// (PlayerRuntime 注入 → シリアライズ参照 → シーン検索 の順)。
/// </summary>
public class PauseMenuView : MonoBehaviour
{
    private const int SaveIndex = 1;

    [Tooltip("参照するプレイヤー (任意。未設定なら実行時に解決する)")]
    [SerializeField] private PlayerController _player;

    [Tooltip("タイトルシーン名")]
    [SerializeField] private string _titleSceneName = "TitleScene";

    [Tooltip("ボタンのメニュー (プレハブ上で事前配置)")]
    [SerializeField] private ImageMenuView _menu;

    [Tooltip("「メインメニューに戻る」の確認 (はい / いいえ)。ポーズ画面の上に重ねる")]
    [SerializeField] private ImageMenuView _confirm;

    private PlayerRuntime _playerRuntime;
    private bool _isPaused;     // 自分がポーズを掛けている間 true (セーブ画面を開いている間も含む)
    private SavePoint _savePoint; // 開いた時に近くにあった拠点
    private int _closedFrame = -1; // 閉じたのと同じ押下で開き直さないように

    [Inject]
    public void Construct(PlayerRuntime playerRuntime)
    {
        _playerRuntime = playerRuntime;
    }

    /// <summary>現在のプレイヤー。Additive シーン運用でもシーンをまたいで解決できる。</summary>
    private PlayerController Player
    {
        get
        {
            if (_playerRuntime != null && _playerRuntime.Current.CurrentValue != null)
                return _playerRuntime.Current.CurrentValue;

            if (_player == null)
                _player = FindAnyObjectByType<PlayerController>();
            return _player;
        }
    }

    private void Awake()
    {
        if (_menu == null)
        {
            Debug.LogError($"[{nameof(PauseMenuView)}] ImageMenuView が設定されていません。", this);
            enabled = false;
            return;
        }

        _menu.Initialize();
        _menu.OnCancelled += CloseMenu;
        if (_confirm != null)
        {
            _confirm.Initialize();
            _confirm.OnCancelled += CloseConfirm;
        }
    }

    private void Update()
    {
        if (_isPaused || Time.frameCount == _closedFrame)
            return;

        // 他のメニュー (拠点・リザルト・アイテム画面等) が開いている間と、それが閉じたフレーム (同じ Esc) は反応しない
        if (MenuPanelView.AnyOpen || Time.frameCount == MenuPanelView.LastClosedFrame)
            return;

        var player = Player;
        if (player != null && player.IsDead)
            return;

        // Esc / Start (PlayerInputSource 経由。テストプレイの仮想入力からも開ける)
        if (PlayerInputSource.Current.PausePressed)
            OpenMenu();
    }

    private void OpenMenu()
    {
        _isPaused = true;
        GamePause.Push();
        ShowMain(0);
    }

    private void ShowMain(int selectIndex)
    {
        _savePoint = FindNearbySavePoint();
        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new(null, CloseMenu),
            new(null, OpenSave, _savePoint != null),
            new(null, null, false), // 設定 (未実装)
            new(null, OpenConfirm),
            new(null, QuitGame),
        }, selectIndex);
        _menu.Open();
    }

    private void CloseMenu()
    {
        CloseConfirm();
        _menu.Close();
        _closedFrame = Time.frameCount;
        if (_isPaused)
        {
            _isPaused = false;
            GamePause.Pop();
        }
    }

    /// <summary>プレイヤーが拠点 (SavePoint) のインタラクト範囲にいれば、その拠点を返す。</summary>
    private SavePoint FindNearbySavePoint()
    {
        var player = Player;
        var interactor = player != null ? player.GetComponent<PlayerInteractor>() : null;
        return interactor != null ? interactor.FindInRange<SavePoint>() : null;
    }

    private void OpenSave()
    {
        var player = Player;
        var bridge = player != null ? player.GetComponent<PlayerSaveBridge>() : null;
        var slots = FindFirstObjectByType<SaveSlotView>();
        if (bridge == null || slots == null)
        {
            Debug.LogWarning("[PauseMenuView] セーブできません (PlayerSaveBridge / SaveSlotUI シーンが見つからない)。", this);
            return;
        }

        var savePoint = _savePoint;
        _menu.Close();
        slots.Open(SaveSlotView.Mode.Save, slot =>
        {
            SaveSystem.ActiveSlot = slot;
            SaveSystem.Save(slot, bridge.CollectAt(savePoint));
            GameSession.SaveBelongsToRun = true;
            Notifier.Notify("セーブしました");
            ShowMain(SaveIndex);
        }, () => ShowMain(SaveIndex), showBackground: true);
    }

    /// <summary>「メインメニューに戻る」の確認を重ねる (既定の選択は「いいえ」)。</summary>
    private void OpenConfirm()
    {
        if (_confirm == null)
        {
            GoTitle();
            return;
        }

        _menu.Interactable = false;
        _confirm.SetEntries(new List<MenuPanelView.Entry>
        {
            new(null, GoTitle),      // はい
            new(null, CloseConfirm), // いいえ
        }, 1);
        _confirm.Open();
    }

    private void CloseConfirm()
    {
        if (_confirm != null)
            _confirm.Close();
        _menu.Interactable = true;
    }

    private void GoTitle()
    {
        CloseConfirm();
        _menu.Close();
        _isPaused = false;
        GamePause.Reset();
        SceneManager.LoadScene(_titleSceneName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
