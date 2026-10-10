using UnityEngine;

/// <summary>ミニマップの Passive View 抽象 (<see cref="MinimapPresenter"/> が参照する)。</summary>
public interface IMinimapView
{
    /// <summary>地図の絵 (全体マップと同じ <see cref="MapData"/> から事前配置したもの)。</summary>
    MapDrawing Drawing { get; }

    /// <summary>拡大率 (画面 px / ワールド単位)。</summary>
    float Zoom { get; }

    /// <summary>地図の無いステージ (旧ステージなど) では隠す。</summary>
    void SetMapVisible(bool visible);
}

/// <summary>
/// HUD のミニマップ (PlayerUI の HUD プレハブ、右上)。全体マップと同じ <see cref="MapData"/> から作った部屋のブロックを
/// 事前配置してあり (Docs/UI/Build/build_map.cs の BuildMap.Minimap)、表示だけを担う。
/// プレイヤーを中心に表示する処理・どの部屋を見せるかは <see cref="MinimapPresenter"/> が行う
/// (2026-10-10 Issue #5 Q5: カメラ + RenderTexture 方式から置き換え)。
/// </summary>
public class MinimapView : MonoBehaviour, IMinimapView
{
    [Tooltip("地図の絵 (マスクの中の Content)")]
    [SerializeField] private MapDrawing _drawing;

    [Tooltip("地図の無いステージで隠すもの (地図の絵と現在地)")]
    [SerializeField] private GameObject _mapRoot;

    [Tooltip("拡大率 (画面 px / ワールド単位)")]
    [SerializeField] private float _zoom = 2f;

    public MapDrawing Drawing => _drawing;
    public float Zoom => _zoom;

    public void SetMapVisible(bool visible)
    {
        if (_mapRoot != null && _mapRoot.activeSelf != visible)
            _mapRoot.SetActive(visible);
    }
}
