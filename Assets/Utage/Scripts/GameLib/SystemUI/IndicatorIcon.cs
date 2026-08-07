// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// インジケーター表示コンポーネント
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/IndicatorIcon")]
	public class IndicatorIcon : MonoBehaviour
	{
		[SerializeField]
		GameObject icon = null;				//回転するアイコン

		[SerializeField]
		float animTime = 1.0f / 12;		//アニメーション時間

		[SerializeField]
		float animRotZ = -36;			//アニメーションする角度

		[SerializeField]
		bool isDeviceIndicator = false;	//デバイスのインジケーターを使うか？

		bool isStarting = false;
		float rotZ = 0;
		List<System.Object> objList = new List<object>();

		void Awake()
		{
			if (IsDeviceIndicator())
			{
				WrapperUnityVersion.SetActivityIndicatorStyle();
				icon.SetActive(false);
			}
		}

		/// <summary>
		/// インジケーターの表示開始
		/// 表示要求しているオブジェクトはあちこちから設定できる。
		/// 全ての要求が終了しない限りは表示を続ける
		/// </summary>
		/// <param name="obj">表示を要求してるオブジェクト</param>

		public void StartIndicator(System.Object obj)
		{
			IncRef(obj);
			if (objList.Count <= 0) return;
			if (isStarting) return;

			this.gameObject.SetActive(true);
			isStarting = true;
			if (IsDeviceIndicator())
			{
#if UNITY_IPHONE || UNITY_ANDROID && !UNITY_EDITOR
			Handheld.StartActivityIndicator();
#endif
			}
			else
			{
				InvokeRepeating("RotIcon", 0, animTime);
			}
		}

		/// <summary>
		/// インジケーターの表示終了
		/// 表示要求しているオブジェクトはあちこちから設定できる。
		/// 全ての要求が終了しない限りは表示を続ける
		/// </summary>
		/// <param name="obj">表示を要求していたオブジェクト</param>
		public void StopIndicator(System.Object obj)
		{
			DecRef(obj);
			if (objList.Count > 0) return;
			if (!isStarting) return;
			if (IsDeviceIndicator())
			{
#if UNITY_IPHONE || UNITY_ANDROID && !UNITY_EDITOR
	        Handheld.StopActivityIndicator();
#endif
			}
			else
			{
				CancelInvoke();
			}
			this.gameObject.SetActive(false);
			isStarting = false;
		}

		void RotIcon()
		{
			icon.transform.eulerAngles = new Vector3(0, 0, rotZ);
			rotZ += animRotZ;
		}

		void IncRef(System.Object obj)
		{
			if (!objList.Contains(obj))
			{
				objList.Add(obj);
			}
		}
		void DecRef(System.Object obj)
		{
			if (objList.Contains(obj))
			{
				objList.Remove(obj);
			}
		}

		bool IsDeviceIndicator()
		{
#if UNITY_IPHONE || UNITY_ANDROID && !UNITY_EDITOR
			return isDeviceIndicator;
#else
			isDeviceIndicator = false; 	// return false としないのはwaring対策
			return isDeviceIndicator;
#endif
		}
	}
}
