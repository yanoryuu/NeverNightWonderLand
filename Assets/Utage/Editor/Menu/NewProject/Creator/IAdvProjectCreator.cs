using TMPro;
using UnityEditor;
using UnityEngine;

namespace Utage
{
    //プロジェクト作成時の設定の共通インターフェース
    public interface IAdvProjectCreator
    {
    }

    //プロジェクト作成時に既存シーンに追加する場合の
    public interface IAdvProjectCreatorAddScene : IAdvProjectCreator
    {
    }

    //プロジェクト作成時にアセットのみ作成する場合のインターフェース
    public interface IAdvProjectCreatorAssetOnly : IAdvProjectCreator
    {
    }

    //画面サイズの設定
    public interface IAdvProjectCreatorGameScreenSize : IAdvProjectCreator
    {
        int GameScreenWidth { get; }
        int GameScreenHeight { get; }
    }

    //レイヤー名の設定
    public interface IAdvProjectCreatorLayerNames : IAdvProjectCreator
    {
        public string LayerName { get; }

        public string DefaultLayerName { get; }

        public string LayerNameUI { get; }

        public string DefaultLayerNameUI { get; }
    }

    //暗号キーの設定
    public interface IAdvProjectCreatorSecurity : IAdvProjectCreator
    {
        string SecretKey { get; }
    }

    //プロジェクト作成時URPのグラフィック設定を行うインターフェース
    public interface IAdvProjectCreatorUrpGraphicSettings : IAdvProjectCreator
    {
        //現在のUnityがデフォルトで使用しているUniversalRenderPipelineAssetのVolumesをクリアするか
        bool AutoClearUrpVolumes   { get; }
    }

    public static class AdvProjectCreatorGuiExtensions
    {
        //プロジェクト作成可能かチェック
        public static bool EnableCreateDefault(this IAdvProjectCreator creator)
        {
            //レイヤー設定
            if (creator is IAdvProjectCreatorLayerNames layerNames)
            {
                if (!layerNames.EnableLayer()) return false;
            }

            //画面サイズ
            if (creator is IAdvProjectCreatorGameScreenSize gameScreenSize)
            {
                if (!gameScreenSize.EnableGameScreenSize()) return false;
            }

            //フォント(Legacy)
            if (creator is IAdvProjectCreatorFontLegacy fontLegacy)
            {
                if (!fontLegacy.EnableFont()) return false;
            }

            //フォント(Text Mesh Pro)
            if (creator is IAdvProjectCreatorFontTMP fontTMP)
            {
                if (!fontTMP.EnableFont()) return false;
            }

            //秘密キー
            if (creator is IAdvProjectCreatorSecurity security)
            {
                if (!security.EnableSecurity()) return false;
            }

            return true;
        }


        //レイヤー設定が有効かチェック
        public static bool EnableLayer(this IAdvProjectCreatorLayerNames layer)
        {
            if (string.IsNullOrEmpty(layer.LayerName)) return false;
            if (string.IsNullOrEmpty(layer.LayerNameUI)) return false;
            return true;
        }


        //画面サイズ設定が有効かチェック
        public static bool EnableGameScreenSize(this IAdvProjectCreatorGameScreenSize gameScreenSize)
        {
            if (gameScreenSize.GameScreenHeight <= 0) return false;
            if (gameScreenSize.GameScreenWidth <= 0) return false;
            return true;
        }


        //セキュリティ設定が有効かチェック
        public static bool EnableSecurity(this IAdvProjectCreatorSecurity security)
        {
            if (string.IsNullOrEmpty(security.SecretKey)) return false;
            return true;
        }
    }
}
