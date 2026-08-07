#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    public abstract class EditorSettingsSingleton<T> : ScriptableSingleton<T>
        where T : ScriptableObject
    {
        protected virtual void OnEnable()
        {
            hideFlags &= ~HideFlags.NotEditable;
        }
        
        public static T GetInstance()
        {
            return instance;
        }

        public virtual void OnSave()
        {
            this.Save(true);
        }
    }
}
#endif
