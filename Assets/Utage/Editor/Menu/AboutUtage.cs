// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	public class AboutUtage: EditorWindow
	{
		const string URL = "https://madnesslabo.net/utage/";
		const string DocumentUrl = "https://madnesslabo.net/utage/?page_id=225";
		const string Version4FeaturesUrl = "https://madnesslabo.net/utage/?page_id=12335";

		void OnGUI()
		{
			GUILayout.Label("Utage version " + VersionUtil.Version);

			GUILayout.Space(10);
			GUIStyle style = GUI.skin.label;
			style.richText = true;
			if (GUILayout.Button( ColorUtil.AddColorTag("WebSite",Color.blue), style ))
			{
				Application.OpenURL(URL);
			}
			GUILayout.Space(8);
			if (GUILayout.Button(ColorUtil.AddColorTag("Document", Color.blue), style))
			{
				Application.OpenURL(DocumentUrl);
			}

			GUILayout.Space(8);
			if (GUILayout.Button(ColorUtil.AddColorTag("Version4 Features", Color.blue), style))
			{
				Application.OpenURL(Version4FeaturesUrl);
			}
		}
	}
}
