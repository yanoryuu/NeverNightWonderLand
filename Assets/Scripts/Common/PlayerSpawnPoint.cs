using UnityEngine;

/// <summary>
/// ステージ内のプレイヤー開始位置マーカー (入り口)。1ステージに複数配置できる。
/// StageLoader がステージロード後、遷移元の出口 (SceneTransitionZone) が指定した
/// 入り口 Id と一致するものへプレイヤーを移動する (セーブ復帰時はセーブ位置が優先)。
/// Id が空のものはデフォルト入口 (タイトル開始・リトライ・Id 不一致時のフォールバック)。
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("入り口の Id。出口 (SceneTransitionZone) の EntranceId と対応させる。空はデフォルト入口")]
    [SerializeField] private string _id = "";

    public string Id => _id;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.down * 0.5f, transform.position + Vector3.up * 0.5f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f,
            string.IsNullOrEmpty(_id) ? "Spawn (default)" : $"Spawn: {_id}");
#endif
    }
}
