using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;


namespace Utage
{
    //Paramシートで設定しているFloat値を、コンフィグのスライダーでUI制御するサンプル
    public class SampleCustomConfigValueController : MonoBehaviour
    {
        public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
        [SerializeField] AdvEngine engine;

        public Slider Slider => this.GetComponentCache(ref slider);
        [SerializeField] Slider slider;

        //対象のパラメーター名
        string ParamName => paramName;
        [SerializeField] string paramName;

        //デフォルト値
        float DefaultValue => defaultValue;
        [SerializeField] float defaultValue;


        public float GetValue()
        {
            if (!Engine.Param.IsInit)
            {
                Debug.LogWarning("まだパラメーターが初期化されていません");
                return 0;
            }

            return Engine.Param.GetParameterFloat(ParamName);
        }

        public void SetValue(float value)
        {
            if (!Engine.Param.IsInit)
            {
                Debug.LogWarning("まだパラメーターが初期化されていません");
            }

            Engine.Param.SetParameterFloat(ParamName, value);
        }

        public void OnClickConfigReset()
        {
            //パラメーターをリセット
            SetValue(DefaultValue);
        }

        void Start()
        {
            //スライダーの値が変わったら、パラメーターに設定
            Slider.onValueChanged.AddListener(SetValue);
        }

        //OnEnableのタイミングで初期化しているので、Sliderかコンフィグ画面とおなじGameObjectにAddComponentするのがベスト
        void OnEnable()
        {
            Slider.value = GetValue();
        }
    }
}

