using UnityEngine;

/// <summary>
/// プレイヤーがインタラクト(E キー)できるオブジェクトが実装するインターフェース。
/// <see cref="PlayerInteractor"/> が近傍から検出し、プロンプト表示と実行を行う。
/// </summary>
public interface IInteractable
{
    /// <summary>プロンプトに表示するテキスト (例: "レバーを引く")。ボタン表記はプロンプト側が付与する。</summary>
    string PromptText { get; }

    /// <summary>現在インタラクト可能か (作動済みのレバー等は false)。</summary>
    bool CanInteract { get; }

    /// <summary>プロンプトを表示するワールド座標のアンカー。</summary>
    Vector3 PromptAnchor { get; }

    /// <summary>インタラクトを実行する。</summary>
    void Interact(GameObject interactor);
}
