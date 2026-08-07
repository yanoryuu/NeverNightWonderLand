// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Utage
{

	// 複数のコマンドで使う可能性のある、キーワード
	public static class AdvCommandKeyword
	{
		//全てのレイヤーと、それ以下のオブジェクトが対象
		public const string All = "All";

		//Bgタイプの全レイヤー以下の全オブジェクトが対象
		public const string AllBgLayers = "AllBgLayers";
		
		//Characterタイプの全レイヤー以下の全オブジェクトが対象
		public const string AllCharacterLayers = "AllCharacterLayers";
		
		//Spriteタイプの全レイヤー以下の全オブジェクトが対象
		public const string AllSpriteLayers = "AllSpriteLayers"; 

		//すべてのBgタイプのオブジェクトが対象
		public const string AllBgObjects = "AllBgObjects"; 
		
		//すべてのCharacterタイプのオブジェクトが対象
		public const string AllCharacterObjects = "AllCharacterObjects"; 
		
		//すべてのSpriteタイプのオブジェクトが対象
		public const string AllSpriteObjects = "AllSpriteObjects"; 

		//GraphicManager以下のオブジェクト（レイヤーやBg、キャラクターを含む）
//		public const string AllGraphicObjects = "AllGraphicObjects"; 
		
		
		//ピボットなどの指定のキーワード
		public const string Left = "Left";
		public const string Right = "Right";
		public const string Top = "Top";
		public const string Bottom = "Bottom";
		public const string Center = "Center";
	}
}
