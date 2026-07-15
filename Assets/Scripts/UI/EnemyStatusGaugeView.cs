using R3;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敵頭上のステータス表示 (World Space Canvas)。
/// 防御値ゲージ(白)・HPゲージ(赤)・ブレイク中の「BREAK!」表示を敵の状態に追従させる。
/// 敵 Prefab の子として配置され、敵の破棄と共に消える。
/// </summary>
public class EnemyStatusGaugeView : MonoBehaviour
{
    [Tooltip("参照する敵 (通常は親)")]
    [SerializeField] private EnemyController _enemy;

    [Tooltip("防御値(白)のフィル画像")]
    [SerializeField] private Image _guardFill;

    [Tooltip("HP(赤)のフィル画像")]
    [SerializeField] private Image _hpFill;

    [Tooltip("ブレイク中に表示するオブジェクト (BREAK! ラベル)")]
    [SerializeField] private GameObject _breakLabel;

    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        if (_enemy == null)
            _enemy = GetComponentInParent<EnemyController>();

        if (_enemy == null)
        {
            Debug.LogError($"[{nameof(EnemyStatusGaugeView)}] EnemyController が見つかりません。", this);
            return;
        }

        var consts = _enemy.Consts;

        if (_hpFill != null)
            _enemy.Hp.Subscribe(hp =>
                _hpFill.fillAmount = Mathf.Clamp01((float)hp / consts.MaxHp)).AddTo(_disposables);

        if (_guardFill != null)
            _enemy.Guard.Subscribe(guard =>
                _guardFill.fillAmount = Mathf.Clamp01((float)guard / consts.MaxGuard)).AddTo(_disposables);

        if (_breakLabel != null)
            _enemy.IsBroken.Subscribe(broken => _breakLabel.SetActive(broken)).AddTo(_disposables);
    }

    private void LateUpdate()
    {
        // 敵スプライトの反転 (flipX) には影響されないが、親スケールの符号反転には備えて正向きを維持する
        var scale = transform.lossyScale;
        if (scale.x < 0f)
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
