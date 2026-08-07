// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using System.Collections;


namespace Utage
{

	/// <summary>
	/// 他のオブジェクトのトランスフォームが変化したらそれに合わせて変化する
	/// 同じアニメーションをさせたい場合に
	/// </summary>
	[AddComponentMenu("Utage/Lib/Effect/LinkTransform")]
	public class LinkTransform : MonoBehaviour
	{
		public Transform target;

		bool isInit;
		Vector3 targetPosition;
		Vector3 targetScale;
		Vector3 targetEuler;

		Vector3 startPosition;
		Vector3 startScale;
		Vector3 startEuler;

		/// <summary>
		/// トランスフォームのキャッシュ
		/// </summary>
		Transform cachedTransform;
		Transform CachedTransform { get { if (null == cachedTransform) cachedTransform = this.transform; return cachedTransform; } }

		void Start()
		{
			StartCoroutine( CoUpdate() );
		}

		void Init()
		{
			targetPosition = target.position;
			targetScale = target.localScale;
			targetEuler = target.eulerAngles;

			startPosition = CachedTransform.position;
			startScale = CachedTransform.localScale;
			startEuler = CachedTransform.eulerAngles;

			isInit = true;
		}

		IEnumerator CoUpdate()
		{
			while(true)
			{
				if (target.gameObject.activeSelf)
				{
					if (!isInit && target.gameObject.activeSelf)
					{
						RectTransform rectTransform = target as RectTransform;
						//RectTransformの場合は1フレーム遅らせる
						if (rectTransform!=null)
						{
							yield return null;
						}
						Init();
					}

					if (target.transform.hasChanged)
					{
						CachedTransform.position = startPosition + (target.position - targetPosition);
						CachedTransform.localScale = startScale + (target.localScale - targetScale);
						CachedTransform.eulerAngles = startEuler + (target.eulerAngles - targetEuler);
					}
				}
				yield return null;
			}
		}
	}
}