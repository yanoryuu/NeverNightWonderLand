using System;
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
    //TIPSリストのボタンの初期化処理をカスタムするサンプル
    public class SampleTipsButtonCustom : MonoBehaviour
    {
        AdvUguiTipsListButton Button => this.GetComponentCache(ref button);
        AdvUguiTipsListButton button;
            
        void Awake()
        {
            Button.OnInit.AddListener(OnInit);
        }

        //TIPSリストのボタンの追加初期化処理
        //AdvUguiTipsListButtonのOnInitイベントに登録
        void OnInit()
        {
            this.GetComponent<Button>().interactable = Button.TipsInfo.IsOpened;
        }
    }

}