using UnityEngine;

namespace Utage
{
    //文字列にタグを付ける処理のまとめ
    public static class StringTagUtil
    {
        //カラータグを追加
        public static string ColorTag(string str, Color color)
        {
            return ColorUtil.AddColorTag(str, color);
        }

        //ハイパーリンクタグ（a href）を追加
        //主に、UnityのDebug.LogでWebへのハイパーリンクを表示する際に使用
        public static string HyperLinkTag(string str, string url)
        {
            return $@"<a href=""{url}"">{str}</a>";
        }
        public static string HyperLinkTag(string url)
        {
            return HyperLinkTag(url, url);
        }

        //プロジェクト内のファイルリンクタグを追加
        //主に、UnityのDebug.Logでコードジャンプ可能なリンクをファイルパスを表示する際に使用
        public static string CodeJumpTag(string str, string filePah, int line)
        {
            return $@"<a href=""{filePah}"" line=""{line}"">{str}</a>";
        }
        public static string CodeJumpTag(string filePah, int line)
        {
            string str = $"{filePah}:{line}";
            return CodeJumpTag(str,filePah,line);
        }
    }
}
