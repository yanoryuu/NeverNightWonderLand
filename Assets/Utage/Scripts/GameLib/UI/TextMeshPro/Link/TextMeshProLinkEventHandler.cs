using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UtageExtensions;


namespace Utage
{
	// <link>タグを持ったテキストのイベント処理をするコンポーネント
	public class TextMeshProLinkEventHandler : MonoBehaviour
		, IPointerEnterHandler
		, IPointerExitHandler
		, IPointerClickHandler
		, IPointerMoveHandler
	{
		public TMP_Text TextMeshPro => this.GetComponentCache(ref textMeshPro);
		TMP_Text textMeshPro;

		public bool AutoSwitchRaycastTarget => autoSwitchRaycastTarget;
		[SerializeField] bool autoSwitchRaycastTarget = true;

		//テキストが変更されたときのイベント
		//初期化（OnEnable）でも呼ばれる
		public UnityEvent OnTextChanged => onTextChanged;
		[SerializeField] UnityEvent onTextChanged = new UnityEvent();

		//マウスがリンクに入ったときのイベント
		//リンクインデックスが引数
		public UnityEvent<int> OnEnterLink => onEnterLink;
		[SerializeField] UnityEvent<int> onEnterLink = new UnityEvent<int>();

		//マウスがリンクから外れたときのイベント
		//リンクインデックスが引数
		public UnityEvent<int> OnExitLink => onExitLink;
		[SerializeField] UnityEvent<int> onExitLink = new UnityEvent<int>();

		//リンクをクリックしたときのイベント
		//リンクインデックスが引数
		public UnityEvent<int> OnClickLink => onClickLink;
		[SerializeField] UnityEvent<int> onClickLink = new UnityEvent<int>();

		//リンク外のテキストをクリックしたときのイベント
		public UnityEvent<BaseEventData> OnClickNoLink => onClickNoLink;
		[SerializeField] UnityEvent<BaseEventData> onClickNoLink = new ();


		//現在のマウス位置にあるリンクのインデックス
		int LinkIndex { get; set; } = -1;


		public TMP_LinkInfo GetLinkInfo(int linkIndex)
		{
			return TextMeshPro.textInfo.linkInfo[linkIndex];
		}

		
		void OnEnable()
		{
			TMPro_EventManager.TEXT_CHANGED_EVENT.Add(TextChanged);
			TextChanged();
		}

		void OnDisable()
		{
			Clear();
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(TextChanged);
		}

		void Clear()
		{
			LinkIndex = -1;
		}
		
		void TextChanged(UnityEngine.Object obj)
		{
			if (obj == this.textMeshPro)
			{
				TextChanged();
			}
		}

		void TextChanged()
		{
			Clear();
			if(AutoSwitchRaycastTarget)
			{
				TextMeshPro.raycastTarget = TextMeshPro.textInfo.linkCount > 0;
			}
			OnTextChanged.Invoke();
			foreach (var eventChangeText in this.GetComponents<ITextMeshProLinkEventChangeText>())
			{
				eventChangeText.OnChangeText();
			}
			UpdateLinkIndexAtMousePosition();
		}
		
		public void OnPointerClick(PointerEventData eventData)
		{
			//左クリックのみに反応
			if (eventData.button != PointerEventData.InputButton.Left) return;
			
			int linkIndex = TextMeshProUtil.FindIntersectingLinkAtMousePosition(TextMeshPro);
			if (linkIndex < 0)
			{
				OnClickNoLink.Invoke(eventData);
				foreach (var eventClick in this.GetComponents<ITextMeshProLinkEventClickNoLink>())
				{
					eventClick.OnClickNoLink(eventData);
				}
				return;
			}

			OnClickLink.Invoke(linkIndex);
			foreach (var eventClick in this.GetComponents<ITextMeshProLinkEventClick>())
			{
				eventClick.OnClickLink(LinkIndex);
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			UpdateLinkIndexAtMousePosition();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			ChangeLinkIndex(-1);
		}

		public void OnPointerMove(PointerEventData eventData)
		{
			UpdateLinkIndexAtMousePosition();
		}

		void UpdateLinkIndexAtMousePosition()
		{
			int linkIndex = TextMeshProUtil.FindIntersectingLinkAtMousePosition(TextMeshPro);
			if (LinkIndex != linkIndex)
			{
				ChangeLinkIndex(linkIndex);
			}
		}

		void ChangeLinkIndex(int index)
		{
			int oldLinkIndex = LinkIndex;
			LinkIndex = index;
			if (oldLinkIndex != -1)
			{
				//リンクから外れた
				OnExitLink.Invoke(oldLinkIndex);
				foreach (var eventExit in this.GetComponents<ITextMeshProLinkEventExit>())
				{
					eventExit.OnExitLink(oldLinkIndex);
				}
			}
			if (LinkIndex != -1)
			{
				//リンクに入った
				OnEnterLink.Invoke(LinkIndex);
				foreach (var eventEnter in this.GetComponents<ITextMeshProLinkEventEnter>())
				{
					eventEnter.OnEnterLink(LinkIndex);
				}
			}
		}

		//IDのタイプをチェックする
		//カンマ区切りの前半にタイプがあるので、それをチェックする
		//enableNoneIdTypeはカンマがない場合にタイプチェックをtrueにするかfalseにするか
		public bool CheckLinkIdType(int index, string idType, bool enableNoneIdType = false)
		{
			var linkInfo = GetLinkInfo(index);
			var id = linkInfo.GetLinkID();
			int commaIndex = id.IndexOf(',');
			if (commaIndex != -1)
			{
				//カンマがあったらカンマの前の文字列で比較する
				int length = idType.Length;
				return string.Compare(id.Trim(),0, idType, 0, length, StringComparison.OrdinalIgnoreCase) == 0;
			}
			else
			{
				//カンマがなかった
				return enableNoneIdType;
			}
		}

		//タイプ解析したIDを返す
		//カンマ区切りの前半にタイプがあるので、それをチェックする
		//enableNoneIdTypeはカンマがない場合にタイプチェックをtrueにするかfalseにするか
		public bool TryGetLinkIdParsedByType(int index, string idType, out string idParsed, bool enableNoneIdType = false)
		{
			idParsed = "";
			var linkInfo = GetLinkInfo(index);
			var id = linkInfo.GetLinkID();
			int commaIndex = id.IndexOf(',');
			if (commaIndex != -1)
			{
				//カンマがあったらカンマの前の文字列で比較する
				int length = idType.Length;
				if (string.Compare(id.Trim(), 0, idType, 0, length, StringComparison.OrdinalIgnoreCase) != 0)
				{
					return false;
				}
				
				// カンマの後ろの文字列を返す
				idParsed = id.Substring(commaIndex + 1);
				return true;
			}
			else
			{
				//カンマがなかった
				idParsed = id;
				return enableNoneIdType;
			}
		}
		
		//タイプ解析したIDを全て取得する
		public IEnumerable<int> GetAllLinkIndexParsedByType(string idType, bool enableNoneIdType =false)
		{
			var textInfo = TextMeshPro.textInfo;
			for(int i = 0; i < textInfo.linkCount; i++)
			{
				if (CheckLinkIdType(i, idType, enableNoneIdType))
				{
					yield return i;
				}
			}
		}
		
		public void SetLinkColor(int index, Color32 textColor, Color32 underlineColor)
		{
			var linkInfo = GetLinkInfo(index);
			var textInfo = TextMeshPro.textInfo;
			int i0 = linkInfo.linkTextfirstCharacterIndex;

			for (var i = 0; i < linkInfo.linkTextLength; i++)
			{
				var characterInfo = textInfo.characterInfo[i + i0];
				
				//アンダーラインの色を設定
				//非表示でも処理が必要。
				if ((characterInfo.style & FontStyles.Underline) == FontStyles.Underline)
				{
					//アンダーラインは予約文字なので0番のメッシュを使う
					var underlineColors32 = textInfo.meshInfo[0].colors32;
					SetUnderlineColor(characterInfo.underlineVertexIndex, underlineColors32, underlineColor);
				}

				if (!characterInfo.isVisible)
					continue;

				// 現在の文字の Material と 頂点 の位置を取得
				var materialIndex = characterInfo.materialReferenceIndex;
				var colors32 = textInfo.meshInfo[materialIndex].colors32;
				SetCharacterColor(characterInfo, colors32, textColor);
			}
			TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
		}
		
		//文字の色を設定
		void SetCharacterColor(TMP_CharacterInfo characterInfo, Color32[] colors32, Color32 textColor)
		{
			var vIndex = characterInfo.vertexIndex;
			for (var i = 0; i < 4; i++)
			{
				colors32[vIndex + i] = textColor;
			}
		}

		void SetUnderlineColor(int underlineVertexIndex, Color32[] colors32, Color32 underLinColor)
		{
			//アンダーラインの頂点数
			//TMP_Text.csのDrawUnderlineMeshの中で設定されている
			const int underlineVertexCount = 12;

			if (underlineVertexIndex + underlineVertexCount > colors32.Length)
			{
				//インデックスオーバー
				return;
			}

			// アンダーラインの色を設定
			for (int i = 0; i < underlineVertexCount; i++)
			{
				colors32[underlineVertexIndex + i] = underLinColor;
			}
		}
	}
}
