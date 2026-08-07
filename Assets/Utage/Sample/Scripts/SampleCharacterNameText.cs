using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //AdvGraphicObjectのキャラクター名を取得するサンプル
    public class SampleCharacterNameText : MonoBehaviour
    {
        //AdvGraphicObjectのキャラクター名を取得する
        //基本的には、Engine.Page.CharacterInfo.NameTextで、現在の表示キャラクター名は取得できる
        //表示中の任意のキャラクター名などが必要な場合は、少し複雑
        public string GetCharacterNameText(AdvGraphicObject advObject)
        {
            
            //オブジェクト名(キャラクターラベル)
            var objectName = advObject.gameObject.name;
            
            //NameTextを取得するにはキャラクターシートが必要
            //キャラクターシートにない名前（シナリオ上で一時的に指定している表示名など）は取得できない
            var graphicInfo = advObject.LastResource;
            if(graphicInfo is { SettingData: AdvCharacterSettingData settingData })
            {
                //キャラクターの名前を取得
                objectName = TextData.MakeCustomLogText(settingData.NameText);
                //キャラクターの名前をローカライズする
                var localizedName = LanguageManagerBase.Instance.LocalizeText( objectName);
                return localizedName;
            }
            else
            {
                Debug.LogWarning("AdvGraphicObject does not have a valid character setting data: " + objectName);
                return objectName;
            }
        }
    }
}