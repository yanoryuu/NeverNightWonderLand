// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Utage
{

	/// <summary>
	/// iTweenのプレイヤー
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/AdvITweenPlayer")]
	internal class AdvITweenPlayer : MonoBehaviour
	{
		iTweenData data;
		Hashtable hashTbl;
		Action<AdvITweenPlayer> callbackComplete;
		bool isColorSprite;
		int count;
		string tweenName;

		/// <summary>
		/// 無限ループするか
		/// </summary>
		public bool IsEndlessLoop { get; private set; }

		//回数制限付きループ
		public bool IsCountLoop { get { return data.IsCountLoop; } }

		/// <summary>
		/// 再生中か
		/// </summary>
		public bool IsPlaying { get { return isPlaying; } }
		bool isPlaying = false;

		AdvEffectColor target;

		private iTween Tween { get; set; }

		
#if ENABLE_UTAGE_TWEEN_BUG
		//Addなどで重複する場合、OnComleteが呼ばれない対策
		List<AdvITweenPlayer> oldTweenPlayers = new List<AdvITweenPlayer>();
#endif


		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="type">Tweenのデータ</param>
		/// <param name="hashObjects">Tweenのパラメーター</param>
		/// <param name="loopCount">ループ回数</param>
		/// <param name="pixelsToUnits">座標1.0単位辺りのピクセル数</param>
		/// <param name="skipSpeed">スキップ中の演出速度の倍率。0ならスキップなし</param>
		/// <param name="callbackComplete">終了時に呼ばれるコールバック</param>
		public void Init(iTweenData data, bool isUnder2DSpace, float pixelsToUnits, float skipSpeed, bool unscaled, Action<AdvITweenPlayer> callbackComplete)
		{
			this.data = data;
			IsEndlessLoop = data.IsEndlessLoop;
			if (data.Type == iTweenType.Stop) return;

			this.callbackComplete = callbackComplete;

			data.ReInit();
			hashTbl = iTween.Hash(data.MakeHashArray());


			if (iTweenData.IsPostionType(data.Type))
			{
				//座標系の処理は、基本的に2D座標の値で入力されているので、
				//非ローカルの場合や、2D座標じゃない場合は、pixelsToUnitsで3D座標値に変換する
				if (!isUnder2DSpace || (!hashTbl.ContainsKey("islocal") || (bool)hashTbl["islocal"] == false) )
				{
					if (hashTbl.ContainsKey("x")) hashTbl["x"] = (float)hashTbl["x"] / pixelsToUnits;
					if (hashTbl.ContainsKey("y")) hashTbl["y"] = (float)hashTbl["y"] / pixelsToUnits;
					if (hashTbl.ContainsKey("z")) hashTbl["z"] = (float)hashTbl["z"] / pixelsToUnits;
				}
			}

			//スキップ中なら演出時間を調整
			if (skipSpeed > 0)
			{
				bool isSpeed = hashTbl.ContainsKey("speed");
				if (isSpeed) hashTbl["speed"] = (float)hashTbl["speed"] * skipSpeed;

				bool isTime = hashTbl.ContainsKey("time");
				if(isTime)
				{
					hashTbl["time"] = (float)hashTbl["time"]/ skipSpeed;
				}
				else if(!isSpeed)
				{
					hashTbl["time"] = 1.0f / skipSpeed;
				}
			}

			//カラーの処理を2D仕様に
			if (data.Type == iTweenType.ColorTo || data.Type == iTweenType.ColorFrom)
			{
				this.target = this.gameObject.GetComponent<AdvEffectColor>();
				if (target != null)
				{
					Color currentColor = target.TweenColor;
					if (data.Type == iTweenType.ColorTo)
					{
						hashTbl["from"] = currentColor;
						hashTbl["to"] = ParaseTargetColor(hashTbl, currentColor);
					}
					else if (data.Type == iTweenType.ColorFrom)
					{
						hashTbl["from"] = ParaseTargetColor(hashTbl, currentColor);
						hashTbl["to"] = currentColor;
					}
					hashTbl["onupdate"] = "OnColorUpdate";
					isColorSprite = true;
				}
			}


			//時間をタイムスケールに比例させない
			if (unscaled)
			{
				hashTbl["ignoretimescale"] = true;
			}

			//終了時に呼ばれるメッセージを登録
			hashTbl["oncomplete"] = "OnCompleteTween";
			hashTbl["oncompletetarget"] = this.gameObject;
			hashTbl["oncompleteparams"] = this;

			//停止処理用に名前を設定
			tweenName = this.GetHashCode().ToString();
			hashTbl["name"] = tweenName;
		}


		/// <summary>
		/// Tween処理開始
		/// </summary>
		public void Play()
		{
			if (TryStoreOldTween())
			{
			}
			
			isPlaying = true;
			if (data.Type == iTweenType.Stop)
			{
				iTween.Stop(gameObject);
				return;
			}

			//GetComponentsして前後で新しくできたものを取得するための準備
			var oldTweens = this.GetComponents<iTween>(); 
			PlaySub();
			if (!IsPlaying) return;

			//開始されたTweenが取得できないので
			//GetComponentsして前後で新しくできたものを取得する
			var newTweens = this.GetComponents<iTween>();
			Tween = null;
			foreach (var newTween in newTweens)
			{
				bool isFind = false;
				foreach (var oldTween in oldTweens)
				{
					if (oldTween == newTween)
					{
						isFind = true;
						break;
					}
				}

				if (!isFind)
				{
					Tween = newTween;
					break;
				}
			}

			if (Tween == null)
			{
				Debug.LogError("Tween is missing");
			}

			TrySetImmediately();
		}

		void PlaySub()
		{
			if (isColorSprite)
			{
				iTween.ValueTo(gameObject, hashTbl);
				return;
			}


			switch (data.Type)
			{
				case iTweenType.ColorFrom:
					iTween.ColorFrom(gameObject, hashTbl);
					break;
				case iTweenType.ColorTo:
					iTween.ColorTo(gameObject, hashTbl);
					break;
				case iTweenType.MoveAdd:
					iTween.MoveAdd(gameObject, hashTbl);
					break;
				case iTweenType.MoveBy:
					iTween.MoveBy(gameObject, hashTbl);
					break;
				case iTweenType.MoveFrom:
					iTween.MoveFrom(gameObject, hashTbl);
					break;
				case iTweenType.MoveTo:
					iTween.MoveTo(gameObject, hashTbl);
					break;
				case iTweenType.PunchPosition:
					iTween.PunchPosition(gameObject, hashTbl);
					break;
				case iTweenType.PunchRotation:
					iTween.PunchRotation(gameObject, hashTbl);
					break;
				case iTweenType.PunchScale:
					iTween.PunchScale(gameObject, hashTbl);
					break;
				case iTweenType.RotateAdd:
					iTween.RotateAdd(gameObject, hashTbl);
					break;
				case iTweenType.RotateBy:
					iTween.RotateBy(gameObject, hashTbl);
					break;
				case iTweenType.RotateFrom:
					iTween.RotateFrom(gameObject, hashTbl);
					break;
				case iTweenType.RotateTo:
					iTween.RotateTo(gameObject, hashTbl);
					break;
				case iTweenType.ScaleAdd:
					iTween.ScaleAdd(gameObject, hashTbl);
					break;
				case iTweenType.ScaleBy:
					iTween.ScaleBy(gameObject, hashTbl);
					break;
				case iTweenType.ScaleFrom:
					iTween.ScaleFrom(gameObject, hashTbl);
					break;
				case iTweenType.ScaleTo:
					iTween.ScaleTo(gameObject, hashTbl);
					break;
				case iTweenType.ShakePosition:
					CancelShake(iTweenType.ShakePosition);
					iTween.ShakePosition(gameObject, hashTbl);
					break;
				case iTweenType.ShakeRotation:
					CancelShake(iTweenType.ShakeRotation);
					iTween.ShakeRotation(gameObject, hashTbl);
					break;
				case iTweenType.ShakeScale:
					CancelShake(iTweenType.ShakeScale);
					iTween.ShakeScale(gameObject, hashTbl);
					break;
				default:
					isPlaying = false;
					Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownType, data.Type.ToString()));
					break;
			}
		}

		void CancelShake( iTweenType type )
		{
			foreach (var old in this.GetComponents<AdvITweenPlayer>())
			{
				if(old==this) continue;
				if (old.data.Type == type && old.isPlaying)
				{
					old.SkipTween(old.Tween,false);
				}
			}
		}

		//時間がゼロ秒だった場合に、即座に終わらせる
		bool TrySetImmediately()
		{
			if (Tween == null) return false;

			if (data.Loop != iTween.LoopType.none) return false;
			
			var time = hashTbl["time"];
			if (time == null) return false;
			if ((float)time > 0.0f)
			{
				return false;
			}

			var delay = hashTbl["delay"];
			if (delay != null && (float)delay > 0.0f) return false;

			Tween.time = 0;
			CallITweenPrivateMethod(Tween, "TweenStart");
			CallITweenPrivateMethod(Tween, "UpdatePercentage");
			CallITweenPrivateMethod(Tween, "Update");
			UpdateAutoDestroy();
			return true;
		}
		

		private void Update()
		{
			UpdateAutoDestroy();
		}

		void UpdateAutoDestroy()
		{
			//重複などによって、Tweenが消されてしまったときはコールバックが呼ばれないので
			//Updateで監視してDestroyする
			if ( isPlaying && Tween == null)
			{
				Destroy(this);
			}
		}


		bool TryStoreOldTween()
		{
			bool ret = false;
#if ENABLE_UTAGE_TWEEN_BUG
			AdvITweenPlayer[] players = GetComponents<AdvITweenPlayer>();
			foreach (AdvITweenPlayer player in players)
			{
				if (player == this) continue;
				if (player.isPlaying)
				{
					ret = true;
					oldTweenPlayers.Add(player);
				}
			}
#endif
			return ret;
		}


		Color ParaseTargetColor( Hashtable hashTbl, Color color )
		{
			if (hashTbl.Contains( iTweenData.Color ))
			{
				color = (Color)hashTbl[iTweenData.Color];
			}
			else
			{
				if (hashTbl.Contains(iTweenData.R))
				{
					color.r = (float)hashTbl[iTweenData.R];
				}
				if (hashTbl.Contains(iTweenData.G))
				{
					color.g = (float)hashTbl[iTweenData.G];
				}
				if (hashTbl.Contains(iTweenData.B))
				{
					color.b = (float)hashTbl[iTweenData.B];
				}
				if (hashTbl.Contains(iTweenData.A))
				{
					color.a = (float)hashTbl[iTweenData.A];
				}
			}

			if (hashTbl.Contains(iTweenData.Alpha))
			{
				color.a = (float)hashTbl[iTweenData.Alpha];
			}

			return color;
		}

		/// <summary>
		/// キャンセルして破棄
		/// </summary>
		public void Cancel()
		{
			iTween.StopByName(this.gameObject, tweenName);
			isPlaying = false;
			UnityEngine.Object.Destroy(this);
		}

		/// <summary>
		/// 破棄するときに呼ばれる
		/// </summary>
		void OnDestroy()
		{
#if ENABLE_UTAGE_TWEEN_BUG
			foreach (var item in oldTweenPlayers)
			{
				if (item != null) UnityEngine.Object.Destroy(item);
			}
			oldTweenPlayers.Clear();
#endif
			if (callbackComplete != null)
			{
				callbackComplete(this);
			}
			callbackComplete = null;
		}

		/// <summary>
		/// 再生終了時に呼ばれる
		/// </summary>
		void OnCompleteTween(AdvITweenPlayer arg)
		{
			if (arg != this) return;
			++count;
			if (count >= this.data.LoopCount && !IsEndlessLoop)
			{
				Cancel();
			}
		}

		/// <summary>
		/// カラーの更新時に呼ばれる
		/// </summary>
		/// <param name="color"></param>
		void OnColorUpdate(Color color)
		{
			if(target != null )
			{
				target.TweenColor = color;
			}
		}

		/// <summary>
		/// セーブデータ用のバイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public void Write(BinaryWriter writer)
		{
			data.Write(writer);
		}

		/// <summary>
		/// セーブデータ用のバイナリ読みこみ
		/// </summary>
		/// <param name="pixelsToUnits">座標1.0単位辺りのピクセル数</param>
		/// <param name="reader">バイナリリーダー</param>
		public void Read(BinaryReader reader, bool isUnder2DSpace, float pixelsToUnits, bool unscaled)
		{
			iTweenData data = new iTweenData(reader);
			Init(data, isUnder2DSpace, pixelsToUnits, 1.0f, unscaled, null);
		}

		internal static void WriteSaveData(BinaryWriter writer, GameObject go)
		{
			//無限ループのTweenがある場合は、Tween情報を書き込む
			AdvITweenPlayer[] tweenArray = go.GetComponents<AdvITweenPlayer>();
			int tweenCount = 0;
			foreach (var tween in tweenArray)
			{
				if (tween.IsEndlessLoop) ++tweenCount;
			}
			writer.Write(tweenCount);
			foreach (var tween in tweenArray)
			{
				if (tween.IsEndlessLoop) tween.Write(writer);
			}
		}

		internal static void ReadSaveData(BinaryReader reader, GameObject go, bool isUnder2DSpace, float pixelsToUnits, bool unscaled)
		{
			//Tweenがある場合は、Tween情報を読み込む
			int tweenCount = reader.ReadInt32();
			for (int i = 0; i < tweenCount; ++i)
			{
				AdvITweenPlayer tween = go.AddComponent<AdvITweenPlayer>();
				tween.Read(reader, isUnder2DSpace, pixelsToUnits, unscaled);
			}
		}



		public bool IsAddType
		{
			get
			{
				switch (data.Type)
				{
					case iTweenType.MoveAdd:
					case iTweenType.RotateAdd:
					case iTweenType.ScaleAdd:
						return true;
					default:
						return false;
				}

			}
		}

		public void SkipToEnd()
		{
#if false
			iTween[] tweenList = GetComponents<iTween>();
			foreach (iTween tween in tweenList)
			{
				SkipTween(tween);
			}
#else
			if (Tween != null)
			{
				SkipTween(Tween,true);
			}
			else
			{
				Debug.LogError("Failed Tween Skip");
			}
#endif
		}

		void SkipTween(iTween tween, bool autoDestroy)
		{
			if(tween==null) return;
			if(Mathf.Approximately(tween.time, 0)) return;
//			if (IsEndlessLoop) return;

			tween.delay = 0;
			Tween.time = 0.000001f;
			if (data.IsCountLoop || IsEndlessLoop)
			{
				if (data.Loop == iTween.LoopType.pingPong && (count + 1) % 2 != data.LoopCount % 2)
				{
					//pingPongは場合は、
					//現在のcountに合わせて
					//スキップ再生を一回追加
					ForceUpdateOnSkipTween();
					Debug.Log("Add ForceUpdateOnSkipTween");
				}
				//回数制限付きループの場合はcountを終わらせるようにスキップする
				count = data.LoopCount;
			}
			//ここで無限ループをオフに
			IsEndlessLoop = false;
			ForceUpdateOnSkipTween();
			if (autoDestroy)
			{
				Update();
			}
		}

		void ForceUpdateOnSkipTween()
		{
			CallITweenPrivateMethod(Tween, "UpdatePercentage");
			CallITweenPrivateMethod(Tween, "Update");
		}
		
		//iTweenのプライベートメッソドを呼ぶ
		void CallITweenPrivateMethod(iTween tween, string function)
		{
			var setMethod = tween.GetType().GetMethod(function, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (setMethod != null) {
				setMethod.Invoke(tween, null);
			}
			else
			{
				Debug.LogErrorFormat("call private method error {0} type={1}",function,tween.GetType());
			}
		}
	}
}
