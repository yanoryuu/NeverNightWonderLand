using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
    //カスタムデータコンテナのクリエイター
    public abstract class AdvCustomDataContainerCreator : ScriptableObject 
    {
        //カスタムデータとして扱うものかチェック
        public abstract bool IsTargetDataName(string customDataName);

        //カスタムデータコンテナを作成
        public abstract AdvCustomDataContainer CreateCustomDataContainer(AdvCustomDataManager customDataManager);

        //カスタムデータコンテナの型を取得
        public abstract Type DataContainerType();
    }

    //カスタムデータコンテナのクリエイターのジェネリック型指定版
    public abstract class AdvCustomDataContainerCreator<T> : AdvCustomDataContainerCreator
        where T : AdvCustomDataContainer
    {
        public override Type DataContainerType() => typeof(T);
    }
}
