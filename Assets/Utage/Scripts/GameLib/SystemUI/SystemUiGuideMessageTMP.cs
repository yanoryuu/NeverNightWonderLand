using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;
using System;
using TMPro;

namespace Utage
{
    //ガイドメッセージ表示（画面手前に一定時間だけ表示されるメッセージ）
    public class SystemUiGuideMessageTMP : SystemUiGuideMessage
        , IUsingTextMeshPro
    {
    }
}
