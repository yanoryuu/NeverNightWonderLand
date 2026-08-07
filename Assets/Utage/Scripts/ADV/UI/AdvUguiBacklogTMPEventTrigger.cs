// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;


namespace Utage
{
	
	// 複数サウンドが設定されている場合のバックログで、TextMeshPro用の当たり判定をとるのに必要なイベントトリガー
	[AddComponentMenu("Utage/TextMeshPro/AdvUguiBacklogTMPEventTrigger")]
	public class AdvUguiBacklogTMPEventTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler
	{
		AdvUguiBacklogTMP BacklogTMP { get; set; }
		TMP_Text TextMeshPro => BacklogTMP.TextMeshProLogText.TextMeshPro;
		public Color hoverColor = ColorUtil.Red;

		class Log
		{
			public AdvBacklog.AdvBacklogDataInPage DataInPage { get; }
			public int BeginIndex { get; }
			public int Length { get; }
			public int EndIndex => BeginIndex + Length -1; 
			List<Color32[]> DefaultColors { get; } = new List<Color32[]>(); 
			public Log(AdvBacklog.AdvBacklogDataInPage dataInPage, int beginIndex)
			{
				DataInPage = dataInPage;
				BeginIndex = beginIndex;
				var textData = new TextData(dataInPage.LogText);
				Length = textData.Length;
				
#if UNITY_EDITOR
				if (Length != textData.NoneMetaString.LengthWithSurrogatePairs())
				{
					Length = textData.NoneMetaString.LengthWithSurrogatePairs();
					Debug.LogError("サロゲートペア文字列の長さと一致しない\n"+ $"{BeginIndex}~{EndIndex} Len={Length}");
				}

#endif

				for (int i = BeginIndex; i <= EndIndex; i++)
				{
					DefaultColors.Add(null);
				}
			}
			public bool ContainsTextIndex(int characterIndex)
			{
				return BeginIndex <= characterIndex && characterIndex <= EndIndex;
			}
			public bool TrySetDefaultColors(int characterIndex, Color32 color0, Color32 color1, Color32 color2, Color32 color3)
			{
				int index = characterIndex - BeginIndex;
				if (DefaultColors[index] != null) return false;
				DefaultColors[index] = new Color32[4]{color0, color1, color2, color3};
				return true;
			}
			public bool TryGetDefaultColors(int characterIndex, out Color32[] colors)
			{
				int index = characterIndex - BeginIndex;
				if (DefaultColors[index] == null)
				{
					colors = null;
					return false;
				}
				colors = DefaultColors[index];
				return true;
			}
		}
		List<Log> LogList { get; } = new List<Log>();

		bool IsEntered { get; set; }
		Log CurrentTarget { get; set; }
		Camera Camera { get; set; }
			
		public void InitAsBackLog(AdvUguiBacklogTMP advUguiBacklogTMP)
		{
			BacklogTMP = advUguiBacklogTMP;
			if(!BacklogTMP.TextMeshProLogText.TextMeshPro.raycastTarget)
			{
				Debug.LogWarning("TextMeshPro's raycastTarget is false. Unable to determine if backlog is correct",this);
			}
			LogList.Clear();
			int textIndex = 0;
			foreach (var dataInPage in BacklogTMP.Data.DataList)
			{
				var log = new Log(dataInPage, textIndex);
				LogList.Add(log);
				textIndex = log.EndIndex+1;
			}
		}
		
		public void OnPointerClick(PointerEventData eventData)
		{
			if(BacklogTMP==null) return;
			var target = HitTest(eventData.position,eventData.pressEventCamera);
			if(target==null) return;
			BacklogTMP.OnClicked(target.DataInPage);
		}

		public void OnPointerDown(PointerEventData eventData) { }

		//当たり判定に入ったとき
		public void OnPointerEnter(PointerEventData eventData)
		{
			if(BacklogTMP==null) return;
			IsEntered = true;
			Camera = eventData.enterEventCamera;
			ChangeCurrentTarget(HitTest(eventData.position,eventData.pressEventCamera));
		}

		//当たり判定から出た
		public void OnPointerExit(PointerEventData eventData)
		{
			if(BacklogTMP==null) return;
			IsEntered = false;
			ChangeCurrentTarget(null);
		}

		void Update()
		{
			if(Camera==null || !IsEntered) return;
			// 現在のマウス位置を取得する
			var mousePosition = InputUtil.GetMousePosition();
			ChangeCurrentTarget(HitTest(mousePosition,Camera));
		}

		Log HitTest(Vector2 screenPoint, Camera cam)
		{
			int characterIndex = TMP_TextUtilities.FindNearestCharacter(TextMeshPro, screenPoint, cam, true);
			if (characterIndex < 0) return null;

			foreach (var log in LogList)
			{
				if (log.ContainsTextIndex(characterIndex))
				{
					return log;
				}
			}
			return null;
		}

		void ChangeCurrentTarget(Log target)
		{
			if (CurrentTarget == target) return;
			if (CurrentTarget != null)
			{
				ResetEffectColor(CurrentTarget);
			}
			CurrentTarget = target;
			if (CurrentTarget != null)
			{
				if (!string.IsNullOrEmpty(CurrentTarget.DataInPage.VoiceFileName))
				{
					ChangeEffectColor(CurrentTarget,hoverColor);
				}
				else
				{
					ResetEffectColor(CurrentTarget);
				}
			}
		}
		
		void ResetEffectColor(Log log)
		{
			for(int i = log.BeginIndex; i <= log.EndIndex; ++i)
			{
				if (log.TryGetDefaultColors(i, out Color32[] colors))
				{
					ChangeColor(log, TextMeshPro,i,colors);
				}
			}
			// メッシュを再構築して変更を反映する
			TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
		}

		static Color32[] CacheColors { get; } = new Color32[4];

		void ChangeEffectColor(Log log, Color color)
		{
//			Debug.Log($"{log.BeginIndex}~{log.EndIndex} in {TextMeshPro.textInfo.characterCount}");
			
			for (int i = 0; i < 4; i++)
			{
				CacheColors[i] = color;
			}
			for(int i = log.BeginIndex; i <= log.EndIndex; ++i)
			{
				ChangeColor(log, TextMeshPro,i,CacheColors);
			}
			// メッシュを再構築して変更を反映する
			TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
		}
		void ChangeColor(Log log, TMP_Text textMeshPro, int index, Color32[] colors)
		{
			// インデックスがテキストの範囲内であることを確認する
			var textInfo = textMeshPro.textInfo;
			if (index < 0 || index >= textInfo.characterCount) return;

			var characterInfo = textInfo.characterInfo[index];
            
			if (!characterInfo.isVisible)
				return;

         
			// 現在の文字の Material と 頂点 の位置を取得
			var materialIndex = characterInfo.materialReferenceIndex;
			var vIndex = characterInfo.vertexIndex;
            
			var colors32 = textMeshPro.textInfo.meshInfo[materialIndex].colors32;
			log.TrySetDefaultColors(index, colors32[vIndex + 0], colors32[vIndex + 1], colors32[vIndex + 2], colors32[vIndex + 3]);
			for (var i = 0; i < 4; i++)
			{
				colors32[vIndex + i] = colors[i];
			}
		}
	}
}
