using System.Collections.Generic;
using System.Linq;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum EDataCardType
{
    InputDataA,
    InputDataB,
    InputDataC,
    OutputDataA,
    OutputDataB,
    OutputDataC,
    ExperimentSchedule,
    ExperimentData,
    MonitoringCardA,
    MonitoringCardB,
    MonitoringCardC,
}

public enum EButtonType
{
    ButtonToggle,
    ButtonTimeSet,
    ButtonDataSet,
    Button
}
public enum EDataCategory
{
    None,
    Tag,
    Status,
    Value,
    Setting,
    Inte,
    Custom,
}

public class DataCard : UiObjectBase
{
    // DataCard
    //  - DataCard는 DataBox에 속하는 UI 요소로, 각 DataCard는 IoInfo의 정보를 보여준다.

    public EDataCardType cardType;

    public TextMeshProUGUI tagText;
    public TextMeshProUGUI valueText;
    public Button StateButton;

    [Header("Experiment Schedule")]
    public TextMeshProUGUI scheduleNameText;
    public TextMeshProUGUI scheduleDescriptionText;
    public TMP_InputField scheduleValue;

    // NOTE : 초기화 시 Card에 필요한 Info를 저장하고 데이터 변화 시 currentData를 사용하여 Key값이 Info와 일치하는 데이터를 저장.
    public Dictionary<string, InstrumentInfo> info { get; private set; } = new Dictionary<string, InstrumentInfo>();
    public Dictionary<string, Datas> currentData { get; private set; } = new Dictionary<string, Datas>();

    // ExperimentBox의 Scheduleing을 위해 사용
    private DataBox containingBox;
    public ExperimentWrapper experimentSchedule { get; private set; }

    // Experiment Setting을 위해 사용
    private Monitor containingMonitor;
    private ExperimentInfo experimentInfo;


    protected override void Intialize()
    {
        base.Intialize();
    }

    public void Intialize(InstrumentInfo InstrumentInfo)
    {
        ObjectID = InstrumentInfo.Group;

        info[InstrumentInfo.Tag] = InstrumentInfo;

        UpdateTagText(ObjectID);
    }
    public void RegistBox(object containing)
    {
        if (containing is DataBox box)
        {
            containingBox = box;
        }
        else if(containing is Monitor monitor)
        {
            containingMonitor = monitor;
        }
    }



    /// <summary>
    /// 데이터 변화시 DataCard의 UI를 업데이트하는 함수
    /// </summary>
    /// <param name="obj"></param>
    public override void OnFunctionCalled(object obj = null)
    {
        // obj가 Dictionary<string, Datas>가 아닐경우 return
        if (obj is not Datas datas)
            return;

        string name = datas.Name;

        if (info.ContainsKey(name))
        {
            currentData[name] = datas;
            OnTextUpdate(datas, info[name].Type);
        }

    }

    public void IsButtonClick(string buttonType)
    {
        if (Manager.Experiment.isProcessing) return;
        switch (buttonType) 
        {
            case nameof(EButtonType.ButtonToggle):
                var target = info.Values.FirstOrDefault(x => x.PointType == "DO");

                if (target == null)
                {
                    Debug.LogWarning("[DataCard] DO 타입을 찾을 수 없습니다.");
                    return;
                }

                ushort value = 0;

                if (currentData.TryGetValue(target.Tag, out var data))
                {
                    value = data.Value > 0 ? (ushort)0 : (ushort)1;
                }
                else value = 1;

                    Manager.Network.ReserveDateWriteing(target.PointType, (ushort)target.Address, value);

                break;
            case nameof(EButtonType.ButtonTimeSet):
                break;

        }
    }
    public override void OnClick()
    {
        base.OnClick();
        if( cardType != EDataCardType.ExperimentData) containingBox.OnClickedDataCard(cardType, ObjectID);
    }

    private void OnTextUpdate(Datas data, EDataCategory dataCategory = EDataCategory.Value)
    {
        if (data == null || dataCategory == EDataCategory.None) return;

        switch (dataCategory)
        {
            case EDataCategory.Value:
                UpdateValueText(data);
                break;
            case EDataCategory.Custom:
                UpdateCustomText(data);
                break;
        }
    }


    private void UpdateTagText(string tag)
    {
        if (tagText != null)
            tagText.text = tag + " : ";
    }

    private void UpdateValueText(Datas data)
    {
        if (valueText != null)
            valueText.text = data.Value.ToString();
    }

    private void UpdateCustomText(Datas data)
    {
        if (valueText != null)
            valueText.text = $"{data.Name} : {data.Value}";
    }
    private void UpdateStateButton(Datas data)
    {
        if (StateButton != null)
        {
            // 예시: 상태에 따라 버튼 색상 변경
            if (data.Value > 0)
                StateButton.image.color = Color.green;
            else
                StateButton.image.color = Color.red;
        }
    }

    #region ExperimentInfomationCard
    public void ExperimentdataSetting(ExperimentInfo info)
    {
        experimentInfo = info;
        scheduleNameText.text = info.Name;
        scheduleDescriptionText.text = info.Description;
        SetInfoValues(info);
    }
    public void ExperimentScheduleSetting(ExperimentWrapper wrapper)
    {
        experimentSchedule = wrapper;
        scheduleNameText.text = wrapper.Name;
    }
    public ExperimentInfo GetExperimentInfo()
    {
        int value = 0;

        if(scheduleValue != null)
        {
            int.TryParse(scheduleValue.text, out value);
        }

        return new ExperimentInfo
        {
            No = experimentInfo.No,
            Name = experimentInfo.Name,
            Description = experimentInfo.Description,
            Group = experimentInfo.Group,
            Tag = experimentInfo.Tag,
            Action = experimentInfo.Action,
            Value = value,
            Process = experimentInfo.Process
        };
    }

    private void SetInfoValues(ExperimentInfo info)
    {
        if (info.Action == "End") return;

        scheduleValue.text = info.Value.ToString();
    }
    #endregion
}
