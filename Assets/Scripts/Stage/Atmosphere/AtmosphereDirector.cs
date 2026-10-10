using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeverNight.Stage
{
    /// <summary>
    /// 画面全体の雰囲気 (ポストプロセス + カメラ周りの環境パーティクル) を、現在のステージの
    /// <see cref="StageAtmosphere"/> の種類に合わせて切り替える。PlayerScene の Main Camera の子 "Atmosphere" に置く。
    /// ・Base Volume (priority 0): 全種類共通 (Tonemapping・Bloom・弱いビネット・ごく弱いフィルムグレイン)。
    /// ・Mood Volume (priority 10): 種類ごとのプロファイルに差し替える (色味・コントラスト・ビネットの上書き)。
    /// ・環境パーティクル: 種類ごとの子 (ワールド空間シミュレーションの ParticleSystem 群) を 1 つだけ有効にする。
    ///   カメラの子なので発生範囲は常に画面の周り、粒はワールドに残るのでカメラが動くと流れて見える。
    /// ステージに指定が無い (旧ステージ・タイトル等) 時は両方の Volume の重みを 0 にしてパーティクルも止める。
    /// 組み立ては Area1StageBuilder.SetupPlayerAtmosphere (エディタ)。手順は Docs/Area1/SceneBuildGuide.md §15。
    /// </summary>
    public class AtmosphereDirector : MonoBehaviour
    {
        [Serializable]
        public class Preset
        {
            [Tooltip("この組を使う雰囲気の種類")]
            public AtmosphereKind Kind;

            [Tooltip("Mood Volume に差し替えるプロファイル")]
            public VolumeProfile Profile;

            [Tooltip("この種類の時だけ有効にする環境パーティクルの親 (子の ParticleSystem をまとめて再生)")]
            public GameObject Particles;
        }

        [Tooltip("全種類共通の Volume (priority 低)")]
        [SerializeField] private Volume _baseVolume;

        [Tooltip("種類ごとにプロファイルを差し替える Volume (priority 高)")]
        [SerializeField] private Volume _moodVolume;

        [SerializeField] private Preset[] _presets = Array.Empty<Preset>();

        [Tooltip("種類が変わった時に Volume の重みを 0→1 にする秒数 (実時間)")]
        [SerializeField] private float _fadeTime = 0.5f;

        [Tooltip("1 フレームでこれ以上カメラが動いたら (ワープ・リスポーン) パーティクルを作り直す")]
        [SerializeField] private float _jumpDistance = 12f;

        private AtmosphereKind _applied = (AtmosphereKind)(-1);
        private float _weight;
        private Vector3 _lastPosition;

        /// <summary>今効いている雰囲気の種類。</summary>
        public AtmosphereKind Applied => _applied;

        private void OnEnable()
        {
            _lastPosition = transform.position;
            Apply(StageAtmosphere.CurrentKind);
        }

        private void LateUpdate()
        {
            var kind = StageAtmosphere.CurrentKind;
            if (kind != _applied)
                Apply(kind);

            var target = kind == AtmosphereKind.None ? 0f : 1f;
            _weight = _fadeTime > 0f ? Mathf.MoveTowards(_weight, target, Time.unscaledDeltaTime / _fadeTime) : target;
            if (_baseVolume != null)
                _baseVolume.weight = target;
            if (_moodVolume != null)
                _moodVolume.weight = _weight;

            var pos = transform.position;
            if ((pos - _lastPosition).sqrMagnitude > _jumpDistance * _jumpDistance)
                RestartParticles(Find(_applied));
            _lastPosition = pos;
        }

        /// <summary>種類を即座に適用する (Mood のプロファイル差し替え・パーティクルの入れ替え)。</summary>
        private void Apply(AtmosphereKind kind)
        {
            _applied = kind;
            var preset = Find(kind);
            if (_moodVolume != null)
            {
                _moodVolume.sharedProfile = preset?.Profile;
                _moodVolume.enabled = preset?.Profile != null;
            }
            _weight = 0f;

            foreach (var p in _presets)
                if (p.Particles != null && p != preset)
                    p.Particles.SetActive(false);
            RestartParticles(preset);
        }

        /// <summary>種類のパーティクルを空にして最初から (prewarm 込みで) 再生する。</summary>
        private static void RestartParticles(Preset preset)
        {
            if (preset?.Particles == null)
                return;
            preset.Particles.SetActive(true);
            foreach (var ps in preset.Particles.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(false);
            }
        }

        private Preset Find(AtmosphereKind kind)
        {
            if (kind == AtmosphereKind.None)
                return null;
            foreach (var p in _presets)
                if (p.Kind == kind)
                    return p;
            return null;
        }
    }
}
