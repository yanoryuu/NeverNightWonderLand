// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Utage;
using System;
using TMPro;

namespace Utage
{

    /// <summary>
    /// シーン回想用のUIのサンプル(TextMeshPro版)
    /// </summary>
    [AddComponentMenu("Utage/TemplateUI/UtageUguiSceneGalleryItemTMP")]
    public class UtageUguiSceneGalleryItemTMP : UtageUguiSceneGalleryItem
        , IUsingTextMeshPro
    {
    }
}
