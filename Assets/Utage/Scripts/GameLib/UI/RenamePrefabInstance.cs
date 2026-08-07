using System;
using System.Linq;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //プレハブインスタンスの名前を変更する
    //主に、プレハブに連番を付けたいときに使用
    public class RenamePrefabInstance : MonoBehaviour
    {
        //リネームする際のフォーマット{0}はプレハブの名前、{1}はインデックス
        public string renameFormat = "{0}({1})";
        
        //プレハブインスタンスのクローン接尾辞を削除するかどうか
        public bool stripCloneSuffix = true;
        
        //プレハブインスタンスのクローン接尾辞
        public string cloneSuffix = "(Clone)";

        //最初のインデックス（インデックスの最初の番号をずらすときに設定）
        public int startIndex = 0;

        //リネーム済みかどうか
        bool Renamed { get; set; }

        void Awake()
        {
            Rename();
        }

        void Start()
        {
            Rename();
        }

        public void Rename()
        {
            if(Renamed) return;
            int index = GetIndex();
            if (index < 0)
            {
                return;
            }
            var renameString = string.Format(renameFormat, this.gameObject.name, index);
            if (stripCloneSuffix)
            {
                renameString = renameString.Replace(cloneSuffix, string.Empty);
            }
            this.gameObject.name = renameString; 
            Renamed = true;
        }

        int GetIndex()
        {
            bool isFind = false;
            int index = startIndex;
            if(this.transform.parent == null)
            {
                //親がいない場合は、インデックスは-1
                return -1;
            }
            foreach (Transform child in this.transform.parent)
            {
                if (child == this.transform)
                {
                    isFind = true;
                    break;
                }
                ++index; 
            }

            if (!isFind)
            {
                index = -1;
            }
            return index;
        }
    }
}