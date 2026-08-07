// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// 宴が操作するGUIオブジェクトの基本クラス
	/// </summary>
	public class AdvGuiBase
	{
		public string Name { get { return Target.name; } }

		//ターゲットとなるオブジェクト
		public GameObject Target { get; private set; }

		//RectTransform
		public RectTransform RectTransform { get { if (rectTransform == null) rectTransform = Target.transform as RectTransform; return rectTransform; } }
		RectTransform rectTransform;

		//キャンバス
		public Canvas Canvas { get { return Target.GetComponentCacheInParent(ref canvas); } }
		Canvas canvas;

		//キャンバスのRectTransform
		public RectTransform CanvasRectTransform { get { if (canvasRectTransform == null) canvasRectTransform = Canvas.transform as RectTransform; return canvasRectTransform; } }
		RectTransform canvasRectTransform;

		//変更があったかどうか
		public bool HasChanged { get; private set; }

		protected byte[] defaultData;

		public AdvGuiBase(GameObject target)
		{
			this.Target = target;
			HasChanged = true;
			this.defaultData = ToBuffer();
			HasChanged = false;
		}

		//バイナリデータに
		public virtual byte[] ToBuffer()
		{
			return BinaryUtil.BinaryWrite(Write);			
		}

		//バイナリデータを読みこみ
		public virtual void ReadBuffer(byte[] buffer)
		{
			BinaryUtil.BinaryRead(buffer, Read);
		}

		const int Version = 0;
		//バイナリ書き込み
		protected virtual void Write(System.IO.BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(HasChanged);
			if (HasChanged)
			{
				WriteChanged(writer);
			}
		}
		//変化があった場合のバイナリ書き込み
		protected virtual void WriteChanged(System.IO.BinaryWriter writer)
		{
			writer.Write(Target.activeSelf);
			writer.WriteRectTransfom(RectTransform);
		}

		//バイナリ読みこみ
		protected virtual void Read(System.IO.BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version >= Version)
			{
				bool changed = reader.ReadBoolean();
				if (changed)
				{
					HasChanged = changed;
					ReadChanged(reader);
				}
				else
				{
					Reset();
				}
			}
			else
			{
				throw new System.Exception(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
		//変化があった場合のバイナリ読みこみ
		protected virtual void ReadChanged(System.IO.BinaryReader reader)
		{
			Target.SetActive(reader.ReadBoolean());
			reader.ReadRectTransfom(RectTransform);
		}

		//初期状態に戻す
		internal virtual void Reset()
		{
			if (HasChanged)
			{
				ReadBuffer(defaultData);
				HasChanged = false;
			}
		}

		//アクティブ状態の切り替え
		public virtual void SetActive(bool isActive)
		{
			HasChanged = true;
			this.Target.SetActive(isActive);
		}

		//位置の変更
		public virtual void SetPosition(float? x, float? y)
		{
			HasChanged = true;
			//キャンバス内の座標
			Vector3 position = CanvasRectTransform.InverseTransformPoint(RectTransform.position);
			if (x.HasValue) position.x = x.Value;
			if (y.HasValue) position.y = y.Value;
			position = CanvasRectTransform.TransformPoint(position);
			RectTransform.position = position;
		}

		//サイズ変更
		internal virtual void SetSize(float? x, float? y)
		{
			HasChanged = true;
			if (x.HasValue) RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, x.Value);
			if (y.HasValue) RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y.Value);
		}
	}
}
