using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utage
{
    //Legacyを扱う古い型と、TextMeshProを扱う新しい型の情報
    public class LegacyToTextMeshProTypeInfo
    {
        public Type OldType { get; }
        public Type NewType { get; }

        public LegacyToTextMeshProTypeInfo(Type oldType, Type newType)
        {
            OldType = oldType;
            NewType = newType;
            if (!NewType.GetInterfaces().ToList().Exists(x => x == typeof(IUsingTextMeshPro)))
            {
                //新しい型がIUsingTextMeshProを継承していないのでエラー出力
                Debug.LogError($"{NewType} is not IUsingTextMeshPro");
            }
        }
    }
}
