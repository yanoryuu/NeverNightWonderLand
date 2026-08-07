// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	[System.Serializable]
	public class EventEffectColor : UnityEvent<AdvEffectColor> { }

	/// <summary>
	/// エフェクトによるカラー値
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/AdvEffectColor")]
	public class AdvEffectColor : MonoBehaviour
	{
		//AnimationClipで制御するカラー
		public Color AnimationColor
		{
			get { return animationColor; }
			set { animationColor = value; ChangedValue(); }
		}
		[SerializeField]
		Color animationColor = Color.white;

		//Tweenで制御するカラー
		public Color TweenColor
		{
			get { return tweenColor; }
			set { tweenColor = value; ChangedValue(); }
		}
		[SerializeField]
		Color tweenColor = Color.white;

		//Scriptから制御するカラー
		public Color ScriptColor
		{
			get { return scriptColor; }
			set { scriptColor = value; ChangedValue(); }
		}
		[SerializeField]
		Color scriptColor = Color.white;

		//カスタム操作で制御するカラー
		public Color CustomColor
		{
			get { return customColor; }
			set { customColor = value; ChangedValue(); }
		}
		[SerializeField]
		Color customColor = Color.white;

		//フェード処理で制御するカラー
		public float FadeAlpha
		{
			get { return fadeAlpha; }
			set { fadeAlpha = value; ChangedValue(); }
		}
		[SerializeField]
		float fadeAlpha = 1;

		public EventEffectColor OnValueChanged = new EventEffectColor();

		//全てのカラーを乗算したカラー値
		public Color MulColor
		{
			get
			{
				Color color = AnimationColor * TweenColor * ScriptColor * CustomColor;
				color.a = color.a*FadeAlpha;
				return color;
			}
		}

		Color lastColor = Color.white;

		void OnValidate()
		{
			ChangedValue();
		}

		void ChangedValue()
		{
			Color color = MulColor;
			OnValueChanged.Invoke(this);
			lastColor = color;
		}

		void Update()
		{
			if (lastColor != MulColor)
			{
				ChangedValue();
			}
		}

		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write (AnimationColor);
			writer.Write (TweenColor);
			writer.Write (ScriptColor);
			writer.Write (CustomColor);
			writer.Write(FadeAlpha);
		}

		//セーブデータ用のバイナリ読み込み
		public void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32 ();
			if (version < 0 || version > Version) {
				Debug.LogError (LanguageErrorMsg.LocalizeTextFormat (ErrorMsg.UnknownVersion, version));
				return;
			}

			animationColor = reader.ReadColor();
			tweenColor = reader.ReadColor();
			scriptColor = reader.ReadColor();
			customColor = reader.ReadColor();
			fadeAlpha = reader.ReadSingle ();

			//フェードのカラーは1に戻す
			fadeAlpha = 1;
			ChangedValue();
		}
	}
}
