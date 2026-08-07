using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
    // 選択肢のキーボード入力を制御するクラス
    public class SampleSelectionKeyboardInput : MonoBehaviour
    {
        AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
        [SerializeField] private AdvEngine engine;
        
        AdvUguiSelectionManager UguiSelectionManager => this.GetComponentCacheInChildren(ref uguiSelectionManager);
        [SerializeField] AdvUguiSelectionManager uguiSelectionManager;
        
        void Awake()
        {
            Engine.SelectionManager.OnBeginWaitInput.AddListener(OnInit);
        }

        void OnInit(AdvSelectionManager selectionManager)
        {
            if(UguiSelectionManager.Items.Count <= 0)  return;

            //最初のボタンを選択状態にする
            EventSystem.current.SetSelectedGameObject(UguiSelectionManager.Items[0]);
            
            //上下のボタンで選択肢のボタンを移動する
            for (int i = 0; i < UguiSelectionManager.Items.Count; i++)
            {
                var button = UguiSelectionManager.Items[i].GetComponent<Button>();
                int count = UguiSelectionManager.Items.Count;
                int prevIndex = (i - 1 + count)% count;
                int nextIndex = (i + 1) % count;
                var prevButton = UguiSelectionManager.Items[prevIndex].GetComponent<Button>();
                var nextButton = UguiSelectionManager.Items[nextIndex].GetComponent<Button>();
                
                //ボタンの移動先を設定
                button.navigation = new Navigation {mode = Navigation.Mode.Explicit, selectOnUp = prevButton, selectOnDown = nextButton};
            }
        }
    }
}