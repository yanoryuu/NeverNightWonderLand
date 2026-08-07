// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.Pool;

namespace Utage
{
	/// <summary>
	/// アウトラインコンポーネントをリッチに
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiRichOutline")]
	public class UguiRichOutline : Outline
	{
		public int copyCount = 16;
#if UNITY_4_6 || UNITY_5_0 || UNITY_5_1
        public override void ModifyVertices(List<UIVertex> verts)
		{
			if (!IsActive())
				return;

            ModifyVerticesSub(verts);
        }
#elif UNITY_5_2_0
        public override void ModifyMesh(Mesh mesh)
        {
            if (!IsActive())
                return;

			var verts = Utage.ListPool<UIVertex>.Get();
            using (var helper = new VertexHelper(mesh))
            {
                helper.GetUIVertexStream(verts);
            }

            ModifyVerticesSub(verts);

            using (var helper2 = new VertexHelper())
            {
                helper2.AddUIVertexTriangleStream(verts);
                helper2.FillMesh(mesh);
            }
			Utage.ListPool<UIVertex>.Release(verts);
        }
#else
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

			var verts = ListPool<UIVertex>.Get();
			vh.GetUIVertexStream(verts);

            ModifyVerticesSub(verts);

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
			ListPool<UIVertex>.Release(verts);
        }
#endif

		void ModifyVerticesSub(List<UIVertex> verts)
        {
            var start = 0;
            var end = verts.Count;

            for (int i = 0; i < copyCount; ++i)
            {
                float x = Mathf.Sin(Mathf.PI * 2 * i / copyCount) * effectDistance.x;
                float y = Mathf.Cos(Mathf.PI * 2 * i / copyCount) * effectDistance.y;
                ApplyShadow(verts, effectColor, start, verts.Count, x, y);
                start = end;
                end = verts.Count;
            }
        }
    }
}
