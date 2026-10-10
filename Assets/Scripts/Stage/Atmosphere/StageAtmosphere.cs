using System;
using UnityEngine;

namespace NeverNight.Stage
{
    /// <summary>ステージの雰囲気の種類 (ポストプロセスの色味と環境パーティクルの組を選ぶ)。</summary>
    public enum AtmosphereKind
    {
        /// <summary>指定なし (雰囲気の演出を切る。旧ステージなど)。</summary>
        None = 0,
        /// <summary>地下: 冷たい青紫・コントラスト強め、漂う埃 + ときどき火の粉。</summary>
        Underground = 1,
        /// <summary>地上の夜: 紫〜紺、灯や星がにじむ、瞬く光の粒。</summary>
        Outdoor = 2,
        /// <summary>ボス部屋: 少し赤み・ビネット強め、舞う火の粉。</summary>
        Boss = 3,
        /// <summary>拠点: 暖かい色味、ゆっくり漂う暖色の粒。</summary>
        Haven = 4,
    }

    /// <summary>
    /// ステージ側に置く「このステージの雰囲気の種類」の指定 (データだけ)。
    /// 見た目 (Volume・環境パーティクル) は PlayerScene のカメラの子の <see cref="AtmosphereDirector"/> が持っていて、
    /// <see cref="Current"/> を見て切り替える。ビルダーの Area1StageBuilder.Atmosphere(sceneId) が置く。
    /// 遷移中は新しいステージが先に有効になるので、StageInfo と同じく「最後に有効になったもの」を現在とする。
    /// </summary>
    public class StageAtmosphere : MonoBehaviour
    {
        [Tooltip("このステージの雰囲気 (ポストプロセスの色味と環境パーティクルの組)")]
        [SerializeField] private AtmosphereKind _kind = AtmosphereKind.Underground;

        /// <summary>ロード済みの (最後に入った) ステージの指定。無ければ null。</summary>
        public static StageAtmosphere Current { get; private set; }

        /// <summary>現在の指定が変わった時に呼ばれる (null = 指定なしになった)。</summary>
        public static event Action<StageAtmosphere> Changed;

        public AtmosphereKind Kind => _kind;

        /// <summary>現在のステージの雰囲気 (指定が無ければ None)。</summary>
        public static AtmosphereKind CurrentKind => Current != null ? Current._kind : AtmosphereKind.None;

        private void OnEnable()
        {
            Current = this;
            Changed?.Invoke(this);
        }

        private void OnDisable()
        {
            if (Current != this)
                return;
            Current = null;
            Changed?.Invoke(null);
        }
    }
}
