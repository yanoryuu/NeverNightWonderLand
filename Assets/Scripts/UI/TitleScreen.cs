using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// タイトル画面 (2 画面 + スロット選択)。UI は実行時に生成せず、シーン上で事前配置したものを参照する。
/// (1) PRESS ANY BUTTON: 何かボタンで (2) へ。
/// (2) メニュー: はじめから / つづきから / 設定 / トロフィー (設定・トロフィーは未実装のため無効表示)。戻るで (1) へ。
/// はじめから → 記録先スロットの選択 → 開始ステージへ。つづきから → ロードするスロットの選択 → セーブ地点へ。
/// </summary>
public class TitleScreen : MonoBehaviour
{
    private enum State { PressAny, Menu, Slots }

    [Tooltip("「はじめから」で読み込むシーン名")]
    [SerializeField] private string _startSceneName = "A1_T1";

    [Header("参照 (シーン上で事前配置)")]
    [Tooltip("画面 (1) PRESS ANY BUTTON のルート")]
    [SerializeField] private GameObject _pressAnyScreen;

    [Tooltip("画面 (2) とスロット画面の背景 (完成形から切り出した仮の背景)")]
    [SerializeField] private GameObject _menuBackground;

    [Tooltip("画面 (2) のメニュー (紙 + ボタン 4 つ + 戻る矢印)")]
    [SerializeField] private ImageMenuView _menu;

    [Tooltip("セーブスロット画面 (SaveSlotUI プレハブ)")]
    [SerializeField] private SaveSlotView _slots;

    private State _state;
    private int _stateFrame;

    private void Awake()
    {
        GamePause.Reset();
    }

    private void Start()
    {
        if (_menu == null || _slots == null)
        {
            Debug.LogError($"[{nameof(TitleScreen)}] メニュー / スロット画面が設定されていません。", this);
            return;
        }

        _menu.Initialize();
        _menu.OnCancelled += ShowPressAny;
        ShowPressAny();
    }

    private void Update()
    {
        // 画面 (1): 何かボタンで (2) へ (切り替えたのと同じフレームの押下は無視する)
        if (_state == State.PressAny && Time.frameCount != _stateFrame && MenuInput.AnyButtonPressed())
            ShowMenu(0);
    }

    private void SetState(State state)
    {
        _state = state;
        _stateFrame = Time.frameCount;
        if (_pressAnyScreen != null)
            _pressAnyScreen.SetActive(state == State.PressAny);
        if (_menuBackground != null)
            _menuBackground.SetActive(state != State.PressAny);
    }

    private void ShowPressAny()
    {
        _menu.Close();
        SetState(State.PressAny);
    }

    private void ShowMenu(int selectIndex)
    {
        SetState(State.Menu);
        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new(null, StartNewGame),
            new(null, ContinueGame, SaveSystem.AnyExists()),
            new(null, null, false), // 設定 (未実装)
            new(null, null, false), // トロフィー (未実装)
        }, selectIndex);
        _menu.Open();
    }

    private void StartNewGame()
    {
        _menu.Close();
        SetState(State.Slots);
        _slots.Open(SaveSlotView.Mode.NewGame, slot =>
        {
            SaveSystem.ActiveSlot = slot;
            GameSession.PendingLoad = null;
            GameSession.PlayTimeSeconds = 0;
            GameSession.SaveBelongsToRun = false; // スロットに前のプレイのセーブが残っていても使わない
            GameSession.RunSlotChosen = true;     // 拠点で休んだ時にこのスロットへ書く (上書きはスロット画面で確認済み)
            GameSession.ClearRespawnBench();      // 拠点で休むまで、死亡リトライは部屋の入り口から
            GameProgress.Clear();
            MapExploration.Clear();
            // PlayerScene を土台に開始ステージを Additive で重ねる (PlayerScene 未登録なら旧方式)
            StageLoader.LoadWithPlayerScene(_startSceneName);
        }, () => ShowMenu(0), showBackground: false);
    }

    private void ContinueGame()
    {
        _menu.Close();
        SetState(State.Slots);
        _slots.Open(SaveSlotView.Mode.Load, slot =>
        {
            var save = SaveSystem.TryLoad(slot);
            if (save == null || string.IsNullOrEmpty(save.sceneName))
            {
                Notifier.Notify("セーブデータが読み込めなかった");
                ShowMenu(1);
                return;
            }

            SaveSystem.ActiveSlot = slot;
            save.continuesRun = true; // 以後の進行はこのスロットへ自動で書き込む
            GameSession.PendingLoad = save;
            GameSession.SaveBelongsToRun = true;
            GameSession.RunSlotChosen = true;
            GameSession.SetRespawnBench(save.sceneName, new Vector2(save.posX, save.posY)); // セーブした拠点
            GameSession.PlayTimeSeconds = save.playTimeSeconds;
            StageLoader.LoadWithPlayerScene(save.sceneName);
        }, () => ShowMenu(1), showBackground: false);
    }
}
