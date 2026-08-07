// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UtageExtensions;

namespace Utage
{
	public interface IAnimationRuleFade
	{
		GameObject gameObject { get; }
		void BeginRuleFade(Texture texture, float vague, bool isPremultipliedAlpha);
		void EndRuleFade();
	}
}
