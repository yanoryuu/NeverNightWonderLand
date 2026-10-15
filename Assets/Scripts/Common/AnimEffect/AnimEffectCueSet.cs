using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 「どのキャラのどのアニメで、どのエフェクトを、どこに、いつ出すか」の設定と、使うエフェクトの一覧。
/// アニメ・エフェクト調整シーン (NeverNight/Anim &amp; Effect Tuner) で編集し、ゲームでは
/// <see cref="AnimEffectEmitter"/> が読んで同じ位置・タイミングでエフェクトを出す。
/// オフセットはキャラの原点 (プレハブのルート) からの右向き基準の値 (左を向くと X が反転する)。
/// アニメ名は左右の向き違い (Spine の "L/名前_L" と "R/名前_R") を 1 つにまとめた名前で持つ (<see cref="NormalizeAnimation"/>)。
/// </summary>
[CreateAssetMenu(menuName = "NeverNight/Anim Effect Cue Set", fileName = "AnimEffectCueSet")]
public class AnimEffectCueSet : ScriptableObject
{
    [Serializable]
    public class Effect
    {
        [Tooltip("一覧に出す名前。エフェクト設定の紐付けに使うので、後から変えない")]
        public string Id = "effect";
        [Tooltip("パーティクル・Spine・Animator 入りのプレハブ")]
        public GameObject Prefab;
        [Tooltip("プレハブの代わりに連番の絵で出す時に入れる (1 回だけ流す)")]
        public Sprite[] Frames = new Sprite[0];
        [Tooltip("連番の絵の 1 秒あたりのコマ数")]
        public float Fps = 12f;
        [Tooltip("連番の絵の描画順 (キャラより手前に出すなら大きく)")]
        public int SortingOrder = 50;
    }

    [Serializable]
    public class Cue
    {
        [Tooltip("Effects の Id")]
        public string EffectId = "";
        [Tooltip("キャラの原点からのずれ (右向き基準)")]
        public Vector2 Offset;
        [Tooltip("アニメ開始から何秒後に出すか (アニメの再生位置で数える)")]
        public float Delay;
        [Tooltip("大きさ")]
        public float Scale = 1f;
        [Tooltip("回転 (度)")]
        public float Rotation;
        [Tooltip("ON: 出した後もキャラに付いて動く / OFF: 出した場所に残る")]
        public bool FollowCharacter;
    }

    [Serializable]
    public class AnimationCues
    {
        public string CharacterId = "";
        public string Animation = "";
        public List<Cue> Cues = new();
    }

    public List<Effect> Effects = new();
    public List<AnimationCues> Entries = new();

    public Effect FindEffect(string id) => Effects.Find(e => e.Id == id);

    /// <summary>該当するアニメの設定を返す。無ければ null (create=true なら作る)。animation は正規化前の名前でもよい。</summary>
    public AnimationCues Find(string characterId, string animation, bool create = false)
    {
        animation = NormalizeAnimation(animation);
        var found = Entries.Find(e => e.CharacterId == characterId && e.Animation == animation);
        if (found == null && create)
        {
            found = new AnimationCues { CharacterId = characterId, Animation = animation };
            Entries.Add(found);
        }
        return found;
    }

    /// <summary>
    /// 左右の向き違いを 1 つにまとめたアニメ名にする。
    /// "L/guramine_jump_L" と "R/guramine_jump_R" はどちらも "guramine_jump" になる (片方で調整すればもう片方にも効く)。
    /// </summary>
    public static string NormalizeAnimation(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "";
        if (name.Length > 2 && (name.StartsWith("L/", StringComparison.Ordinal) || name.StartsWith("R/", StringComparison.Ordinal)))
            name = name.Substring(2);
        if (name.Length > 2 && (name.EndsWith("_L", StringComparison.Ordinal) || name.EndsWith("_R", StringComparison.Ordinal)))
            name = name.Substring(0, name.Length - 2);
        return name;
    }
}
