using System.IO;
using UnityEngine;

namespace Utage
{
    //Tipsの情報
    public class TipsInfo
    {
        public string Id => Data.Id;
        public bool SystemSave => Data.SystemSave;
        public TipsData Data { get; }

        //TIPSが開放済みか（本文中に出現済み）
        public bool IsOpened { get; protected set; }

        //TIPSが既読み済みか（詳細を閲覧済みか）
        public bool HasRead { get; protected set; }

        public TipsInfo(TipsData data)
        {
            Data = data;
            Clear();
        }

        public virtual void Open()
        {
            IsOpened = true;
        }
        
        //既読済みにする
        public virtual void Read()
        {
            HasRead = true;
        }
        
        //未読に戻す（表示内容を変える処理をしているときなど）
        public virtual void UnRead()
        {
            HasRead = false;
        }

        const int Version = 0;

        public virtual void Clear()
        {
            IsOpened = Data.DefaultOpened;
            HasRead = Data.DefaultRead;
        }

        //書き込み
        public virtual void WriteSave(System.IO.BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(IsOpened);
            writer.Write(HasRead);
        }

        //読み込み
        public virtual void ReadSave(System.IO.BinaryReader reader)
        {
            //バージョンチェック
            int version = reader.ReadInt32();
            if (version == Version)
            {
                //マスターデータのデフォルト設定がTRUEになった場合、それが反映されるようにする
                IsOpened = Data.DefaultOpened | reader.ReadBoolean();
                HasRead = Data.DefaultRead | reader.ReadBoolean();
            }
            else
            {
                Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
            }
        }
    }
}
