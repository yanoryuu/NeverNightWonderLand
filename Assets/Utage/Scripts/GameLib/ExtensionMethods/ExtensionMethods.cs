// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Utage;

namespace UtageExtensions
{
	//拡張メソッド
	public static class UtageExtensions
	{

		//********GameObjectの拡張メソッド********//

		/// <summary>
		/// SendMessageする
		/// ただし、funcがnullだった場合何もしない
		/// </summary>
		/// <param name="functionName">送信するメッセージ</param>
		/// <param name="isForceActive">送り先のGameObjectを強制的にactiveにしてからSendMessageするか</param>
		public static void SafeSendMessage(this GameObject go, string functionName, System.Object obj = null, bool isForceActive = false)
		{
			if (!string.IsNullOrEmpty(functionName))
			{
				if (isForceActive) go.SetActive(true);
				go.SendMessage(functionName, obj, SendMessageOptions.DontRequireReceiver);
			}
		}


		/// <summary>
		/// 子を含む全てのレイヤーを変更する
		/// </summary>
		/// <param name="trans">レイヤーを変更する対象</param>
		/// <param name="layer">設定するレイヤー</param>
		public static void ChangeLayerDeep(this GameObject go, int layer)
		{
			go.layer = layer;
			foreach (Transform child in go.transform)
			{
				child.gameObject.ChangeLayerDeep(layer);
			}
		}


		//********シーン内のHierarchyの拡張メソッド********//
		public static string GetHierarchyPath(this Transform t)
		{
			string path = t.name;
			var parent = t.parent;
			while (parent)
			{
				path = $"{parent.name}/{path}";
				parent = parent.parent;
			}

			return path;
		}
		public static string GetHierarchyPath(this GameObject go)
		{
			return go.transform.GetHierarchyPath();
		}

		//********Graphicの拡張メソッド********//

		//α値の設定
		public static void SetAlpha(this Graphic graphic, float alpha)
		{
			Color c = graphic.color;
			c.a = alpha;
			graphic.color = c;
		}

		//α値の取得
		public static float GetAlpha(this Graphic graphic)
		{
			return graphic.color.a;
		}

		//********Rectの拡張メソッド********//

		//テクスチャの幅と高さと、切り抜き矩形からUV座標を取得
		internal static Rect ToUvRect(this Rect rect, float w, float h)
		{
			return new Rect(rect.x / w, 1.0f - (rect.yMax) / h, rect.width / w, rect.height / h);
		}

		//********RenderTextureの拡張メソッド********//

		//元のテクスチャをコピーした一時的なRenderTextureを作成する
		public static RenderTexture CreateCopyTemporary(this RenderTexture renderTexture)
		{
			return renderTexture.CreateCopyTemporary(renderTexture.depth);
		}
		public static RenderTexture CreateCopyTemporary(this RenderTexture renderTexture, int depth)
		{
			RenderTexture copy = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height, depth, renderTexture.format);
			Graphics.Blit(renderTexture, copy);
			return copy;
		}


		//******** Dictionaryの拡張メソッド********//

		// 値を取得、keyがなければデフォルト値を設定し、デフォルト値を取得
		public static TValue GetValueOrSetDefaultIfMissing<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
		{
			TValue value;
			if (!dictionary.TryGetValue(key, out value))
			{
				dictionary.Add(key, defaultValue);
				return defaultValue;
			}
			else
			{
				return value;
			}
		}

		/// 値を取得、keyがなければデフォルト値を設定し、デフォルト値を取得
		public static TValue GetValueOrGetNullIfMissing<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : class
		{
			TValue value;
			if (!dictionary.TryGetValue(key, out value))
			{
				return null;
			}
			else
			{
				return value;
			}
		}

		//******** Vector2の拡張メソッド********//

		public static bool Approximately(this Vector2 a, Vector2 b)
		{
			return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
		}


		//******** TextMeshProの拡張メソッド********//
	
		//TMP_TextInfoが未完成なことを想定して強制アップデートして取得
		//重いので頻繁には使わないこと
		public static TMP_TextInfo ForceGetTextInfo(this TMP_Text tmpText)
		{
			return tmpText.GetTextInfo(tmpText.text);
		}



		//レイヤーマスクからレイヤー番号を取得する
		public static IEnumerable<int> GetLayerNumbers(this LayerMask layerMask)
		{
			for (int i = 0; i < 32; i++)
			{
				if ((layerMask & (1 << i)) != 0)
					yield return i;
			}
		}
	}
}
