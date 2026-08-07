
// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{
	[Serializable]
	public class AdvGraphicObjectEvent : UnityEvent<AdvGraphicObject>
	{
	}
}
