// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utage;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;

namespace Utage
{

    /// <summary>
    /// CGギャラリーの各ボタンのUIのサンプル(TextMeshPro版)
    /// </summary>
    [AddComponentMenu("Utage/TemplateUI/UtageUguiCgGalleryItemTMP")]
    public class UtageUguiCgGalleryItemTMP : UtageUguiCgGalleryItem
        , IUsingTextMeshPro
    {
    }
}
