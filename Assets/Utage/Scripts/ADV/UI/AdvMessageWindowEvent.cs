// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UtageExtensions;

namespace Utage
{
	[Serializable]
	public class AdvMessageWindowEvent : UnityEvent<AdvMessageWindow>
	{
	}
}
