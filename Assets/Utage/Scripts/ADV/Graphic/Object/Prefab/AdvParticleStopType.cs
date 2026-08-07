// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{
	public enum AdvParticleStopType
	{
		Default,		//デフォルト操作のまま
		Clear,			//即座に消す。StopEmittingAndClearと同じ
		StopEmitting,	//新たな発生だけとめる。ループも切る
	}
}
