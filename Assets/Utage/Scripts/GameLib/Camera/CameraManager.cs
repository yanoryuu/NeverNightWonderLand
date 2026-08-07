using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UtageExtensions;
using System;

namespace Utage
{
	[AddComponentMenu("Utage/Lib/Camera/CameraManager")]
	public class CameraManager : MonoBehaviour, IBinaryIO
	{
		public string SaveKey { get { return "CameraManager"; } }

		public List<CameraRoot> CameraList
		{
			get 
			{					
				if (cameraList == null) {
					cameraList = new List<CameraRoot> (this.GetComponentsInChildren<CameraRoot> (true));
				}
				return cameraList;
			}
		}
		[NonSerialized]
		List<CameraRoot> cameraList = null;

		//カメラのキャプチャ処理制御（RenderPipeLineによってコンポーネントが違う）
		public ICameraCaptureManager CaptureManager
		{
			get
			{
				if (captureManager == null)
				{
					//Addされてるコンポーネントを取得
					captureManager = this.GetComponent<ICameraCaptureManager>();
					if (captureManager == null)
					{
						//なかったらビルトインのコンポーネントを作成
						captureManager = this.gameObject.AddComponent<CameraCaptureManagerBuiltin>();
					}
				}
				return captureManager;
			}
		}
		ICameraCaptureManager captureManager;

		public CameraRoot FindCameraRoot(string name)
		{
			return CameraList.Find(x=>x.name == name);
		}


		public Camera FindCameraByLayer(int layer)
		{
			int layerMask = 1 << layer;
			foreach (var item in CameraList )
			{
				Camera camera = item.LetterBoxCamera.CachedCamera;
				if ( (camera.cullingMask & layerMask) != 0 )
				{
					return camera;
				}
			}
			return null;
		}

		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		public void OnWrite(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(CameraList.Count);
			foreach (var camera in CameraList)
			{
				writer.Write(camera.name);
				writer.WriteBuffer(camera.Write);
			}
		}

		//セーブデータ用のバイナリ読み込み
		public void OnRead(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}

			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				string name = reader.ReadString();
				CameraRoot camera = FindCameraRoot(name);
				if (camera!=null)
				{
					reader.ReadBuffer(camera.Read);
				}
				else
				{
					//セーブされていたが、消えているので読み込まない
					reader.SkipBuffer();
				}
			}
		}

        internal void OnClear()
        {
			foreach (var item in CameraList)
			{
                item.OnClear();
			}
		}
	}
}
