// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.IO;

namespace Utage
{

	/// <summary>
	/// 選択肢のデータ
	/// </summary>
	public class AdvSelection
	{
		/// <summary>
		/// 選択肢ラベル（選択済みのチェックに）
		/// </summary>
		public string Label { get; private set; }
		
		/// <summary>
		/// 表示テキスト
		/// </summary>
		public string Text { get { return this.text; } }
		string text;

		/// <summary>
		/// ジャンプ先のラベル
		/// </summary>
		public string JumpLabel { get { return this.jumpLabel; } }
		string jumpLabel;

		/// <summary>
		/// 選択時に実行する演算式
		/// </summary>
		public ExpressionParser Exp { get { return this.exp; } }
		ExpressionParser exp;

		//使用するプレハブ名
		public string PrefabName { get; protected set; }

		//表示座標X
		public float? X { get; protected set; }

		//表示座標Y
		public float? Y { get; protected set; }
		
		//クリックに反応するスプライト名
		public string SpriteName { get { return this.spriteName; } }
		string spriteName = "";

		//ポリゴンコライダーを使うか
		public bool IsPolygon { get { return this.isPolygon; } }
		bool isPolygon;

		//設定されているデータ
		public StringGridRow RowData { get { return this.row; } }
		StringGridRow row;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="jumpLabel">飛び先のラベル</param>
		/// <param name="text">表示テキスト</param>
		/// <param name="exp">選択時に実行する演算式</param>
		public AdvSelection(string jumpLabel, string text, ExpressionParser exp, string prefabName, float? x, float? y, StringGridRow row)
		{
			this.Label = "";
			this.jumpLabel = jumpLabel;
			this.text = text;
			this.exp = exp;
			this.PrefabName = prefabName;
			this.X = x;
			this.Y = y;
			this.row = row;
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="label">ジャンプ先のラベル</param>
		/// <param name="spriteName">クリックを有効にするオブジェクト名</param>
		/// <param name="isPolygon">ポリゴンコライダーを使うか</param>
		/// <param name="exp">選択時に実行する演算式</param>
		public AdvSelection(string jumpLabel, string spriteName, bool isPolygon, ExpressionParser exp, StringGridRow row)
		{
			this.Label = "";
			this.jumpLabel = jumpLabel;
			this.text = "";
			this.exp = exp;
			this.spriteName = spriteName;
			this.isPolygon = isPolygon;
			this.row = row;
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public AdvSelection(BinaryReader reader, AdvEngine engine)
		{
			Read(reader,engine);
		}

		const int VERSION = 2;
		const int VERSION_1 = 1;
		const int VERSION_0 = 0;
		//バイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(VERSION);
			writer.Write(this.jumpLabel);
			writer.Write(this.text );
			if (this.Exp != null)
			{
				writer.Write(this.Exp.Exp );
			}
			else
			{
				writer.Write("");
			}
			
			writer.Write(this.spriteName);
			writer.Write(this.isPolygon );
		}
		//バイナリ読み込み
		void Read(BinaryReader reader, AdvEngine engine)
		{
			int version = reader.ReadInt32();
			if (version == VERSION)
			{
				this.jumpLabel = reader.ReadString();
				this.text = reader.ReadString();
				string expStr = reader.ReadString();
				if (!string.IsNullOrEmpty(expStr))
				{
					this.exp = engine.DataManager.SettingDataManager.DefaultParam.CreateExpression(expStr);
				}
				else
				{
					this.exp = null;
				}
				this.spriteName = reader.ReadString();
				this.isPolygon = reader.ReadBoolean();
			}
			else if (version == VERSION_1)
			{
				this.jumpLabel = reader.ReadString ();
				this.text = reader.ReadString ();
				string expStr = reader.ReadString ();
				if(!string.IsNullOrEmpty(expStr))
				{
					this.exp = engine.DataManager.SettingDataManager.DefaultParam.CreateExpression(expStr);
				}
				else
				{
					this.exp = null;
				}
			}
			else if (version == VERSION_0)
			{
				this.jumpLabel = reader.ReadString ();
				this.text = reader.ReadString ();
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

	}
}