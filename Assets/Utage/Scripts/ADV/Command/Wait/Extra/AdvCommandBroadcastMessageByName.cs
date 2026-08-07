// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing System;
using UnityEngine;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// コマンド：ゲーム固有の独自処理のためにSendMessageをする
	/// </summary>
	public class AdvCommandBroadcastMessageByName : AdvCommand
	{
		enum TargetType
		{
			Default,
			UtageObject,
			RenderTexture,
		}
		readonly string name;
		readonly string function;
		readonly TargetType targetType;
		public bool IsWait { get; set; }
		public AdvEngine Engine { get; private set; }
		public AdvCommandBroadcastMessageByName(StringGridRow row)
			: base(row)
		{
			name = ParseCell<string>(AdvColumnName.Arg1);
			function = ParseCell<string>(AdvColumnName.Arg2);
			targetType = ParseCellOptional(AdvColumnName.Arg3,TargetType.Default);
		}

		public override void DoCommand(AdvEngine engine)
		{
			Engine = engine;
			GameObject target = FindTarget(engine);
			if(target==null) return;

			target.BroadcastMessage(function, this, SendMessageOptions.RequireReceiver);
		}

		GameObject FindTarget(AdvEngine engine)
		{
			GameObject target = null;
			switch (targetType)
			{
				case TargetType.UtageObject:
					target = engine.GraphicManager.FindObjectOrLayer(name);
					if (target == null)
					{
						Debug.LogError(name + " is not found in Utage Objects");
					}
					break;
				case TargetType.RenderTexture:
					AdvGraphicObject obj = engine.GraphicManager.FindObject(name);
					if (obj == null)
					{
						Debug.LogError(name + " is not found in Utage Objects");
					}
					else
					{
						target = obj.TargetObject.gameObject;
					}
					break;
				case TargetType.Default:
				default:
					target= GameObject.Find(name);
					if (target == null)
					{
						Debug.LogError(name + " is not found in current scene");
					}
					break;
			}
			return target;
		}

		public override bool Wait(AdvEngine engine)
		{
			return IsWait;
		}
	}
}