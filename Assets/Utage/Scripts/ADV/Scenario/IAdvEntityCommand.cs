// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;
using UtageExtensions;


namespace Utage
{
	public interface IAdvInitOnCreateEntity
	{
		void InitFromPageData(AdvScenarioPageData page);
		void InitOnCreateEntity(AdvCommand command);
	}
}
