using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 「どのキャラのどのアニメで、どのエフェクトを、どこに、いつ出すか」の設定。
/// アニメ・エフェクト調整シーンで編集し、そのままアセットに残る (再生を止めても消えない)。
/// オフセットはキャラの足元を原点にした右向き基準の値 (左を向くと X が反転する)。
/// </summary>
[CreateAssetMenu(menuName = "NeverNight/Anim Effect Cue Set", fileName = "AnimEffectCueSet")]
public class AnimEffectCueSet : ScriptableObject
{
    [Serializable]
    public class Cue
    {
        [Tooltip("AnimTuningCatalog のエフェクト Id")]
        public string EffectId = "";
        [Tooltip("キャラの足元からのずれ (右向き基準)")]
        public Vector2 Offset;
        [Tooltip("アニメ開始から何秒後に出すか")]
        public float Delay;
        [Tooltip("大きさ")]
        public float Scale = 1f;
        [Tooltip("回転 (度)")]
        public float Rotation;
    }

    [Serializable]
    public class AnimationCues
    {
        public string CharacterId = "";
        public string Animation = "";
        public List<Cue> Cues = new();
    }

    public List<AnimationCues> Entries = new();

    /// <summary>該当するアニメの設定を返す。無ければ null (create=true なら作る)。</summary>
    public AnimationCues Find(string characterId, string animation, bool create = false)
    {
        var found = Entries.Find(e => e.CharacterId == characterId && e.Animation == animation);
        if (found == null && create)
        {
            found = new AnimationCues { CharacterId = characterId, Animation = animation };
            Entries.Add(found);
        }
        return found;
    }
}
