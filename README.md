## 命名規則 (Naming Conventions)

本プロジェクトでは、コードの可読性と一貫性を保つため、以下の命名規則を採用しています。

| 対象 | 命名スタイル | 例 | 備考 |
| :--- | :--- | :--- | :--- |
| **Private Field** | `_camelCase` | `_itemName` | 先頭にアンダースコアを付与 |
| **Public Field** | `PascalCase` | `ItemCount` | 外部公開変数は大文字開始 |
| **Property** | `PascalCase` | `IsActive` | ゲッター/セッターを含むもの |
| **Static Field** | `PascalCase` | `Instance` | クラス共通の変数 |
| **Method** | `PascalCase` | `UpdateInventory()` | 動詞から始める |
| **Local Variable** | `camelCase` | `index` | メソッド内などの一時変数 |

---
