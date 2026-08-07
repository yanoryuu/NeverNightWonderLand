// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Utage
{

	/// <summary>
	/// シナリオシートの行データからAdvCommandを生成するファクトリクラス。
	/// 約80種のコマンドIDを定義し、IDに対応するコマンドインスタンスを返す。
	/// カスタムコマンドはOnCreateCustomCommandFromIDコールバックで拡張できる。
	/// </summary>
	public static class AdvCommandParser
	{
		//独自コマンドを作成するためのコールバック
		//独自にカスタムしたい、IDのコマンドだけ作成すればいい
		public delegate void CreateCustomCommandFromID( string id, StringGridRow row, AdvSettingDataManager dataManager, ref AdvCommand command);
		[field: RuntimeInitializeStaticField] public static CreateCustomCommandFromID OnCreateCustomCommandFromID;

		/// <summary>
		/// コマンド生成
		/// </summary>
		/// <param name="row">行データ</param>
		/// <param name="dataManager">データマネージャー</param>
		/// <returns>生成されたコマンド</returns>
		public static AdvCommand CreateCommand(StringGridRow row, AdvSettingDataManager dataManager)
		{
			if (row.IsCommentOut)
			{
				//コメント
				return null;
			}
			Profiler.BeginSample("CreateCommandRow");

			string id = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Command, "");
			Profiler.BeginSample("Check Comment");
			if (IsComment(id,row))
			{
				Profiler.EndSample();
				Profiler.EndSample();
				//コメント
				return null;
			}
			Profiler.EndSample();

			///基本のコマンド解析処理
			AdvCommand command = CreateCommand(ParseCommandID(id,row), row, dataManager);

			Profiler.BeginSample("Check IsAllEmptyCellNamedColumn");
			if (command == null)
			{
				//列名がついたセル全て空かどうか
				if (row.IsAllEmptyCellNamedColumn())
				{
				}
				else
				{
					//記述ミス
					Debug.LogError(row.ToErrorString(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.CommandParseNull)));
					Profiler.EndSample();
					Profiler.EndSample();
					return new AdvCommandError(row);

				}
			}
			Profiler.EndSample();
			Profiler.EndSample();
			return command;
		}

		public const string IdError = "Error";						//構文エラー
		public const string IdComment = "Comment";					//コメント
		public const string IdCharacter = "Character";				//キャラクター＆台詞表示
		public const string IdCharacterOff = "CharacterOff";		//キャラクター表示オフ
		public const string IdText = "Text";						//テキスト表示（地の文）
		public const string IdBg = "Bg";							//背景表示・切り替え
		public const string IdBgOff = "BgOff";						//キャラクター表示オフ
		public const string IdBgEvent = "BgEvent";					//イベント絵表示・切り替え
		public const string IdBgEventOff = "BgEventOff";			//イベント絵表示
		public const string IdSprite = "Sprite";					//スプライト表示
		public const string IdSpriteOff = "SpriteOff";              //スプライト表示オフ

		public const string IdParticle = "Particle";                //パーティクル再生
		public const string IdParticleOff = "ParticleOff";          //パーティクル表示オフ

		public const string IdLayerReset = "LayerReset";            //レイヤー操作　Reset（位置なども初期状態に戻す）
		public const string IdLayerOff = "LayerOff";				//レイヤー操作　オブジェクトすべて消す
		public const string IdChangeLayer = "ChangeLayer";			//レイヤー操作　オブジェクトのレイヤーを変更

		public const string IdSetPivot = "SetPivot";				//オブジェクトのピボットを設定
		public const string IdResetPivot = "ResetPivot";			//オブジェクトのピボットを元に戻す

		public const string IdMovie = "Movie";                      //ムービー再生
		public const string IdVideo = "Video";                      //VideoClip再生

		public const string IdSe = "Se";							//SE再生
		public const string IdStopSe = "StopSe";					//SE停止
		public const string IdBgm = "Bgm";							//BGM再生
		public const string IdStopBgm = "StopBgm";					//BGM停止
		public const string IdAmbience = "Ambience";				//環境音再生
		public const string IdStopAmbience = "StopAmbience";		//環境音停止
		public const string IdVoice = "Voice";						//ボイス再生
		public const string IdStopVoice = "StopVoice";				//ボイス停止
		public const string IdStopSound = "StopSound";              //サウンドの停止
		public const string IdChangeSoundVolume = "ChangeSoundVolume"; //サウンドのボリューム変更

		public const string IdSelection = "Selection";				//選択肢表示
		public const string IdSelectionEnd = "SelectionEnd";		//選択肢追加終了
		public const string IdSelectionClick = "SelectionClick";	//クリックによる選択肢表示
		public const string IdJump = "Jump";						//他シナリオにジャンプ
		public const string IdJumpRandom = "JumpRandom";			//ランダムジャンプ
		public const string IdJumpRandomEnd = "JumpRandomEnd";		//ランダムジャンプ終了
	
		public const string IdJumpSubroutine = "JumpSubroutine";			//サブルーチンへ飛ぶ
		public const string IdJumpSubroutineRandom = "JumpSubroutineRandom";			//ランダムにサブルーチンへ飛ぶ
		public const string IdJumpSubroutineRandomEnd = "JumpSubroutineRandomEnd";	//ランダムにサブルーチンへ飛ぶ終了
		public const string IdEndSubroutine = "EndSubroutine";		//サブルーチン終了
		public const string IdExitSubroutine = "ExitSubroutine";	//サブルーチンを抜ける

		public const string IdBeginMacro = "BeginMacro";			//マクロ定義開始
		public const string IdEndMacro = "EndMacro";				//マクロ定義終了

		public const string IdEffect = "Effect";					//エフェクト表示

		public const string IdWait = "Wait";						//一定時間のウェイト
		public const string IdWaitInput = "WaitInput";				//入力ウェイト
		public const string IdWaitCustom = "WaitCustom";			//カスタムウェイト
		public const string IdWaitConditional = "WaitConditional";	//条件式ウェイト
		public const string IdSkipEffect = "SkipEffect";			//演出の強制スキップ
		
		public const string IdTween = "Tween";						//Tweenアニメ
		public const string IdFadeIn = "FadeIn";					//フェードイン
		public const string IdFadeOut = "FadeOut";					//フェードアウト
		public const string IdShake = "Shake";                      //シェイク

		public const string IdZoomCamera = "ZoomCamera";            //カメラをズームする

		public const string IdVibrate = "Vibrate";                  //バイブレーション

		public const string IdPlayAnimation = "PlayAnimation";      //アニメーションClipの再生

		public const string IdRuleFadeIn = "RuleFadeIn";            //ルール画像つきフェードイン
		public const string IdRuleFadeOut = "RuleFadeOut";          //ルール画像つきフェードアウト
//		public const string IdObjectFadeIn = "ObjectFadeIn";		//オブジェクトに対してのフェードイン
//		public const string IdObjectFadeOut = "ObjectFadeOut";		//オブジェクトに対してのフェードアウト
		public const string IdWaitFadeObjects = "WaitFadeObjects"; //オブジェクトに対してのフェードを待つ
		public const string IdWaitEffectTime = "WaitEffectTime";   //エフェクトの終了時間を待つ
		public const string IdWaitSound = "WaitSound";             //サウンドの再生終了を待つ
		public const string IdWaitVideo = "WaitVideo";             //ビデオオブジェクトの再生終了を待つ

		public const string IdCaptureImage = "CaptureImage";		//キャプチャ画像の作成と表示

		public const string IdImageEffect = "ImageEffect";					//イメージエフェクトの開始
		public const string IdImageEffectOff = "ImageEffectOff";            //イメージエフェクトの終了

		public const string IdPostEffect = "PostEffect";			//ポストエフェクトの開始
		public const string IdPostEffectOff = "PostEffectOff";      //ポストエフェクトの終了

		public const string IdParam = "Param";						//パラメーター代入
		public const string IdIf = "If";							//If文制御
		public const string IdElseIf = "ElseIf";                    //If文制御
		public const string IdElse = "Else";                        //If文制御
		public const string IdEndIf = "EndIf";                      //If文制御

		public const string IdShowMessageWindow = "ShowMessageWindow";		//メッセージウィンドウを強制表示
		public const string IdHideMessageWindow = "HideMessageWindow";		//メッセージウィンドウを強制非表示

		public const string IdShowMenuButton = "ShowMenuButton";			//メニューボタンを強制表示
		public const string IdHideMenuButton = "HideMenuButton";			//メニューボタンを強制非表示

		public const string IdChangeMessageWindow = "ChangeMessageWindow";	//メッセージウィンドウ操作　変更
		public const string IdInitMessageWindow = "InitMessageWindow";		//メッセージウィンドウ操作　初期化


		public const string IdGuiReset = "GuiReset";				//GUI操作　Reset
		public const string IdGuiActive = "GuiActive";				//GUI操作　Active
		public const string IdGuiPosition = "GuiPosition";			//GUI操作　Position
		public const string IdGuiSize = "GuiSize";                  //GUI操作　Size

		public const string IdSendMessage = "SendMessage";					//SendMessage処理（各ゲームに固有の処理のために）
		public const string IdSendMessageByName = "SendMessageByName";		//指定の名前のオブジェクトにSendMessage処理
		public const string IdBroadcastMessageByName = "BroadcastMessageByName";	//指定の名前のオブジェクトにBroadcastMessage処理

		public const string IdEndScenario = "EndScenario";			//シナリオ終了
		public const string IdPauseScenario = "PauseScenario";		//シナリオ中断
		public const string IdEndSceneGallery = "EndSceneGallery";	//シーン回想終了

		public const string IdScenarioLabel = "ScenarioLabel";		//シナリオラベル
		public const string IdPageControler = "PageControl";		//ページの制御

		public const string IdThread = "Thread";                    //スレッド
		public const string IdWaitThread = "WaitThread";            //スレッド終了待ち

		public const string IdEndPage = "EndPage";			        //改ページコマンド
		public const string IdEndThread = "EndThread";              //スレッドやサブルーチンの終了の区切り

		/// <summary>
		/// 基本のコマンド生成処理
		/// </summary>
		/// <param name="row">行データ</param>
		/// <param name="dataManager">データマネージャー</param>
		/// <returns>生成されたコマンド</returns>
		static public AdvCommand CreateCommand(string id, StringGridRow row, AdvSettingDataManager dataManager)
		{
			AdvCommand command = null;

			Profiler.BeginSample("OnCreateCustomCommandFromID");
			///独自のコマンド解析処理があるならそっちを先に
			if (OnCreateCustomCommandFromID != null) OnCreateCustomCommandFromID(id, row, dataManager, ref command);
			Profiler.EndSample();

			if (string.IsNullOrEmpty(id)) return null;
			Profiler.BeginSample(id);
			///基本のコマンド解析処理
			if (command == null) command = CreateCommandDefault(id, row, dataManager);
			Profiler.EndSample();
			//コマンドID（本当ならコンストラクタに置きたいけど、互換性のためにここで設定）
			if (command != null) command.Id = id;
			return command;
		}

		/// <summary>
		/// 基本のコマンド生成処理
		/// </summary>
		/// <param name="id">コマンドID</param>
		/// <param name="dataManager">データマネージャー</param>
		/// <returns>生成されたコマンド</returns>
		static AdvCommand CreateCommandDefault(string id, StringGridRow row, AdvSettingDataManager dataManager)
		{
			switch (id)
			{
				case IdCharacter:
					return new AdvCommandCharacter(row,dataManager);
				case IdText:
					return new AdvCommandText(row, dataManager);
				case IdCharacterOff:
					return new AdvCommandCharacterOff(row);
				case IdBg:
					return new AdvCommandBg(row, dataManager);
				case IdBgOff:
					return new AdvCommandBgOff(row);
				case IdBgEvent:
					return new AdvCommandBgEvent(row, dataManager);
				case IdBgEventOff:
					return new AdvCommandBgEventOff(row);
				case IdSprite:
					return new AdvCommandSprite(row, dataManager);
				case IdSpriteOff:
					return new AdvCommandSpriteOff(row);
				case IdParticle:
					return new AdvCommandParticle(row, dataManager);
				case IdParticleOff:
					return new AdvCommandParticleOff(row);


				case IdLayerOff:
					return new AdvCommandLayerOff(row, dataManager);
				case IdLayerReset:
					return new AdvCommandLayerReset(row, dataManager);
				case IdChangeLayer:
					return new AdvCommandChangeLayer(row, dataManager);

				case IdSetPivot:
					return new AdvCommandSetPivot(row, dataManager);
				case IdResetPivot:
					return new AdvCommandResetPivot(row, dataManager);

				case IdVideo:
					return new AdvCommandVideo(row, dataManager);

				case IdTween:
					return new AdvCommandTween(row, dataManager);
				case IdFadeIn:
					return new AdvCommandFadeIn(row, dataManager);
				case IdFadeOut:
					return new AdvCommandFadeOut(row, dataManager);
				case IdShake:
					return new AdvCommandShake(row, dataManager);
				case IdVibrate:
					return new AdvCommandVibrate(row, dataManager);

				case IdZoomCamera:
					return new AdvCommandZoomCamera(row, dataManager);

				case IdPlayAnimation:
					return new AdvCommandPlayAnimatin(row, dataManager);

				case IdRuleFadeIn:
					return new AdvCommandRuleFadeIn(row,dataManager);
				case IdRuleFadeOut:
					return new AdvCommandRuleFadeOut(row,dataManager);
				case IdWaitFadeObjects:
					return new AdvCommandWaitFadeObjects(row, dataManager);
				case　IdWaitEffectTime:
					return new AdvCommandWaitEffectTime(row, dataManager);
				case　IdWaitSound:
					return new AdvCommandWaitSound(row, dataManager);
				case　IdWaitVideo:
					return new AdvCommandWaitVideo(row, dataManager);

/*				case IdObjectFadeIn:
					return new AdvCommandObjectFadeIn(row);
				case IdObjectFadeOut:
					return new AdvCommandObjectFadeOut(row);
*/
				
				case IdCaptureImage:
					return new AdvCommandCaptureImage(row);
					

				case IdImageEffect:
					return new AdvCommandImageEffect(row, dataManager);
				case IdImageEffectOff:
					return new AdvCommandImageEffectOff(row, dataManager);

				case IdPostEffect:
					return new AdvCommandPostEffect(row, dataManager);
				case IdPostEffectOff:
					return new AdvCommandPostEffectOff(row, dataManager);


				case IdSe:
					return new AdvCommandSe(row, dataManager);
				case IdStopSe:
					return new AdvCommandStopSe(row, dataManager);
				case IdBgm:
					return new AdvCommandBgm(row, dataManager);
				case IdStopBgm:
					return new AdvCommandStopBgm(row);
				case IdAmbience:
					return new AdvCommandAmbience(row, dataManager);
				case IdStopAmbience:
					return new AdvCommandStopAmbience(row);
				case IdVoice:
					return new AdvCommandVoice(row, dataManager);
				case IdStopVoice:
					return new AdvCommandStopVoice(row);
				case IdStopSound:
					return new AdvCommandStopSound(row);
				case IdChangeSoundVolume:
					return new AdvCommandChangeSoundVolume(row);

				case IdWait:
					return new AdvCommandWait(row);
				case IdWaitInput:
					return new AdvCommandWaitInput(row);
				case IdWaitCustom:
					return new AdvCommandWaitCustom(row);
				case IdWaitConditional:
					return new AdvCommandWaitConditional(row,dataManager);
				case IdSkipEffect:
					return new AdvCommandSkipEffect(row,dataManager);

				case IdParam:
					return new AdvCommandParam(row, dataManager);
				case IdSelection:
					return new AdvCommandSelection(row, dataManager);
				case IdSelectionEnd:
					return new AdvCommandSelectionEnd(row, dataManager);
				case IdSelectionClick:
					return new AdvCommandSelectionClick(row, dataManager);
				case IdJump:
					return new AdvCommandJump(row, dataManager);
				case IdJumpRandom:
					return new AdvCommandJumpRandom(row, dataManager);
				case IdJumpRandomEnd:
					return new AdvCommandJumpRandomEnd(row, dataManager);

				case IdJumpSubroutine:
					return new AdvCommandJumpSubroutine(row, dataManager);
				case IdJumpSubroutineRandom:
					return new AdvCommandJumpSubroutineRandom(row, dataManager);
				case IdJumpSubroutineRandomEnd:
					return new AdvCommandJumpSubroutineRandomEnd(row, dataManager);
				case IdEndSubroutine:
					return new AdvCommandEndSubroutine(row, dataManager);
				case IdExitSubroutine:
					return new AdvCommandExitSubroutine(row, dataManager);

				case IdIf:
					return new AdvCommandIf(row, dataManager);
				case IdElseIf:
					return new AdvCommandElseIf(row, dataManager);
				case IdElse:
					return new AdvCommandElse(row);
				case IdEndIf:
					return new AdvCommandEndIf(row);

				case IdShowMessageWindow:
					return new AdvCommandShowMessageWindow(row);
				case IdHideMessageWindow:
					return new AdvCommandHideMessageWindow(row);
				case IdShowMenuButton:
					return new AdvCommandShowMenuButton(row);
				case IdHideMenuButton:
					return new AdvCommandHideMenuButton(row);


				case IdChangeMessageWindow:
					return new AdvCommandMessageWindowChangeCurrent(row);
				case IdInitMessageWindow:
					return new AdvCommandMessageWindowInit(row);

				case IdGuiReset:
					return new AdvCommandGuiReset(row);
				case IdGuiActive:
					return new AdvCommandGuiActive(row);
				case IdGuiPosition:
					return new AdvCommandGuiPosition(row);
				case IdGuiSize:
					return new AdvCommandGuiSize(row);

				case IdSendMessage:
					return new AdvCommandSendMessage(row);
				case IdSendMessageByName:
					return new AdvCommandSendMessageByName(row);
				case IdBroadcastMessageByName:
					return new AdvCommandBroadcastMessageByName(row);
				
				case IdEndScenario:
					return new AdvCommandEndScenario(row);
				case IdPauseScenario:
					return new AdvCommandPauseScenario(row);
				case IdEndSceneGallery:
					return new AdvCommandEndSceneGallery(row);

				case IdPageControler:
					return new AdvCommandPageControler(row,dataManager);
				case IdScenarioLabel:
					return new AdvCommandScenarioLabel(row);

				case IdThread:
					return new AdvCommandThread(row);
				case IdWaitThread:
					return new AdvCommandWaitThread(row);

				case IdEndThread:
					return new AdvCommandEndThread(row);
				case IdEndPage:
					return new AdvCommandEndPage(row);

				default:
					return null;
			}
		}

		/// <summary>
		/// コマンド名なしの場合のコマンドIDを取得
		/// </summary>
		/// <param name="row">行データ</param>
		/// <param name="dataManager">データマネージャー</param>
		/// <returns>生成するコマンドID</returns>
		static string ParseCommandID(StringGridRow row)
		{
			string id = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Command, "");
			return ParseCommandID(id, row);
		}

		static string ParseCommandID(string id, StringGridRow row)
		{
			Profiler.BeginSample("ParseCommandID");
			if (string.IsNullOrEmpty(id))
			{
				//コマンドなしは、テキスト表示が基本
				if (!AdvParser.IsEmptyCell(row, AdvColumnName.Arg1))
				{
					Profiler.EndSample();
					//パラメーターつきなので、キャラ表示
					return IdCharacter;
				}
				if (!AdvParser.IsEmptyTextCommand(row))
				{
					Profiler.EndSample();
					//テキスト表示
					return IdText;
				}
				else
				{   //なにもないので空データ
					Profiler.EndSample();
					return null;
				}
			}
			else if (IsScenarioLabel(id))
			{
				//シナリオラベル
				id = IdScenarioLabel;
			}
			Profiler.EndSample();
			return id;
		}

		// シナリオラベルかチェック
		static public bool IsScenarioLabel(string str)
		{
			return ( !string.IsNullOrEmpty(str) && str.Length >= 2 && (str[0] == '*'));
		}
		//シナリオラベルとして解析し、成功した場合はtrue・ラベル文字列を返す
		static public bool TryParseScenarioLabel(StringGridRow row, AdvColumnName columnName, out string scenarioLabel)
		{
			string label = AdvParser.ParseCell<string>(row, columnName);
			if (!IsScenarioLabel(label))
			{
				scenarioLabel = label;
				return false;
			}
			else
			{
				if (label.Length >= 3 && label[1] == '*')
				{
					scenarioLabel = row.Grid.SheetName + '*' + label.Substring(2);
				}
				else
				{
					scenarioLabel = label.Substring(1);
				}
				return true;
			}
		}

		//シナリオラベルを解析・取得
		static public string ParseScenarioLabel(StringGridRow row, AdvColumnName name)
		{
			string label;
			if (!TryParseScenarioLabel(row, name, out label))
			{
				Debug.LogError(row.ToErrorString(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.NotScenarioLabel, label)));
			}
			return label;
		}



		//コメントのコマンドかチェック
		static bool IsComment(string command, StringGridRow row)
		{
			if( string.IsNullOrEmpty(command) )
			{
				return false;
			}
			else if (command == IdComment)
			{
				return true;
			}
			else if (command.Length >= 2 && command[0] == '/' && command[1] == '/')
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
