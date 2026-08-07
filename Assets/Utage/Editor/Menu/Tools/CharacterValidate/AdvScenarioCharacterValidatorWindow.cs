// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Utage
{
	//シナリオの使用文字の検証ツールウィンドウ
	public class AdvScenarioCharacterValidatorWindow
		: EditorWindowWithSave
	{
		AdvScenarioDataProject Project => project;
		[SerializeField] AdvScenarioDataProject project;

		//想定外の文字が含まれていないか検証する際の設定
		public AdvScenarioCharacterValidatorSettings Settings => settings;
		[SerializeField, UnfoldedSerializable] AdvScenarioCharacterValidatorSettings settings = new();

#pragma warning disable 414
		[SerializeField, Button(nameof(Validate), nameof(DisableValidate), false)]
		string validate = "";
#pragma warning restore 414

		protected override string SaveKey => nameof(AdvScenarioCharacterValidatorWindow);

		bool DisableValidate()
		{
			return Project == null || Settings.DisableValidate();
		}
		
		void Validate()
		{
			
			//いったんプロジェクトのシナリオを全インポートして、シナリオデータを作成する
			AdvScenarioImporterInEditor importer = new(Project) { DisableTextValidate = true };
			importer.ImportAll();

			Debug.Log("Validate");
			//使用文字の検証
			Settings.CreateValidator().Validate(importer);
		}
	}
}
