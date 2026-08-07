// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
	[System.Flags]
	public enum AssetBundleTargetFlags
	{
		Android = 0x1 << 0,
		iOS = 0x1 << 1,
		WebGL = 0x1 << 2,
		Windows = 0x1 << 3,
		OSX = 0x1 << 4,
		// 他のプラットフォームは必要に応じて追加
	};

	//アセットバンドルのヘルパー
	public class AssetBundleHelper
	{
		public static AssetBundleTargetFlags RuntimeAssetBundleTarget()
		{
#if UNITY_EDITOR
			return EditorAssetBundleTarget();
#else
			return RuntimePlatformToBuildTargetFlag(Application.platform);
#endif
		}

#if UNITY_EDITOR
		public static AssetBundleTargetFlags EditorAssetBundleTarget()
		{
			switch (Application.platform)
			{
				case RuntimePlatform.WindowsEditor:
					return AssetBundleTargetFlags.Windows;
				case RuntimePlatform.OSXEditor:
					return AssetBundleTargetFlags.OSX;
				default:
					Debug.Log("Not support");
					return AssetBundleTargetFlags.Windows;
			}
		}
#endif

		//ランタイムのプラットフォームを、ターゲットフラグにに変換
		public static AssetBundleTargetFlags RuntimePlatformToBuildTargetFlag(RuntimePlatform platform)
		{
			switch (platform)
			{
				case RuntimePlatform.Android:
					return AssetBundleTargetFlags.Android;
				case RuntimePlatform.IPhonePlayer:
					return AssetBundleTargetFlags.iOS;
				case RuntimePlatform.WebGLPlayer:
					return AssetBundleTargetFlags.WebGL;
				case RuntimePlatform.WindowsPlayer:
					return AssetBundleTargetFlags.Windows;

				case RuntimePlatform.OSXPlayer:
					return AssetBundleTargetFlags.OSX;
				default:
					Debug.LogError("Not support " + platform.ToString());
					return 0;
			}
		}
#if UNITY_EDITOR
		//ターゲットフラグスを、ビルドターゲットのリストに変換
		public static List<BuildTarget> BuildTargetFlagsToBuildTargetList(AssetBundleTargetFlags flags)
		{
			List<BuildTarget> list = new List<BuildTarget>();
			foreach (AssetBundleTargetFlags flag in Enum.GetValues(typeof(AssetBundleTargetFlags)))
			{
				if ((flags & flag) == flag)
				{
					list.Add(BuildTargetFlagToBuildTarget(flag));
				}
			}
			return list;
		}

		//ターゲットフラグを、ビルドターゲットに変換
		public static BuildTarget BuildTargetFlagToBuildTarget(AssetBundleTargetFlags flag)
		{
			switch (flag)
			{
				case AssetBundleTargetFlags.Android:
					return BuildTarget.Android;
				case AssetBundleTargetFlags.iOS:
					return BuildTarget.iOS;
				case AssetBundleTargetFlags.WebGL:
					return BuildTarget.WebGL;
				case AssetBundleTargetFlags.Windows:
					return BuildTarget.StandaloneWindows64;
				case AssetBundleTargetFlags.OSX:
#if UNITY_2017_3_OR_NEWER
					return BuildTarget.StandaloneOSX;
#else
					return BuildTarget.StandaloneOSXUniversal;
#endif
				default:
					Debug.LogError("Not support " + flag.ToString());
					return 0;
			}
		}

		//ビルドターゲットを、ターゲットフラグに変換
		public static AssetBundleTargetFlags BuildTargetToBuildTargetFlag(BuildTarget target)
		{
			switch (target)
			{
				case BuildTarget.Android:
					return AssetBundleTargetFlags.Android;
				case BuildTarget.iOS:
					return AssetBundleTargetFlags.iOS;
				case BuildTarget.WebGL:
					return AssetBundleTargetFlags.WebGL;
				case BuildTarget.StandaloneWindows:
				case BuildTarget.StandaloneWindows64:
					return AssetBundleTargetFlags.Windows;

#if UNITY_2017_3_OR_NEWER
				case BuildTarget.StandaloneOSX:
					return AssetBundleTargetFlags.OSX;
#else
				case BuildTarget.StandaloneOSXIntel:
				case BuildTarget.StandaloneOSXIntel64:
				case BuildTarget.StandaloneOSXUniversal:
					return AssetBundleTargetFlags.OSX;
#endif
				default:
					return 0;
			}
		}
#endif
	}
}