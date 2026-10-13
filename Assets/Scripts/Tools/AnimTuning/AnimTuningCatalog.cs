using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// アニメ・エフェクト調整シーン (AnimEffectTuningScene) で選べるキャラクターとエフェクトの一覧。
/// キャラクターは「プレハブ」か「Spine の骨格データ」のどちらかで登録する。
/// プレハブはシーンに出したあと、見た目以外のスクリプト・物理を外して人形として扱う
/// (Spine / Animator / SpriteFlipbook のアニメーションは自動で一覧に出る)。
/// コードでスプライトを差し替えている敵 (ポップコーン爆弾など) は、その絵を <see cref="SpriteClips"/> に連番で登録すると再生できる。
/// </summary>
[CreateAssetMenu(menuName = "NeverNight/Anim Tuning Catalog", fileName = "AnimTuningCatalog")]
public class AnimTuningCatalog : ScriptableObject
{
    [Serializable]
    public class SpriteClip
    {
        [Tooltip("アニメーション名 (一覧での表示とエフェクト設定の紐付けに使う)")]
        public string Name = "clip";
        public Sprite[] Frames = new Sprite[0];
        [Tooltip("1 秒あたりのコマ数")]
        public float Fps = 12f;
    }

    [Serializable]
    public class CharacterEntry
    {
        [Tooltip("一覧に出す名前。エフェクト設定 (AnimEffectCueSet) の紐付けにも使うので、後から変えない")]
        public string Id = "character";
        [Tooltip("敵などのプレハブ。Spine 骨格データと両方入れた時はプレハブを優先する")]
        public GameObject Prefab;
        [Tooltip("プレハブを使わず Spine の骨格データだけで出す時に入れる")]
        public SkeletonDataAsset Spine;
        [Tooltip("Spine のスキン名 (空欄なら既定)")]
        public string SpineSkin = "";
        [Tooltip("表示倍率")]
        public float Scale = 1f;
        [Tooltip("足元の位置合わせ (立ち位置からのずれ)")]
        public Vector2 Offset;
        [Tooltip("素の向きが左のキャラは ON (右向きを基準にそろえる)")]
        public bool FacesLeft;
        [Tooltip("コードで絵を差し替えているキャラ用の連番アニメ")]
        public List<SpriteClip> SpriteClips = new();
        [Tooltip("連番アニメを描く SpriteRenderer の子パス (空欄なら最初に見つかった SpriteRenderer)")]
        public string SpriteRendererPath = "";
    }

    [Serializable]
    public class EffectEntry
    {
        [Tooltip("一覧に出す名前。エフェクト設定の紐付けに使うので、後から変えない")]
        public string Id = "effect";
        [Tooltip("パーティクル・Spine・Animator・SpriteFlipbook 入りのプレハブ")]
        public GameObject Prefab;
        [Tooltip("プレハブの代わりに連番の絵で出す時に入れる")]
        public SpriteClip Frames = new();
        [Tooltip("連番の絵の描画順 (キャラより手前に出すなら大きく)")]
        public int SortingOrder = 50;
    }

    public List<CharacterEntry> Characters = new();
    public List<EffectEntry> Effects = new();

    public EffectEntry FindEffect(string id) => Effects.Find(e => e.Id == id);
}
