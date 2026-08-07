// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// レイヤー設定のデータ
	/// </summary>	
	public class AdvLayerSettingData : AdvSettingDictinoayItemBase
	{
		/// <summary>
		/// レイヤー名
		/// </summary>
		public string Name { get { return this.Key; } }

		/// <summary>
		/// レイヤーのタイプ
		/// </summary>
		public enum LayerType
		{
			/// <summary>背景</summary>
			Bg,
			/// <summary>キャラクター</summary>
			Character,
			/// <summary>その他スプライト</summary>
			Sprite,
			/// <summary>背景</summary>
			Dummy,
			/// <summary>タイプ数</summary>
			Max,
		};
		/// <summary>
		/// レイヤーのタイプ
		/// </summary>
		public LayerType Type { get; private set; }

		//ボーダー設定
		internal enum BorderType
		{
			None,       //設定なし
			Streach,    //大きさに合わせて広げる
			BorderMin,  //小さいほうの値（左や下）だけに合わせる
			BorderMax,  //大きいほうの値（左や下）だけに合わせる
		};

		//ボーダー設定つきの矩形情報
		internal class RectSetting
		{
			public BorderType type;
			public float position;
			public float size;
			public float borderMin;
			public float borderMax;

			internal void GetBorderdPositionAndSize(float defaultSize, out float position, out float size)
			{
				switch (type)
				{
					case BorderType.BorderMin:
						size = (this.size == 0) ? defaultSize : this.size;
						position = (-defaultSize / 2 + borderMin + size / 2);
						break;
					case BorderType.BorderMax:
						size = (this.size == 0) ? defaultSize : this.size;
						position = (defaultSize / 2 - borderMax - size / 2);
						break;
					case BorderType.Streach:
						size = defaultSize;
						size -= (borderMin + borderMax);
						position = (borderMin - borderMax);
						break;
					case BorderType.None:
					default:
						size = (this.size == 0) ? defaultSize : this.size;
						position = this.position;
						break;
				}
			}
		}

		/// <summary>
		/// ボーダー設定を考慮した位置やサイズ
		/// </summary>
		internal RectSetting Horizontal { get; private set; }
		internal RectSetting Vertical { get; private set; }


		/// <summary>
		/// Z座標
		/// </summary>
		public float Z { get; private set; }

		/// <summary>
		/// スケール値
		/// </summary>
		public Vector3 Scale { get; private set; }

		/// <summary>
		/// ピボット
		/// </summary>
		public Vector2 Pivot { get; private set; }

		/// <summary>
		/// 描画順
		/// </summary>
		public int Order { get; private set; }

		/// <summary>
		/// レイヤーマスク（Unityのレイヤー名）
		/// </summary>
		public string LayerMask { get; private set; }

		/// <summary>
		/// アラインメント
		/// </summary>
		public Alignment Alignment { get; private set; }

		/// <summary>
		/// 左右反転
		/// </summary>
		public bool FlipX { get; private set; }

		/// <summary>
		/// 上下反転
		/// </summary>
		public bool FlipY { get; private set; }


		/// <summary>
		/// デフォルトデータ
		/// </summary>
		public bool IsDefault { get { return this.isDefault; } set { this.isDefault = value; } }
		bool isDefault;

		/// <summary>
		/// StringGridの一行からデータ初期化
		/// </summary>
		/// <param name="row">初期化するためのデータ</param>
		/// <returns>成否</returns>
		public override bool InitFromStringGridRow(StringGridRow row)
		{
			RowData = row;
			string key = AdvParser.ParseCell<string>(row, AdvColumnName.LayerName);
			if (string.IsNullOrEmpty(key))
			{
				return false;
			}
			else
			{
				InitKey(key);
				this.Type = AdvParser.ParseCell<LayerType>(row, AdvColumnName.Type);
				if (this.Type == LayerType.Dummy)
				{
					return true;
				}
				this.Order = AdvParser.ParseCell<int>(row, AdvColumnName.Order);
				this.LayerMask = AdvParser.ParseCellOptional<string>(row, AdvColumnName.LayerMask, "");

				//X座標や幅の設定
				this.Horizontal = new RectSetting();
				bool isBorderLeft = !AdvParser.IsEmptyCell(row, AdvColumnName.BorderLeft);
				bool isBorderRight = !AdvParser.IsEmptyCell(row, AdvColumnName.BorderRight);
				if (isBorderLeft)
				{
					this.Horizontal.type = (isBorderRight) ? BorderType.Streach : BorderType.BorderMin;
				}
				else
				{
					this.Horizontal.type = (isBorderRight) ? BorderType.BorderMax : BorderType.None;
				}
				Horizontal.position = AdvParser.ParseCellOptional<float>(row, AdvColumnName.X, 0);
				Horizontal.size = AdvParser.ParseCellOptional<float>(row, AdvColumnName.Width, 0);
				Horizontal.borderMin = AdvParser.ParseCellOptional<float>(row, AdvColumnName.BorderLeft, 0);
				Horizontal.borderMax = AdvParser.ParseCellOptional<float>(row, AdvColumnName.BorderRight, 0);


				//Y座標や高さの設定
				this.Vertical = new RectSetting();
				bool isBorderTop = !AdvParser.IsEmptyCell(row, AdvColumnName.BorderTop);
				bool isBorderBottom = !AdvParser.IsEmptyCell(row, AdvColumnName.BorderBottom);
				if (isBorderTop)
				{
					this.Vertical.type = (isBorderBottom) ? BorderType.Streach : BorderType.BorderMax;
				}
				else
				{
					this.Vertical.type = (isBorderBottom) ? BorderType.BorderMin : BorderType.None;
				}
				Vertical.position = AdvParser.ParseCellOptional<float>(row, AdvColumnName.Y, 0);
				Vertical.size = AdvParser.ParseCellOptional<float>(row, AdvColumnName.Height, 0);
				Vertical.borderMin = AdvParser.ParseCellOptional<float>(row, AdvColumnName.BorderBottom, 0);
				Vertical.borderMax = AdvParser.ParseCellOptional<float>(row, AdvColumnName.BorderTop, 0);


				Vector2 pivot;
				pivot.x = AdvParser.ParseCellOptional<float>(row, AdvColumnName.PivotX, 0.5f);
				pivot.y = AdvParser.ParseCellOptional<float>(row, AdvColumnName.PivotY, 0.5f);
				this.Pivot = pivot;

				Vector3 scale;
				scale.x = AdvParser.ParseCellOptional<float>(row, AdvColumnName.ScaleX, 1.0f);
				scale.y = AdvParser.ParseCellOptional<float>(row, AdvColumnName.ScaleY, 1.0f);
				scale.z = AdvParser.ParseCellOptional<float>(row, AdvColumnName.ScaleZ, 1.0f);
				this.Scale = scale;

				this.Z = AdvParser.ParseCellOptional<float>(row, AdvColumnName.Z, -0.01f * Order);
				this.Alignment = AdvParser.ParseCellOptional<Alignment>(row, AdvColumnName.Align, Alignment.None);
				this.FlipX = AdvParser.ParseCellOptional<bool>(row, AdvColumnName.FlipX, false);
				this.FlipY = AdvParser.ParseCellOptional<bool>(row, AdvColumnName.FlipY, false);
				return true;
			}
		}

		/// <summary>
		/// デフォルトレイヤー用の初期化
		/// </summary>
		/// <param name="name">名前</param>
		/// <param name="type">タイプ</param>
		/// <param name="order">描画順</param>
		public void InitDefault(string name, LayerType type, int order)
		{
			InitKey(name);
			this.Type = type;
			this.Horizontal = new RectSetting();
			this.Vertical = new RectSetting();
			this.Pivot = Vector2.one * 0.5f;
			this.Order = order;
			this.Scale = Vector3.one;
			this.Z = -0.01f * order;
			this.LayerMask = "";
			this.Alignment = Alignment.None;
			this.FlipX = false;
			this.FlipY = false;
		}

		public void InitFromCanvas(Canvas canvas, LayerType type, Alignment alignment, bool flipX, bool flipY)
		{
			RectTransform rectTransform = canvas.transform as RectTransform;
			InitKey(canvas.gameObject.name);
			this.Type = type;


			this.Horizontal = new RectSetting();	
			this.Vertical = new RectSetting();
			this.Pivot = rectTransform.pivot;
			this.Order = canvas.sortingOrder;
			this.LayerMask = UnityEngine.LayerMask.LayerToName( canvas.gameObject.layer);
			this.Scale = rectTransform.localScale;
			this.Z = rectTransform.localPosition.x;
			this.Alignment = alignment;
			this.FlipX = flipX;
			this.FlipY = flipY;
		}
	}

	/// <summary>
	/// レイヤー設定
	/// </summary>
	public class AdvLayerSetting : AdvSettingDataDictinoayBase<AdvLayerSettingData>
	{
		public override void ParseGrid(StringGrid grid)
		{
			base.ParseGrid(grid);
			InitDefaultLayers();
		}

		//各レイヤータイプのデフォルトレイヤーを補完する
		//レイヤーが1つもないタイプには「Bg Default」などのデフォルト設定を追加する
		//Layerシートがプロジェクトに1枚も存在しない場合はParseGridが呼ばれないため、
		//AdvSettingDataManagerのBootInitからも呼ぶこと（冪等なので複数回呼んでも問題ない）
		public void InitDefaultLayers()
		{
			InitDefault(AdvLayerSettingData.LayerType.Bg, 0);
			InitDefault(AdvLayerSettingData.LayerType.Character, 100);
			InitDefault(AdvLayerSettingData.LayerType.Sprite, 200);
		}

		void InitDefault(AdvLayerSettingData.LayerType type, int defaultOrder)
		{
			AdvLayerSettingData defaultLayer = List.Find((item) => item.Type == type);
			if (defaultLayer == null)
			{
				defaultLayer = new AdvLayerSettingData();
				defaultLayer.InitDefault(type.ToString() + " Default", type, defaultOrder);
				AddData(defaultLayer);
			}
			defaultLayer.IsDefault = true;
		}

		public bool Contains(string layerName, AdvLayerSettingData.LayerType type)
		{
			AdvLayerSettingData data;
			if (Dictionary.TryGetValue(layerName, out data))
			{
				return data.Type == type || data.Type == AdvLayerSettingData.LayerType.Dummy;
			}
			return false;
		}

		public bool Contains(string layerName)
		{
			return Dictionary.ContainsKey(layerName);
		}

		public AdvLayerSettingData FindDefaultLayer(AdvLayerSettingData.LayerType type)
		{
			return List.Find((item) => (item.Type == type) && item.IsDefault);
		}
	}
}