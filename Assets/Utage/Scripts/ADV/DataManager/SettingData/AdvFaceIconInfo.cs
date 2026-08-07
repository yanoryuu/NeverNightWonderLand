// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	// 顔アイコン情報
	public class AdvFaceIconInfo
	{
		public enum Type
		{
			None,			//アイコンを使用しない
			IconImage,		//アイコン専用の画像ファイルを使う
			DicingPattern,  //メイン画像と同じダイシングパックのパターン画像を使う
			RectImage,		//立ち絵の一部を切り出して使う
		}
		//アイコンファイルのパスの情報
		public Type IconType { get; internal set; }
		//アイコンファイルのパスの情報
		public string FileName { get; internal set; }
		public AssetFile File { get; set; }
		//アイコンの切り抜き矩形の情報
		public Rect IconRect { get; internal set; }
		//アイコンのサブファイル名
		public string IconSubFileName { get; internal set; }
		//レイヤー設定に合わせて反転するかの情報
		public bool AutoFlip { get; internal set; }

		public AdvFaceIconInfo(StringGridRow row)
		{
			this.FileName = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Icon,"");
			this.AutoFlip = AdvParser.ParseCellOptional<bool>(row, AdvColumnName.IconAutoFlip, false);
			if (!string.IsNullOrEmpty(FileName))
			{
				if (!AdvParser.IsEmptyCell(row, AdvColumnName.IconSubFileName))
				{
					this.IconType = AdvFaceIconInfo.Type.DicingPattern;
					this.IconSubFileName = AdvParser.ParseCell<string>(row, AdvColumnName.IconSubFileName);
				}
				else
				{
					this.IconType = AdvFaceIconInfo.Type.IconImage;
				}
			}
			else if (!AdvParser.IsEmptyCell(row, AdvColumnName.IconRect))
			{
				float[] rect = row.ParseCellArray<float>(AdvColumnName.IconRect.QuickToString());
				if (rect.Length == 4)
				{
					this.IconType = AdvFaceIconInfo.Type.RectImage;
					this.IconRect = new Rect(rect[0], rect[1], rect[2], rect[3]);
				}
				else
				{
					Debug.LogError(row.ToErrorString("IconRect. Array size is not 4"));
				}
			}
			else
			{
				this.IconType = Type.None;
			}
		}
		public void BootInit(System.Func<string, string> fileNameToPath)
		{
			if (!string.IsNullOrEmpty(this.FileName))
			{
				File = AssetFileManager.GetFileCreateIfMissing(fileNameToPath(FileName));
			}
		}
	}
}
