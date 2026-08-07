// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// インデックス管理できるToggledGroup
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiToggleGroupIndexed")]
	public class UguiToggleGroupIndexed : MonoBehaviour
	{
		public Toggle[] TogglesToArray {get { return this.toggles.ToArray(); }}
		[SerializeField]
		protected List<Toggle> toggles = new List<Toggle>();

		public int firstIndexOnAwake = 0;
		public bool ignoreValueChangeOnAwake = true;

		public bool autoToggleInteractiveOff = true;


		//シフト移動時にループするか
		public bool isLoopShift = true;

		//左にシフトするボタン
		public Button shiftLeftButton;
		//右にシフトするボタン
		public Button shiftRightButton;
		//左端にジャンプするボタン
		public Button jumpLeftEdgeButton;
		//右端にジャンプするボタン
		public Button jumpRightEdgeButton;

		//現在のインデックス
		public virtual int CurrentIndex
		{
			get { return currentIndex; }
			set
			{
				if (value < toggles.Count)
				{
					for( int i = 0; i < toggles.Count; ++i )
					{
						bool isOn = ( i == value);
						toggles[i].isOn = isOn;
						if(autoToggleInteractiveOff)
						{
							toggles[i].interactable = !isOn;
						}						
//						Debug.Log( i  + " " + toggles[i].isOn );
					}
					if(currentIndex!=value)
					{
						currentIndex = value;
						this.OnValueChanged.Invoke(value);
					}
				}
			}
		}
		protected int currentIndex = -1;

		//ボタンの数
		public int Count
		{
			get { return toggles.Count; }
		}

		
		[System.Serializable]
		public class UguiTabButtonGroupEvent : UnityEvent<int> { };
		public UguiTabButtonGroupEvent OnValueChanged;

		protected virtual void Awake()
		{
			for( int i = 0; i < toggles.Count; ++i )
			{
				Toggle toggle = toggles[i];
				toggle.onValueChanged.AddListener((bool isOn) => OnToggleValueChanged(toggle));
			}
			BroadcastToggleIndex();
			
			if(ignoreValueChangeOnAwake) currentIndex = firstIndexOnAwake;
			CurrentIndex = firstIndexOnAwake;

			if (shiftLeftButton) shiftLeftButton.onClick.AddListener(ShiftLeft);
			if (shiftRightButton) shiftRightButton.onClick.AddListener(ShiftRight);
			if (jumpLeftEdgeButton) jumpLeftEdgeButton.onClick.AddListener(JumpLeftEdge);
			if (jumpRightEdgeButton) jumpRightEdgeButton.onClick.AddListener(JumpRightEdge);
		}

		protected bool isIgnoreValueChange;
		protected virtual void OnToggleValueChanged( Toggle toggle )
		{
//			Debug.Log (toggle.name + " " + toggle.isOn);
			if (isIgnoreValueChange) return;
			isIgnoreValueChange = true;
			CurrentIndex = toggles.FindIndex( (Toggle obj) => (obj == toggle) );
//			Debug.Log (CurrentIndex);
//			Debug.Log ( "Real " + toggles.FindIndex( (Toggle obj) => obj.isOn ) );
			isIgnoreValueChange = false;
		}
		
		//管理対象のToggleを追加
		//BroadcastToggleIndexが何度も呼ばれてしまうので、複数Toggleを追加する場合はなるべくAddTogglesを使う
		public virtual void Add(Toggle toggle)
		{
			toggles.Add(toggle);
			toggle.onValueChanged.AddListener((bool isOn) => OnToggleValueChanged(toggle));
			BroadcastToggleIndex();
		}

		//管理対象のToggleを複数追加
		public virtual void AddToggles( IEnumerable<Toggle> newToggles)
		{
			foreach (var toggle in newToggles)
			{
				toggles.Add(toggle);
				toggle.onValueChanged.AddListener((bool isOn) => OnToggleValueChanged(toggle));
			}
			BroadcastToggleIndex();
		}
		
		//Toggleのインデックスを子オブジェクト以下のコンポーネントに送信
		protected virtual void BroadcastToggleIndex()
		{
			int count = toggles.Count;
			for (var i = 0; i < count; i++)
			{
				var toggle = toggles[i];
				//インデックスを設定するインターフェースがある場合は、インデックスを設定
				foreach (var component in toggle.GetComponentsInChildren<IUguiIndex>())
				{
					component.SetIndex(i, count);
				}
			}

		}

		public virtual void ClearToggles()
		{
			toggles.Clear();
		}

		public virtual void SetActiveLRButtons(bool isActive)
		{
			if (shiftLeftButton) shiftLeftButton.gameObject.SetActive(isActive);
			if (shiftRightButton) shiftRightButton.gameObject.SetActive(isActive);
			if (jumpLeftEdgeButton) jumpLeftEdgeButton.gameObject.SetActive(isActive);
			if (jumpRightEdgeButton) jumpRightEdgeButton.gameObject.SetActive(isActive);
		}

		//左にシフト
		public virtual void ShiftLeft()
		{
			if (Count <= 0) return;

			int index = CurrentIndex - 1;
			if (index < 0)
			{
				index = (isLoopShift) ? Count - 1 : 0;
			}
			CurrentIndex = index;
		}

		//右にシフト
		public virtual void ShiftRight()
		{
			if (Count <= 0) return;

			int index = CurrentIndex + 1;
			if (index >= Count)
			{
				index = (isLoopShift) ? 0 : Count - 1;
			}
			CurrentIndex = index;
		}

		//左端に移動
		public virtual void JumpLeftEdge()
		{
			if (Count <= 0) return;
			CurrentIndex = 0;
		}

		//右端に移動
		public virtual void JumpRightEdge()
		{
			if (Count <= 0) return;
			CurrentIndex = Count - 1;
		}
	}
}
