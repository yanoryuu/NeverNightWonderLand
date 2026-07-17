using UnityEngine;

/// <summary>
/// 攻撃方法の定義 (抽象 ScriptableObject)。アイテム (ItemDefinition) と同じパターンで、
/// 表示名・説明・パラメータを攻撃自身が持ち、新しい攻撃方法は
/// このクラスを継承したクラス+アセットを追加するだけで増やせる。
/// 装備枠は □=近接 (MeleeAttackDefinition) と △=特殊 (SpecialAttackDefinition) の2つで、
/// セーブポイントで解放済みの攻撃方法と入れ替えられる。
/// </summary>
public abstract class AttackDefinition : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("表示名")]
    [SerializeField] private string _displayName = "攻撃";

    [Tooltip("説明 (入れ替えメニュー表示用)")]
    [SerializeField, TextArea] private string _description = "";

    [Tooltip("アイコンの色 (仮素材)")]
    [SerializeField] private Color _iconColor = Color.white;

    public string DisplayName => _displayName;
    public string Description => _description;
    public Color IconColor => _iconColor;
}
