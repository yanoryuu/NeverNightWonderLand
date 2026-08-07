using UnityEngine;
using System.Collections;

namespace Utage
{
	[AddComponentMenu("Utage/ADV/Examples/SampleTips")]
	public class SampleTips : MonoBehaviour
	{

		public void OnClickTips(UguiNovelTextHitArea hit)
		{
			Debug.Log(hit.Arg);
		}
	}
}
