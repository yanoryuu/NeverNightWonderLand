// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	interface IAdvSkipSpeed
	{
		//ループアニメーションなど、スキップ解除後に速度変化させる必要があるものに使用する
		void OnChangeSkipSpeed(float speed);
	}
}
