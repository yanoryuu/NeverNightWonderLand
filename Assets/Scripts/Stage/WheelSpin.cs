using UnityEngine;

/// <summary>
/// 見た目だけの回転 (背景の観覧車の輪など)。毎フレーム z 軸で _speed 度/秒 回す。
/// _upright に入れた子 (吊られたゴンドラ) は回転を打ち消して常に真っすぐ下に吊られたままにする。
/// 当たり・物理は持たない (足場には MovingPlatform を使う)。
/// </summary>
public class WheelSpin : MonoBehaviour
{
    [Tooltip("回転の速さ (度/秒、正 = 反時計回り)")]
    [SerializeField] private float _speed = 1f;

    [Tooltip("回転を打ち消して常に直立させる子 (ゴンドラ)")]
    [SerializeField] private Transform[] _upright;

    private void Update()
    {
        transform.Rotate(0f, 0f, _speed * Time.deltaTime);
        if (_upright == null)
            return;
        foreach (var t in _upright)
            if (t != null)
                t.rotation = Quaternion.identity;
    }
}
