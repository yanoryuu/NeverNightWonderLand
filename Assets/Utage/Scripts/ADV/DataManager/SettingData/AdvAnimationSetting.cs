// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System;
using UtageExtensions;

namespace Utage
{
	public class AdvAnimationData : IAdvSettingData
	{
		public AnimationClip Clip { get; set; }
		enum TangentMode
		{
			Default,
			Linear,
		}
		TangentMode Tangent { get; set; }

		public AdvAnimationData(StringGrid grid, ref int index, bool legacy)
		{
			Clip = new AnimationClip();
			Clip.legacy = legacy;
			ParseHeader(grid.Rows[index++]);
			List<float> timeTbl = ParseTimeTbl(grid.Rows[index++]);
			if (!Clip.legacy)
			{
				AddDummyCurve(timeTbl);
			}

			while (index < grid.Rows.Count)
			{
				StringGridRow row = grid.Rows[index];
				try
				{
					if (row.IsEmptyOrCommantOut)
					{
						++index;
						continue;
					}

					if (IsHeader(row))
					{
						break;
					}
					PropertyType propertyType;
					if (!row.TryParseCellTypeOptional<PropertyType>(0, PropertyType.Custom, out propertyType))
					{
						string str = row.ParseCell<string>(0);
						//					Debug.LogError( row.ToErrorString("PropertyType Parse Error") );
						string typeName, propertyName;
						str.Separate('.', false, out typeName, out propertyName);
						Type type = System.Type.GetType(typeName);
						if (type != null)
						{
							Clip.SetCurve("", type, propertyName, ParseCurve(timeTbl, row));
						}
						else
						{
							str.Separate(',', true, out typeName, out propertyName);
							Type type1 = System.Type.GetType(typeName);
							if (type1 != null)
							{
								Clip.SetCurve("", type1, propertyName, ParseCurve(timeTbl, row));
							}
							else
							{
								Debug.LogError(typeName + "is not class name");
							}
						}
					}
					else
					{
						if (IsEvent(propertyType))
						{
							AddEvent(propertyType, timeTbl, row);
						}
						else
						{
							AddCurve(propertyType, ParseCurve(timeTbl, row));
						}
					}
					++index;
				}
				catch (System.Exception e)
				{
					Debug.LogError( row.ToErrorString( e.Message) );
				}
			}
		}

		bool IsHeader(StringGridRow row)
		{
			return (row.ParseCell<string>(0)[0] == '*');
		}

		void ParseHeader(StringGridRow row)
		{
			Clip.name = row.ParseCell<string>(0).Substring(1);
			Clip.wrapMode = row.ParseCellOptional<WrapMode>(1, WrapMode.Default);
			Tangent = row.ParseCellOptional<TangentMode>(2, TangentMode.Default); 
		}

		List<float> ParseTimeTbl(StringGridRow row)
		{
			List<float> timeTbl = new List<float>();
			for (int i = 1; i < row.Strings.Length; ++i)
			{
				float time;
				if (!row.TryParseCell<float>(i, out time))
				{
					string errorMsg = string.Format("TimeTbl parse error. The cell \" {0} \" in the {1} column cannot be parsed into numbers. is ",row.Strings[i],i);
					Debug.LogError(row.ToErrorString(errorMsg));
				}
				timeTbl.Add(time);
			}
			return timeTbl;
		}

		//アニメーションするのプロパティ
		enum PropertyType
		{
			Custom,
			X,
			Y,
			Z,
			Scale,      //スケールXYZすべて
			ScaleX,
			ScaleY,
			ScaleZ,
			Angle,      //AngleZに同じ（2Dメインなので）
			AngleX,
			AngleY,
			AngleZ,
			Alpha,
			R,
			G,
			B,
			Texture,
			Pattern,
		};

		bool IsEvent(PropertyType type)
		{
			switch (type)
			{
				case PropertyType.Texture:
					return true;
				case PropertyType.Pattern:
					return true;
				default:
					return false;
			}
		}

		bool IsCustomProperty(PropertyType type)
		{
			switch (type)
			{
				case PropertyType.Custom:
					return true;
				default:
					return false;
			}
		}

		void AddEvent(PropertyType propertyType, List<float> timeTbl, StringGridRow row)
		{
			for (int i = 0; i < row.Strings.Length; ++i)
			{
				if (i == 0) continue;
				if (row.IsEmptyCell(i)) continue;

				//キーの追加
				AnimationEvent e = new AnimationEvent();
				// AnimationCurveの生成.
				switch (propertyType)
				{
					case PropertyType.Texture:
					{
						string value;
						if (!row.TryParseCell<string>(i, out value)) continue;
						e.functionName = "ChangePattern";
						e.stringParameter = value;
						e.time = timeTbl[i - 1];
						break;
					}
					case PropertyType.Pattern:
					{
						string value;
						if (!row.TryParseCell<string>(i, out value)) continue;
						e.functionName = "ChangePatternAnimation";
						e.stringParameter = value;
						e.time = timeTbl[i - 1];
						break;
					}
				}
				if (Application.isPlaying)
				{
					Clip.AddEvent(e);
				}
				else
				{
#if UNITY_EDITOR
//					UnityEditor.AnimationUtility. Clip.AddEvent(e);
#endif
				}
			}
		}

		AnimationCurve ParseCurve(List<float> timeTbl, StringGridRow row)
		{
			// AnimationCurveの生成.
			AnimationCurve curve = new AnimationCurve();
			for (int i = 0; i < row.Strings.Length; ++i)
			{
				if (i == 0) continue;
				if (row.IsEmptyCell(i)) continue;

				float value;
				if (!row.TryParseCell<float>(i, out value)) continue;
				//キーの追加
				var key = new Keyframe(timeTbl[i - 1], value);
				curve.AddKey(key);
			}
			if (curve.keys.Length <= 1)
			{
//				Debug.LogError(row.ToErrorString("Need more than 2 key data"));
			}

			switch (Tangent)
			{
				case TangentMode.Linear:
					SetCurveLinear(curve);
					break;
				default:
					break;
			}
			return curve;
		}
		
		//https://forum.unity.com/threads/how-to-set-an-animation-curve-to-linear-through-scripting.151683/
		public static void SetCurveLinear(AnimationCurve curve) 
		{
			for (int i = 0; i < curve.keys.Length; ++i) 
			{
				float inTangent = 0;
				float outTangent = 0;
				bool inTangentSet = false;
				bool outTangentSet = false;
				Vector2 point1;
				Vector2 point2;
				Vector2 deltaPoint;
				Keyframe key = curve[i];
 
				if (i == 0) {
					inTangent = 0; inTangentSet = true;
				}
 
				if (i == curve.keys.Length - 1) {
					outTangent = 0; outTangentSet = true;
				}
 
				if (!inTangentSet) {
					point1.x = curve.keys[i - 1].time;
					point1.y = curve.keys[i - 1].value;
					point2.x = curve.keys[i].time;
					point2.y = curve.keys[i].value;
 
					deltaPoint = point2 - point1;
 
					inTangent = deltaPoint.y / deltaPoint.x;
				}
				if (!outTangentSet) {
					point1.x = curve.keys[i].time;
					point1.y = curve.keys[i].value;
					point2.x = curve.keys[i + 1].time;
					point2.y = curve.keys[i + 1].value;
 
					deltaPoint = point2 - point1;
 
					outTangent = deltaPoint.y / deltaPoint.x;
				}
 
				key.inTangent = inTangent;
				key.outTangent = outTangent;
				curve.MoveKey(i, key);
			}
		}

		//Animatorの場合、最後のフレームまでカーブデータがないと途中で終わってしまうのでダミーで乗せる
		void AddDummyCurve(List<float> timeTbl)
		{
			AnimationCurve dummyCurve = AnimationCurve.Linear(timeTbl[0], 0, timeTbl[timeTbl.Count - 1], 1);
			Clip.SetCurve("", typeof(UnityEngine.Object), "", dummyCurve);
		}

		void AddCurve(PropertyType type, AnimationCurve curve)
		{
			if (curve.keys.Length <= 0) return;
			switch (type)
			{
				case PropertyType.X:
					Clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
					break;
				case PropertyType.Y:
					Clip.SetCurve("", typeof(Transform), "localPosition.y", curve);
					break;
				case PropertyType.Z:
					Clip.SetCurve("", typeof(Transform), "localPosition.z", curve);
					break;
				case PropertyType.ScaleX:
					Clip.SetCurve("", typeof(Transform), "localScale.x", curve);
					break;
				case PropertyType.ScaleY:
					Clip.SetCurve("", typeof(Transform), "localScale.y", curve);
					break;
				case PropertyType.ScaleZ:
					Clip.SetCurve("", typeof(Transform), "localScale.z", curve);
					break;
				case PropertyType.Scale:
					Clip.SetCurve("", typeof(Transform), "localScale.x", curve);
					Clip.SetCurve("", typeof(Transform), "localScale.y", curve);
					Clip.SetCurve("", typeof(Transform), "localScale.z", curve);
					break;
				case PropertyType.AngleX:
					Clip.SetCurve("", typeof(Transform), "localEulerAngles.x", curve);
					break;
				case PropertyType.AngleY:
					Clip.SetCurve("", typeof(Transform), "localEulerAngles.y", curve);
					break;
				case PropertyType.Angle:
				case PropertyType.AngleZ:
					Clip.SetCurve("", typeof(Transform), "localEulerAngles.z", curve);
					break;
				case PropertyType.Alpha:
					Clip.SetCurve("", typeof(AdvEffectColor), "animationColor.a", curve);
					break;
				case PropertyType.R:
					Clip.SetCurve("", typeof(AdvEffectColor), "animationColor.r", curve);
					break;
				case PropertyType.G:
					Clip.SetCurve("", typeof(AdvEffectColor), "animationColor.g", curve);
					break;
				case PropertyType.B:
					Clip.SetCurve("", typeof(AdvEffectColor), "animationColor.b", curve);
					break;
				default:
					Debug.LogError("UnknownType");
					break;					
			}
		}
	};

	/// <summary>
	/// キーフレームアニメーションの設定
	/// </summary>
	public class AdvAnimationSetting : AdvSettingBase
	{
		List<AdvAnimationData> DataList = new List<AdvAnimationData>();
		protected override void OnParseGrid(StringGrid grid)
		{
			int index = 0;
			while (index < grid.Rows.Count)
			{
				if (grid.Rows[index].IsEmpty)
				{
					index++;
					continue;
				}

				AdvAnimationData data = new AdvAnimationData(grid, ref index, true);
				DataList.Add(data);
			}
		}

		public AdvAnimationData Find(string name)
		{
			return DataList.Find(x => x.Clip.name == name);
		}
	}
}
