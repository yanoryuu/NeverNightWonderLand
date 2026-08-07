// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System.Collections;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utage
{
	public interface IAdvScenarioDataProject
	{
		IEnumerable<IAdvScenarioFileReader> CreateScenarioFileReaders(AdvScenarioFileReaderManager manager);
	}
}
