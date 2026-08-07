// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UTAGE_URP_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Linq;
using UnityEngine.Rendering;
using Utage;
using UtageExtensions;
using Object = UnityEngine.Object;

namespace Utage.RenderPipeline.Urp
{
    //現在のシーンを、URP用に変換する
    public class UrpSceneConverter
    {
        //カメラに設定するRendererのインデックス
        public int RendererIndex { get; set; }
        //スタックカメラのBaseの背後を黒で埋める
        public bool BaseCameraClearBackGroundColor { get; set; } = true;
        //スクリーン解像度をフレキシブルにする（レターボックスを無効化して全画面表示）
        public bool FlexibleScreenSize { get; set; }
        
        const string PrefabGuid = "ed910f02e461d774b8717a741fc15cc8";
        public static bool CheckTemplateAssets()
        {
            return AssetDataBaseEx.IsExistAssetByGuid(PrefabGuid);

        }

        public bool SetUp()
        {
            var cameraManager = SceneManagerEx.GetComponentInActiveScene<CameraManager>(true);
            return SetUp(cameraManager);
        }
        
        public bool SetUp(CameraManager cameraManager)
        {
            if (cameraManager == null)
            {
                //現在のシーンにCameraManagerがない
                Debug.LogError(ToErrorString("Not found CameraManager in Active Scene"));
                return false;
            }

            //GUID指定で、もとになるプレハブをロード
            GameObject prefabAsset = AssetDataBaseEx.LoadAssetByGuid<GameObject>(PrefabGuid);
            if (prefabAsset == null)
            {
                Debug.LogError(ToErrorString("Not found VolumePrefab"));
                return false;
            }

            bool result = true;
            // アンドゥ操作をまとめるためのグループ
            using (new EditorUndoGroupScope("SetupCamerasForUrp"))
            {
                //カメラのセットアップ
                cameraManager.GetComponentCreateIfMissing<CameraCaptureManagerUrp>();
                
                //背景クリア用のカメラのセットアップ
                var clearCamera = cameraManager.GetComponentInChildren<UguiBackgroundRaycaster>(true);
                if (clearCamera!=null)
                {
                    SetupClearCamera(clearCamera.eventCamera);
                }
                
                //ADV用のカメラのセットアップ
                var cameras = cameraManager.GetComponentsInChildren<LetterBoxCamera>(true);
                cameras = cameras.OrderBy(x => x.CachedCamera.depth).ToArray();
                if (FlexibleScreenSize)
                {
                    foreach (var letterBoxCamera in cameras)
                    {
                        letterBoxCamera.IsFlexible = true;
                        letterBoxCamera.MaxWidth = 100000;
                        letterBoxCamera.MaxHeight = 100000;
                    }
                }
                for (var i = 0; i < cameras.Length; i++)
                {
                    var camera = cameras[i]; 
                    //カメラURP用に設定
                    SetupCamera(camera.CachedCamera,cameras[0].CachedCamera);
                    //ボリューム設定のオブジェクトをプレハブから作成
                    result &= TryCreatePostEffectVolumes(camera,prefabAsset);
                }
                
                var postEffectManager = SceneManagerEx.GetComponentInActiveScene<AdvPostEffectManager>(true);
                if (postEffectManager)
                {
                    var builtin = postEffectManager.GetComponent<AdvPostEffectRenderPipelineUsingBuiltin>();
                    if (builtin != null)
                    {
                        UnityEngine.Object.DestroyImmediate(builtin);
                    }
                    var urp = postEffectManager.GetComponentCreateIfMissing<AdvPostEffectRenderPipelineUsingUniversal>();
                }
            }
            return result;
        }
        
        //VolumeProfileのアセットを差し替える
        public void SwapVolumeProfileAssets(Dictionary<Object,Object> swapAssetsDictionary)
        {
            foreach (var advCameraPostEffectManager in SceneManagerEx.GetComponentsInActiveScene<AdvCameraPostEffectManager>(true))
            {
                foreach (var volume in advCameraPostEffectManager.GetComponentsInChildren<Volume>(true))
                {
                    var oldAsset = volume.sharedProfile;
                    if (swapAssetsDictionary.TryGetValue(oldAsset, out Object asset))
                    {
                        Debug.Log("Swap VolumeProfile Asset", volume);
                        volume.sharedProfile = (VolumeProfile)asset;
                    }
                    else 
                    {
                        if (!swapAssetsDictionary.Values.Contains(oldAsset))
                        {
                            //差し替えるアセットが見つからず、かつ差し替えるアセットにも含まれていない
                            Debug.LogError(ToErrorString("Not found VolumeProfile in SwapAssetsDictionary"), oldAsset);
                        }
                    }
                }
            }
        }

        void SetupClearCamera(Camera camera)
        {
            var urpCamera = camera.GetUniversalAdditionalCameraData();
            //PostProcessingの設定（一応）
            urpCamera.renderPostProcessing = true;
            //VolumeLayerMaskの設定(Noneに設定)
            urpCamera.volumeLayerMask = 0;
        }

        void SetupCamera(Camera camera, Camera baseCamera)
        {
            var urpCamera = camera.GetUniversalAdditionalCameraData();
            //rendererの設定
            if (RendererIndex >= 0)
            {
                urpCamera.SetRenderer(RendererIndex);
            }

            //PostProcessingの設定
            urpCamera.renderPostProcessing = true;
            //VolumeLayerMaskの設定
            urpCamera.volumeLayerMask = camera.cullingMask;

            if (camera == baseCamera)
            {
                if (BaseCameraClearBackGroundColor)
                {
                    urpCamera.renderType = CameraRenderType.Base;
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = Color.black;
                }
            }
            else
            {
                //カメラスタックとして追加
                urpCamera.renderType = CameraRenderType.Overlay;
                var baseCameraData = baseCamera.GetUniversalAdditionalCameraData();
                if (!baseCameraData.cameraStack.Contains(camera))
                {
                    baseCameraData.cameraStack.Add(camera);
                }
            }
        }
        
        //ボストエフェクト用のボリューム設定のオブジェクトをプレハブから作成
        bool TryCreatePostEffectVolumes(LetterBoxCamera camera, GameObject prefabAsset)
        {
            if( camera.GetComponentInChildren<AdvCameraPostEffectManager>() !=null)
            {
                //すでに作成済み
                Debug.LogWarning(ToErrorString("Already Setup PostEffectVolumes"), camera);
                return false;
            }
            
            
            //プレハブからインスタンスを作成
            var go = Object.Instantiate(prefabAsset, camera.transform);
            go.name = "Volumes";
            HierarchyWindowContextMenu.InitPrefabInstance(go);
            Undo.RegisterCreatedObjectUndo(go, "Create PostEffect Volumes");

            //レイヤー変更
            LayerMask layerMask = camera.CachedCamera.cullingMask;
            var layerNumbers = layerMask.GetLayerNumbers().ToArray();
            if (layerNumbers.Length <= 0)
            {
                //cullingMaskにレイヤーが一つも設定されていないのでエラーを出す
                Debug.LogError(ToErrorString("Not found Layer in CullingMask"), camera);
            }
            else
            {

                int layer = layerNumbers[^1];
                go.ChangeLayerDeep(layer);
                Debug.Log("CreatePostEffectVolumes", go);
            }
            return true;
        }
        
        string ToErrorString(string msg)
        {
            string url = @"https://madnesslabo.net/utage/?page_id=14418";
            return $"{msg}\n{StringTagUtil.HyperLinkTag(url)}";
        }
    }
}
#endif
