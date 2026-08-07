// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// GUIの管理
	/// </summary>
	[AddComponentMenu("Utage/ADV/AdvGuiManager")]
	public class AdvGuiManager : MonoBehaviour, IAdvSaveData
	{
		//管理対象のUI
		public List<GameObject> GuiObjects => guiObjects;
		[SerializeField] protected List<GameObject> guiObjects = new List<GameObject>();

		protected Dictionary<string, AdvGuiBase> objects = new Dictionary<string, AdvGuiBase>();
		public Dictionary<string, AdvGuiBase> Objects => objects;

		//
		protected virtual void Awake()
		{
			foreach( var item in guiObjects)
			{
				if (objects.ContainsKey(item.name))
				{
				}
				else
				{
					objects.Add(item.name, new AdvGuiBase(item));
				}
			}
		}

		internal virtual bool TryGet(string name, out AdvGuiBase gui)
		{
			return this.objects.TryGetValue(name, out gui);
		}

		//データのキー
		public virtual string SaveKey { get{return "GuiManager";} }

		//クリアする(初期状態に戻す)
		public virtual void OnClear()
		{
			foreach (var item in objects.Values)
			{
				item.Reset();
			}
		}

		const int Version = 0;
		//書き込み
		public virtual void OnWrite(System.IO.BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(objects.Count);
			foreach (var key in objects.Keys)
			{
				writer.Write(key);
				byte[] buffer = objects[key].ToBuffer();
				writer.WriteBuffer(buffer);
			}
		}

		//読み込み
		public virtual void OnRead(System.IO.BinaryReader reader)
		{
			//バージョンチェック
			int version = reader.ReadInt32();
			if (version == Version)
			{
				int count = reader.ReadInt32();
				for (int i = 0; i < count; ++i)
				{
					string key = reader.ReadString();
					int bufferLen = reader.ReadInt32();
					byte[] buffer = reader.ReadBytes(bufferLen);
					AdvGuiBase gui;
					if (this.objects.TryGetValue(key, out gui))
					{
						gui.ReadBuffer(buffer);
					}
					else
					{
						Debug.LogError(key + " is not found in GuiManager");
					}
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
	}
}
