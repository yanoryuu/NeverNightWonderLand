// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	public class SampleCustomSaveLoadButton : MonoBehaviour
	{
		//セーブロード画面の制御コンポーネント
		UtageUguiSaveLoad SaveLoad => this.GetComponentCacheInParent(ref saveLoad);
		UtageUguiSaveLoad saveLoad;

		AdvEngine Engine => SaveLoad.Engine;

		UtageUguiSaveLoadItem SaveLoadItem => this.GetComponentCache(ref saveLoadItem);
		[SerializeField] UtageUguiSaveLoadItem saveLoadItem;
		
		//セーブデータの削除
		public void OnClickDelete()
		{
			//セーブデータの削除
			Engine.SaveManager.DeleteSaveData(SaveLoadItem.Data);
			//セーブボタンの表示の更新
			SaveLoadItem.Refresh(SaveLoad.IsSave);
		}
	}
}
