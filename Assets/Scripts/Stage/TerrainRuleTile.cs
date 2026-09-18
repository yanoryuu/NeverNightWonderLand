using UnityEngine;
using UnityEngine.Tilemaps;

namespace NeverNight.Stage
{
    /// <summary>
    /// 地形用の RuleTile。隣接判定を「同じ RuleTile か」ではなく「何かタイルがあるか」で行う。
    /// 坂 (ascent/descent) や透明マーカーなど、別アセットのタイルも実体として扱えるので、
    /// 坂の脇に端キャップや壁面が出ない。
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainRuleTile", menuName = "NeverNight/Terrain Rule Tile")]
    public class TerrainRuleTile : RuleTile
    {
        public override bool RuleMatch(int neighbor, TileBase other)
        {
            switch (neighbor)
            {
                case TilingRuleOutput.Neighbor.This: return other != null;
                case TilingRuleOutput.Neighbor.NotThis: return other == null;
            }
            return base.RuleMatch(neighbor, other);
        }
    }
}
