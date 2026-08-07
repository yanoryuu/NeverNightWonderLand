// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace Utage
{
	//構文解析した際の文字データ（宴の基本仕様のほうでは、色などメタデータも含む）
	public class CharData : IParsedTextData
	{
		public enum HitEventType
		{
			Sound,
			Link,
			Tips,
		};

		//カスタム情報
		public class CustomCharaInfo
		{
			//カラーが指定されているか
			public bool IsColor{get;set;}
			//指定しているカラーの値
			public Color color;
			//カラータグの文字列（リッチテキストに正確に復元するときに使う）
			public string colorStr;

			//サイズが指定されているか
			public bool IsSize{get;set;}
			//指定しているサイズの値
			public int size;

			//Bold（太字）が設定されているか
			public bool IsBold{get;set;}
			//Italic（イタリック体）が設定されているか
			public bool IsItalic{get;set;}

			//本来二文字ぶんかどうか
			public bool IsDoubleWord{get;set;}

			//ルビの開始文字か
			public bool IsRubyTop{get;set;}
			//ルビが設定されているか
			public bool IsRuby{get;set;}
			public string rubyStr;

			//傍点が設定されているか
			public bool IsEmphasisMark{get;set;}

			//上付き文字が設定されているか
			public bool IsSuperScript{get;set;}

			//下付き文字が設定されているか
			public bool IsSubScript{get;set;}

			//上付き文字か下付き文字が設定されているか
			public bool IsSuperOrSubScript { get { return IsSuperScript || IsSubScript; } }

			//下線（アンダーライン）の開始文字か
			public bool IsUnderLineTop { get; set; }
			//下線（アンダーライン）が設定されているか
			public bool IsUnderLine{get;set;}

			//取り消し線の開始文字か
			public bool IsStrikeTop { get; set; }
			//取り消し線が設定されているか
			public bool IsStrike{get;set;}

			//グループ文字の開始文字か
			public bool IsGroupTop { get; set; }
			//グループ文字が設定されているか
			public bool IsGroup{get;set;}

			//絵文字が設定されているか
			public bool IsEmoji{get;set;}
			//絵文字のキー
			public string EmojiKey { get; set; }
			//絵文字タグのタイプ
			public string EmojiType { get; set; }

			//ダッシュ（ハイフン・横線）が設定されているか
			public bool IsDash{get;set;}
			//ダッシュのサイズ
			public int DashSize { get; set; }

			//サイズ指定のスペース
			public bool IsSpace { get; set; }
			//スペースのサイズ
			public int SpaceSize { get; set; }

			//スピードが指定されているか
			public bool IsSpeed{get;set;}
			//指定しているスピードの値
			public float speed;

			//インターバル（一文字単位の文字送り待ち時間）が指定されているか
			public bool IsInterval { get; set; }
			//指定しているインターバルの値
			public float Interval;

			//当たり判定の開始文字か
			public bool IsHitEventTop { get; set; }
			//当たり判定が設定されているか
			public bool IsHitEvent { get; set; }
			//当たり判定の引数
			public string HitEventArg { get; set; }
			//当たり判定のタイプ
			public HitEventType HitEventType { get; set; }

			public CustomCharaInfo Clone()
			{
				return (CustomCharaInfo)MemberwiseClone();
			}

			//Bold(太字)の解析
			public bool TryParseBold(string arg)
			{
				return IsBold = true;
			}
			//Bold(太字)のリセット
			public void ResetBold()
			{
				IsBold = false;
			}

			//Italic(イタリック体)の解析
			public bool TryParseItalic(string arg)
			{
				return IsItalic = true;
			}
			//Italic(イタリック体)のリセット
			public void ResetItalic()
			{
				IsItalic = false;
			}

			//カスタムサイズの解析
			public bool TryParseSize(string arg)
			{
				return IsSize = int.TryParse(arg, out size);
			}
			//カスタムサイズのリセット
			public void ResetSize()
			{
				IsSize = false;
				size = 0;
			}

			//カスタムカラーの解析
			public bool TryParseColor(string arg)
			{
				IsColor = ColorUtil.TryParseColor(arg, ref color);
				if (IsColor) colorStr = arg;
				return IsColor;
			}
			//カスタムカラーのリセット
			public void ResetColor()
			{
				IsColor = false;
				color = Color.white;
			}

			//ルビの解析
			public bool TryParseRuby(string arg)
			{
				if (string.IsNullOrEmpty(arg)) return false;

				IsRubyTop = IsRuby = true;
				rubyStr = arg;
				return true;
			}
			//ルビ設定のリセット
			public void ResetRuby()
			{
				IsRuby = false;
				rubyStr = "";
			}

			//傍点の解析
			public bool TryParseEmphasisMark (string arg)
			{
				if (string.IsNullOrEmpty(arg)) return false;
				rubyStr = arg;
				if (rubyStr.Length != 1) return false;
				IsRubyTop = IsRuby = IsEmphasisMark = true;
				return true;
			}
			//傍点設定のリセット
			public void ResetEmphasisMark()
			{
				IsRuby = IsEmphasisMark = false;
				rubyStr = "";
			}

			//上付き文字の解析
			public bool TryParseSuperScript(string arg)
			{
				IsSuperScript = true;
				return true;
			}

			//上付き文字のリセット
			public void ResetSuperScript()
			{
				IsSuperScript = false;
			}

			//下付き文字の解析
			public bool TryParseSubScript(string arg)
			{
				IsSubScript = true;
				return true;
			}

			//下付き文字のリセット
			public void ResetSubScript()
			{
				IsSubScript = false;
			}

			//下線の解析
			public bool TryParseUnderLine(string arg)
			{
				IsUnderLineTop = IsUnderLine = true;
				return true;
			}

			//下線のリセット
			public void ResetUnderLine()
			{
				IsUnderLine = false;
			}

			//取り消し線の解析
			public bool TryParseStrike(string arg)
			{
				IsStrikeTop = IsStrike = true;
				return true;
			}

			//取り消し線のリセット
			public void ResetStrike()
			{
				IsStrike = false;
			}

			//グループ文字の解析
			public bool TryParseGroup(string arg)
			{
				IsGroupTop = IsGroup = true;
				return true;
			}

			//グループ文字のリセット
			public void ResetGroup()
			{
				IsGroup = false;
			}

			//リンクの解析
			public bool TryParseLink(string arg)
			{
				IsHitEventTop = IsHitEvent = true;
				HitEventArg = arg;
				HitEventType = CharData.HitEventType.Link;
				return true;
			}

			//リンクのリセット
			public void ResetLink()
			{
				IsHitEvent = false;
			}


			//TIPSの解析
			public bool TryParseTips(string arg)
			{
				IsHitEventTop = IsHitEvent = true;
				HitEventArg = arg;
				HitEventType = CharData.HitEventType.Tips;
				return true;
			}

			//TIPSのリセット
			public void ResetTips()
			{
				IsHitEvent = false;
			}

			//サウンドの解析
			internal bool TryParseSound(string arg)
			{
				IsHitEventTop = IsHitEvent = true;
				HitEventArg = arg;
				HitEventType = CharData.HitEventType.Sound;
				return true;
			}

			//サウンドの解析
			internal void ResetSound()
			{
				IsHitEvent = false;
			}


			//スピードの解析
			internal bool TryParseSpeed(string arg)
			{
				return IsSpeed = WrapperUnityVersion.TryParseFloatGlobal(arg, out speed);
			}

			//スピードの解析
			internal void ResetSpeed()
			{
				IsSpeed = false;
				speed = 0;
			}

			//スピードの解析
			internal bool TryParseInterval(string arg)
			{
				return IsInterval = WrapperUnityVersion.TryParseFloatGlobal(arg, out Interval);
			}			

			//直前のデータと比べて、Boldのカスタム設定が終了するか
			public bool IsEndBold(CustomCharaInfo lastCustomInfo)
			{
				if (!lastCustomInfo.IsBold) return false;
				return (!IsBold);
			}
			//直前のデータと比べて、Boldのカスタム設定が開始するか
			public bool IsBeginBold(CustomCharaInfo lastCustomInfo)
			{
				if (!IsBold) return false;
				return (!lastCustomInfo.IsBold);
			}

			//直前のデータと比べて、Italicのカスタム設定が終了するか
			public bool IsEndItalic(CustomCharaInfo lastCustomInfo)
			{
				if (!lastCustomInfo.IsItalic) return false;
				return (!IsItalic);
			}
			//直前のデータと比べて、Italicのカスタム設定が開始するか
			public bool IsBeginItalic(CustomCharaInfo lastCustomInfo)
			{
				if (!IsItalic) return false;
				return (!lastCustomInfo.IsItalic);
			}
			
			//直前のデータと比べて、サイズのカスタム設定が終了するか
			public bool IsEndSize(CustomCharaInfo lastCustomInfo)
			{
				if(!lastCustomInfo.IsSize) return false;
				if(!IsSize) return true;
				return lastCustomInfo.size != size;
			}
			//直前のデータと比べて、サイズのカスタム設定が開始するか
			public bool IsBeginSize(CustomCharaInfo lastCustomInfo)
			{
				if (!IsSize) return false;
				if (!lastCustomInfo.IsSize) return true;
				return lastCustomInfo.size != size;
			}

			//直前のデータと比べて、カラーのカスタム設定が終了するか
			public bool IsEndColor(CustomCharaInfo lastCustomInfo)
			{
				if(!lastCustomInfo.IsColor) return false;
				if(!IsColor) return true;
				return lastCustomInfo.color != color;
			}
			//直前のデータと比べて、カラーのカスタム設定が開始するか
			public bool IsBeginColor(CustomCharaInfo lastCustomInfo)
			{
				if (!IsColor) return false;
				if (!lastCustomInfo.IsColor) return true;
				return lastCustomInfo.color != color;
			}


			//カスタム設定したサイズの取得
			public int GetCustomedSize(int defaultSize)
			{
				return IsSize ? size : defaultSize;
			}
			//カスタム設定したフォントスタイルの取得
			public FontStyle GetCustomedStyle(FontStyle defaultFontStyle)
			{
				if (IsItalic && IsBold)
				{
					return FontStyle.BoldAndItalic;
				}
				else if (IsItalic)
				{
					return FontStyle.Italic;
				}
				else if (IsBold)
				{
					return FontStyle.Bold;
				}
				else
				{
					return defaultFontStyle;
				}
			}

			//カスタム設定したカラーの取得
			public Color GetCustomedColor(Color defaultColor)
			{
				return IsColor ? color : defaultColor;
			}

			//次の文字になったときに必要なフラグクリア処理
			public void ClearOnNextChar()
			{
				IsRubyTop = false;
				IsUnderLineTop = false;
				IsStrikeTop = false;
				IsHitEventTop = false;
				IsGroupTop = false;
				rubyStr = "";
			}
		}


		public const char Dash = '—';

		/// <summary>
		/// 文字
		/// </summary>
		public char Char { get; private set; }

		/// <summary>
		/// Unityのリッチテキストのインデックス
		/// </summary>
		public int UnityRitchTextIndex { 
			get { return this.unityRitchTextIndex; }
			set { this.unityRitchTextIndex = value; }
		}
		int unityRitchTextIndex = -1;

		/// <summary>
		/// カスタム情報
		/// </summary>
		public CustomCharaInfo CustomInfo { get; }

		//サロゲートペア文字の場合、下位サロゲートペア文字
		public char CharSurrogatePair { get; private set; }
		//サロゲートペア文字かどうか
		public bool IsSurrogatePair { get; } = false;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="c">文字</param>
		public CharData(char c, CustomCharaInfo customInfo)
		{
			this.Char = c;
			this.CustomInfo = customInfo.Clone();
		}
		//コンストラクタ
		//サロゲートペア文字用
		public CharData(char c, char surrogatePair, CustomCharaInfo customInfo)
			: this(c, customInfo)
		{
			this.IsSurrogatePair = true;
			CharSurrogatePair = surrogatePair;
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="c">文字</param>
		public CharData(char c)
		{
			this.Char = c;
			this.CustomInfo = new CustomCharaInfo();
		}
		
		//文字を上書きする
		public void OverrideChar(char c)
		{
			Char = c;
		}

		//文字列作成用に文字を追加する
		public void AppendToStringBuilder(StringBuilder builder)
		{
			builder.Append(Char);
			if (IsSurrogatePair)
			{
				builder.Append(CharSurrogatePair);
			}
		}

		/// <summary>
		/// 改行コードか
		/// </summary>
		public bool IsBr { get { return (Char == '\n' || Char == '\r'); } }

		internal bool TryParseInterval(string arg)
		{
			return this.CustomInfo.TryParseInterval(arg);
		}
	};
}
