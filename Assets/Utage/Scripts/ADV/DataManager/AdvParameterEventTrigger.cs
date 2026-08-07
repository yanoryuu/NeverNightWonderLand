using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
    //パラメーターの変更があったときにイベントを発生させるためのコンポーネント
    //ただし、パラメーターを個別に変更するときのみイベントが発生する
    //パラメーター全体の初期化やリセットのタイミングには対応してないので
    //必要に応じて、AdvEngineの初期化時やセーブデータのロード時に手動でイベントを発生させる必要がある
    //AdvEngineの初期化時（システムセーブデータロード後）　AdvEngine.OnPostInit
    //AdvEngineの終了時　AdvEngine.OnClear
    //セーブデータのロード後　AdvScenarioPlayer.OnBeginScenarioAfterParametersInitialized
    public class AdvParameterEventTrigger : MonoBehaviour
    {
        AdvEngine AdvEngine => this.GetComponentCache(ref engine);
        AdvEngine engine;

        [Serializable] public class UnityEventParam : UnityEvent<AdvParamData> { }

        //なんらかの変更があった
        public UnityEventParam OnChanged => onChanged;
        [SerializeField] protected UnityEventParam onChanged = new ();

        //名前指定のパラメーターの変更イベントの基底抽象クラス
        [Serializable]
        public abstract class UnityEventParamByNameBase<T>
            where T : UnityEventBase, new ()
        {
            public string paramName;
            public T onChanged = new();
        }

        //名前指定のパラメーターの変更イベント
        [Serializable] public class UnityEventParamByName : UnityEventParamByNameBase<UnityEventParam> { }
        public List<UnityEventParamByName> OnChangedByNamesEvents => onChangedByNamesEvents;
        [SerializeField] List<UnityEventParamByName> onChangedByNamesEvents = new ();

        //Boolパラメーターの変更イベント
        [Serializable] public class UnityEventBoolParamByName : UnityEventParamByNameBase<BoolEvent>{}
        public List<UnityEventBoolParamByName> OnChangedBoolParamEvents => onChangedBoolParamEvents;
        [SerializeField] List<UnityEventBoolParamByName> onChangedBoolParamEvents = new();

        //Floatパラメーターの変更イベント
        [Serializable] public class UnityEventFloatParamByName : UnityEventParamByNameBase<FloatEvent>{}
        public List<UnityEventFloatParamByName> OnChangedFloatParamEvents => onChangedFloatParamEvents;
        [SerializeField] List<UnityEventFloatParamByName> onChangedFloatParamEvents = new();

        //Intパラメーターの変更イベント
        [Serializable] public class UnityEventIntParamByName : UnityEventParamByNameBase<IntEvent>{}
        public List<UnityEventIntParamByName> OnChangedIntParamEvents => onChangedIntParamEvents;
        [SerializeField] List<UnityEventIntParamByName> onChangedIntParamEvents = new();
        
        //stringパラメーターの変更イベント
        [Serializable] public class UnityEventStringParamByName : UnityEventParamByNameBase<StringEvent>{}
        public List<UnityEventStringParamByName> OnChangedStringParamEvents => onChangedStringParamEvents;
        [SerializeField] List<UnityEventStringParamByName> onChangedStringParamEvents = new();

        private void Awake()
        {
            CheckSameParamName<UnityEventParamByName, UnityEventParam>(OnChangedByNamesEvents,nameof(OnChangedByNamesEvents));
            CheckSameParamName<UnityEventBoolParamByName, BoolEvent>(OnChangedBoolParamEvents,nameof(OnChangedBoolParamEvents));
            CheckSameParamName<UnityEventFloatParamByName, FloatEvent>(OnChangedFloatParamEvents,nameof(OnChangedFloatParamEvents));
            CheckSameParamName<UnityEventIntParamByName, IntEvent>(OnChangedIntParamEvents,nameof(OnChangedIntParamEvents));
            CheckSameParamName<UnityEventStringParamByName, StringEvent>(OnChangedStringParamEvents,nameof(OnChangedStringParamEvents));
        }
        //イベントリスト内に、同じparamNameが登録されていたらエラーを出す
        void CheckSameParamName<TEventByName, TEvent>(List<TEventByName> events, string listName)
            where TEventByName : UnityEventParamByNameBase<TEvent>
            where TEvent : UnityEventBase, new()
        {
            HashSet<string> checkSet = new();
            foreach (var item in events)
            {
                if (!checkSet.Add(item.paramName))
                {
                    Debug.LogError($"Same paramName is already registered in {nameof(AdvParameterEventTrigger)}  {listName}. paramName:{item.paramName}",this);
                }
            }
        }

        //名前指定のパラメーターの変更イベントを取得。なければnull
        TEventByName FindEventByName<TEventByName,TEvent>(List<TEventByName> events, string paramName)
            where TEventByName : UnityEventParamByNameBase<TEvent>
            where TEvent : UnityEventBase, new()
        {
            foreach (var item in events)
            {
                if (item.paramName == paramName)
                {
                    return item;
                }
            }
            return null;
        }

        //名前指定のパラメーターの変更イベントを取得。なければ作成 
        TEventByName FindEventByNameOrCreateIfMissing<TEventByName, TEvent>(List<TEventByName> events, string paramName)
            where TEventByName : UnityEventParamByNameBase<TEvent>, new()
            where TEvent : UnityEventBase, new()
        {
            var item = FindEventByName<TEventByName, TEvent>(events,paramName);
            if (item == null)
            {
                item = new TEventByName { paramName = paramName };
                events.Add(item);
            }
            return item;
        }

        //名前指定のパラメーターの変更イベントを追加
        public void AddEventByName(string paramName, UnityAction<AdvParamData> onChangedByName, bool invokeImmediately = true)
        {
            FindEventByNameOrCreateIfMissing<UnityEventParamByName, UnityEventParam>(OnChangedByNamesEvents, paramName).onChanged.AddListener(onChangedByName);
        }

        //名前指定のパラメーターの変更イベントを削除
        public void RemoveEventByName(string paramName, UnityAction<AdvParamData> onChangedByName)
        {
            FindEventByName<UnityEventParamByName, UnityEventParam>(OnChangedByNamesEvents, paramName)?.onChanged.RemoveListener(onChangedByName);
        }

        //Boolのパラメーターの変更イベントを追加
        public void AddBoolEvent(string paramName, UnityAction<bool> onChangedBool)
        {
            FindEventByNameOrCreateIfMissing<UnityEventBoolParamByName, BoolEvent>(OnChangedBoolParamEvents, paramName).onChanged.AddListener(onChangedBool);
        }
        //Boolのパラメーターの変更イベントを削除
        public void RemoveBoolEvent(string paramName, UnityAction<bool> onChangedBool)
        {
            FindEventByName<UnityEventBoolParamByName, BoolEvent>(OnChangedBoolParamEvents, paramName)?.onChanged.RemoveListener(onChangedBool);
        }

        //Floatのパラメーターの変更イベントを追加
        public void AddFloatEvent(string paramName, UnityAction<float> onChangedFloat)
        {
            FindEventByNameOrCreateIfMissing<UnityEventFloatParamByName, FloatEvent>(OnChangedFloatParamEvents, paramName).onChanged.AddListener(onChangedFloat);
        }
        //Floatのパラメーターの変更イベントを削除
        public void RemoveFloatEvent(string paramName, UnityAction<float> onChangedFloat)
        {
            FindEventByName<UnityEventFloatParamByName, FloatEvent>(OnChangedFloatParamEvents, paramName)?.onChanged.RemoveListener(onChangedFloat);
        }

        //Intのパラメーターの変更イベントを追加
        public void AddIntEvent(string paramName, UnityAction<int> onChangedInt)
        {
            FindEventByNameOrCreateIfMissing<UnityEventIntParamByName, IntEvent>(OnChangedIntParamEvents, paramName).onChanged.AddListener(onChangedInt);
        }
        //Intのパラメーターの変更イベントを削除
        public void RemoveIntEvent(string paramName, UnityAction<int> onChangedInt)
        {
            FindEventByName<UnityEventIntParamByName, IntEvent>(OnChangedIntParamEvents, paramName)?.onChanged.RemoveListener(onChangedInt);
        }

        //Stringのパラメーターの変更イベントを追加
        public void AddStringEvent(string paramName, UnityAction<string> onChangedString)
        {
            FindEventByNameOrCreateIfMissing<UnityEventStringParamByName, StringEvent>(OnChangedStringParamEvents, paramName).onChanged.AddListener(onChangedString);
        }
        //Stringのパラメーターの変更イベントを削除
        public void RemoveStringEvent(string paramName, UnityAction<string> onChangedString)
        {
            FindEventByName<UnityEventStringParamByName, StringEvent>(OnChangedStringParamEvents, paramName)?.onChanged.RemoveListener(onChangedString);
        }


        //パラメーターの変更があったときに呼び出される
        public void CallEventChangeParameter(AdvParamData paramData)
        {
            OnChanged.Invoke(paramData);
            FindEventByName<UnityEventParamByName, UnityEventParam>(OnChangedByNamesEvents,paramData.Key)?.onChanged.Invoke(paramData);
            switch (paramData.Type)
            {
                case AdvParamData.ParamType.Bool:
                    FindEventByName<UnityEventBoolParamByName, BoolEvent>(OnChangedBoolParamEvents,paramData.Key)?.onChanged.Invoke(paramData.BoolValue);
                    break;
                case AdvParamData.ParamType.Float:
                    FindEventByName<UnityEventFloatParamByName, FloatEvent>(OnChangedFloatParamEvents,paramData.Key)?.onChanged.Invoke(paramData.FloatValue);
                    break;
                case AdvParamData.ParamType.Int:
                    FindEventByName<UnityEventIntParamByName, IntEvent>(OnChangedIntParamEvents,paramData.Key)?.onChanged.Invoke(paramData.IntValue);
                    break;
                case AdvParamData.ParamType.String:
                    FindEventByName<UnityEventStringParamByName, StringEvent>(OnChangedStringParamEvents,paramData.Key)?.onChanged.Invoke(paramData.StringValue);
                    break;
            }
        }
    }
}
