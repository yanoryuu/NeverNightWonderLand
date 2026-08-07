// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;
using System.IO;

namespace Utage
{

	/// <summary>
	/// メッセージウィンドウのデータ
	/// </summary>
	public class AdvMessageWindow
	{
		public string Name { get; protected set; }
		public TextData Text { get; protected set; }
		public string NameText { get; protected set; }
		public string CharacterLabel { get; protected set; }
		public int TextLength { get; protected set; }

		public IAdvMessageWindow MessageWindow { get; set; }
		public AdvMessageWindow(IAdvMessageWindow messageWindow)
		{
			this.MessageWindow = messageWindow;
			Name = this.MessageWindow.gameObject.name;
			Clear();
		}

		void Clear()
		{
			this.Text = new TextData("");
			this.NameText = "";
			this.CharacterLabel = "";
			this.TextLength = -1;
		}

		public virtual void ChangeActive(bool isActive)
		{
			if (!isActive) Clear();
			MessageWindow.OnChangeActive(isActive);
		}

		internal void Reset()
		{
			this.Clear();
			this.MessageWindow.OnReset();
		}

		internal void ChangeCurrent(bool isCurrent)
		{
			this.MessageWindow.OnChangeCurrent(isCurrent);
		}

		internal void PageTextChange(AdvPage page)
		{
			this.Text = page.TextData;
			this.NameText = page.NameText;
			this.CharacterLabel = page.CharacterLabel;
			this.TextLength = page.CurrentTextLength;
			this.MessageWindow.OnTextChanged(this);
		}

		internal void ReadPageData(BinaryReader reader)
		{
			this.Text = new TextData(reader.ReadString());
			this.NameText = reader.ReadString();
			this.CharacterLabel = reader.ReadString();
			this.TextLength = -1;
			this.MessageWindow.OnTextChanged(this);
		}

		internal void WritePageData(BinaryWriter writer)
		{
			writer.Write(this.Text.OriginalText);
			writer.Write(this.NameText);
			writer.Write(this.CharacterLabel);
		}
	}
}
