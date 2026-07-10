using System.Collections.Generic;
using System.Linq;
using System.Net;
using System;
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
    ExperimentData_DoubleToggle,
    //Monitor에 들어가는 카드
    MonitoringCardA,
    MonitoringCardB,
    MonitoringCardC,
    SettingCard,
}

public enum EDataCardButtonType
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

    public TMP_InputField DataSettingField;

    public Button StateButton;

    [Header("MultiSlot")]
    // MultiSlot TextMeshProUGUI를 관리하기 위한 규칙
    public string MultiSlotTextRule = "MultiText_";
    public string MultiSlotValueRule = "MultiValueText_";
    public List<string> MultiSlotNames = new List<string>();


    // Main Value 외에도 여러 개의 TextMeshProUGUI를 관리하기 위한 리스트
    private List<TextMeshProUGUI> MultiSlotTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> MultiSlotValueTexts = new List<TextMeshProUGUI>();
    private List<TMP_InputField> MultiSlotInputFields = new List<TMP_InputField>();


    [Header("Experiment Schedule")]
    public TextMeshProUGUI scheduleNameText;
    public TextMeshProUGUI scheduleDescriptionText;
    public TMP_InputField scheduleValue;

    private TextMeshProUGUI experimentScheduleNo;
    private List<Toggle> Toggles = new List<Toggle>();

    // NOTE : 초기화 시 Card에 필요한 Info를 저장하고 데이터 변화 시 currentData를 사용하여 Key값이 Info와 일치하는 데이터를 저장.
    public Dictionary<string, InstrumentInfo> info { get; private set; } = new Dictionary<string, InstrumentInfo>();
    public Dictionary<string, Datas> currentData { get; private set; } = new Dictionary<string, Datas>();

    // ExperimentBox의 Scheduleing을 위해 사용
    private DataBox containingBox;
    public ExperimentWrapper experimentSchedule { get; private set; }

    // Experiment Setting을 위해 사용
    private Monitor containingMonitor;
    private ExperimentInfo experimentInfo;

    protected void Initialize(bool beforeVersionDonotUse)
    {
        base.Initialize();

        InitializeMultiSlots();

        if (cardType == EDataCardType.ExperimentSchedule)
        {
            Transform scheduleNo = transform.Find("ScheduleNo");

            if (scheduleNo != null)
            {
                experimentScheduleNo = scheduleNo.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                Debug.LogWarning($"[{name}] ScheduleNo 오브젝트를 찾을 수 없습니다.");
            }
        }
    }
    protected override void Initialize()
    {
        base.Initialize();

        InitializeMultiSlots();

        if (cardType == EDataCardType.ExperimentSchedule)
        {
            experimentScheduleNo = GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(x => x.name == "ScheduleNo");

            if (experimentScheduleNo == null)
            {
                Debug.LogWarning($"[{name}] ScheduleNo TextMeshProUGUI를 찾을 수 없습니다.");
            }
        }
        else if(cardType == EDataCardType.SettingCard)
        {

        }
    }
    public void Initialize(InstrumentInfo instrumentInfo)
    {
        if (instrumentInfo == null)
            return;

        if (cardType == EDataCardType.SettingCard)
        {
            info[instrumentInfo.Tag] = instrumentInfo;

            tagText.text = instrumentInfo.Tag;
            valueText.text = instrumentInfo.NO.ToString();

            for (int i = 0; i < MultiSlotNames.Count; i++)
            {
                string propertyName = MultiSlotNames[i];

                object value = GetInstrumentInfoValue(instrumentInfo, propertyName);


                if (i < MultiSlotInputFields.Count && MultiSlotInputFields[i] != null)
                {
                    MultiSlotInputFields[i].text = value?.ToString() ?? "";
                }
            }

            return;
        }

        if (string.IsNullOrEmpty(ObjectID))
        {
            ObjectID = instrumentInfo.Group;
            UpdateTagText(ObjectID);
        }

        info[instrumentInfo.Tag] = instrumentInfo;
    }
    public void Initialize(ExperimentInfo experimentInfo)
    {
        //만약 Toggle을 찾아야한다면, "Toggle_"규칙을 가진 Toggle을 찾아서 Toggles라는 List에 저장
        if (cardType == EDataCardType.ExperimentData_DoubleToggle)
        {
            Toggles = GetComponentsInChildren<Toggle>(true)
                .Where(t => t.name.StartsWith("Toggle_"))
                .OrderBy(t =>
                {
                    string numberText = t.name.Replace("Toggle_", "");

                    if (int.TryParse(numberText, out int number))
                        return number;

                    return int.MaxValue;
                })
                .ToList();
        }
    }

    private void InitializeMultiSlots()
    {
        MultiSlotTexts.Clear();
        MultiSlotValueTexts.Clear();

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);


        MultiSlotTexts = GetComponentsByRule<TextMeshProUGUI>(texts, MultiSlotTextRule);
        if (cardType != EDataCardType.SettingCard) MultiSlotValueTexts = GetComponentsByRule<TextMeshProUGUI>(texts, MultiSlotValueRule);
        else
        {
            MultiSlotInputFields.Clear();

            TMP_InputField[] fields = GetComponentsInChildren<TMP_InputField>(true);

            MultiSlotInputFields = GetComponentsByRule<TMP_InputField>(fields, MultiSlotValueRule);
        }

        // MultiSlotNames -> MultiText에 적용
        for (int i = 0; i < MultiSlotTexts.Count; i++)
            {
                if (i >= MultiSlotNames.Count)
                    break;

                if (MultiSlotTexts[i] == null)
                    continue;

                MultiSlotTexts[i].text = $"{MultiSlotNames[i]} :";

            }


        //Debug.Log($"[{name}] MultiText : {MultiSlotTexts.Count}, " + $"MultiValue : {MultiSlotValueTexts.Count}");
    }
    private List<T> GetComponentsByRule<T>(T[] components, string rule) where T : Component
    {
        if (components == null)
            return new List<T>();

        return components.Where(t => t != null && t.name.StartsWith(rule)).OrderBy(t =>
        {
            string numberText = t.name.Replace(rule, "");

            if (int.TryParse(numberText, out int number))
                return number;

            return int.MaxValue;
        }).ToList();
    }


    protected override void EventSubscriber()
    {
        base.EventSubscriber();
    }
    protected override void EventUnsubscriber()
    {
        base.EventUnsubscriber();
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

    /// <summary>
    /// DataCard에 있는 Button이 클릭이 되면 작동되는 함수
    /// </summary>
    /// <param name="buttonType">버튼의 이름 or 종류</param>
    public void IsButtonClick(string buttonType)
    {
        if (Manager.Experiment.isProcessing) return;

        InstrumentInfo target = new InstrumentInfo();
        ushort value = 0;

        switch (buttonType) 
        {
            case nameof(EDataCardButtonType.ButtonToggle):
                target = info.Values.FirstOrDefault(x => x.PointType == "DO");

                if (target == null)
                {
                    Debug.LogWarning("[DataCard] DO 타입을 찾을 수 없습니다.");
                    return;
                }

                if (currentData.TryGetValue(target.Tag, out var data))
                {
                    value = data.Value > 0 ? (ushort)0 : (ushort)1;
                }
                else value = 1;
                Manager.Network.ReserveDateWriteing(target.PointType, (ushort)target.Address, value);
                break;
            case nameof(EDataCardButtonType.ButtonDataSet):
                target = info.Values.FirstOrDefault(x => x.Type == EDataCategory.Setting);

                value = ushort.Parse(DataSettingField.text);

                Manager.Network.ReserveDateWriteing(target.PointType, (ushort)target.Address, value);
                break;
            case nameof(EDataCardButtonType.ButtonTimeSet):
                break;

        }
    }
    public override void OnClick()
    {
        base.OnClick();
        if( cardType != EDataCardType.ExperimentData && cardType != EDataCardType.ExperimentSchedule) containingBox.OnClickedDataCard(cardType, info.Values.FirstOrDefault().System);
        else containingBox.OnClickedDataCard(cardType, ObjectID);

    }

    private void OnTextUpdate(Datas data, EDataCategory dataCategory = EDataCategory.Value)
    {
        if (data == null || dataCategory == EDataCategory.None) return;

        switch (dataCategory)
        {
            case EDataCategory.Value:
                UpdateValueText(data);
                break;
            case EDataCategory.Inte:
                UpdateMultiText(data);
                break;
        }
    }


    private void UpdateTagText(string tag)
    {
        if (tagText != null)
            //tagText.text = tag + " : ";
            tagText.text = tag;
    }

    private void UpdateValueText(Datas data)
    {
        if (valueText == null)
        {
            Debug.LogWarning($"[DataCard] Value Text is null : {ObjectID}");
            return;
        }

        if (cardType == EDataCardType.InputDataB)
        {
            valueText.text = (int)data.Value switch
            {
                0 => "Normal",
                1 => "High",
                2 => "Low",
                _ => data.Value.ToString()
            };
        }
        else if (cardType == EDataCardType.OutputDataA)
        {
            valueText.text = data.Value > 0 ? "Open" : "Close";
        }
        else
        {
            valueText.text = data.Value.ToString();
        }
    }

    private void UpdateMultiText(Datas data)
    {
        // data를 모니터링할 MultiSlotValueTexts의 인덱스를 찾기 위해 MultiSlotNames에서 Measurement를 검색
        int index = MultiSlotNames.IndexOf(info[data.Name].Measurement);

        if (index < 0)
        {
            Debug.LogWarning($"Measurement '{info[data.Name].Measurement}'를 MultiSlotNames에서 찾을 수 없습니다.");
            return;
        }

        if (index < MultiSlotValueTexts.Count)
        {
            //MultiSlotValueTexts의 인덱스가 범위 내에 있는 경우에만 업데이트
            MultiSlotValueTexts[index].text = data.Value.ToString();
        }
        else
        {
            Debug.LogWarning($"MultiSlotValueTexts의 인덱스 {index}가 범위를 벗어났습니다. (Count: {MultiSlotValueTexts.Count})");
        }

    }
    private void UpdateCustomText(Datas data)
    {
        if (valueText != null) valueText.text = $"{data.Name} : {data.Value}";
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
    public void SetMultiValue(int index, string value)
    {
        if (index < 0 || index >= MultiSlotValueTexts.Count)
            return;

        MultiSlotValueTexts[index].text = value;
    }
    #region ExperimentScheduleCard

    [SerializeField] private Image cardImage;
    [SerializeField] private Image LeftNumberimage;
    [SerializeField] Image RightTextImage;
    [SerializeField] private List<TextMeshProUGUI> scheduleTexts;
    

    public void UpdateExperimentStateColor()
    {
        if (experimentSchedule == null)
            return;

        switch (experimentSchedule.ReservedState)
        {
            case EReservedExperimentState.Reserved:
                cardImage.color = Color.white;
                LeftNumberimage.color = Color.white;
                RightTextImage.color = Color.white;
                foreach (var text in scheduleTexts)
                {
                    text.color = Color.black;
                }
                break;

            case EReservedExperimentState.Processing:
                if(cardImage != null)
                    cardImage.color = new Color(187f/255f,226f/255f,214f/255f);
                if(LeftNumberimage != null)
                    LeftNumberimage.color = new Color(27f/255f, 175f/255f, 130f/255f);
                if(RightTextImage != null)
                    RightTextImage.color = new Color(27f/255f, 175f/255f, 130f/255f);
              
                if (scheduleTexts != null)
                foreach (var text in scheduleTexts)
                {
                    text.color = Color.white;
                }

                break;

            case EReservedExperimentState.Resetting:
                cardImage.color = new Color(253f/255f, 241f/255f, 255f/255f);
                LeftNumberimage.color = new Color (250f/255f, 107f/255f, 1f/255f);
                RightTextImage.color = new Color(250f/255f, 107f/255f, 1f/255f);
                foreach (var text in scheduleTexts)
                {
                    text.color = Color.white;
                }
                break;

            case EReservedExperimentState.Failed:
                cardImage.color = new Color(252f/255f, 244f/255f, 245f/255f);
                LeftNumberimage.color = new Color(207f/255f, 39f/255f, 42f/255f);
                RightTextImage.color = new Color(207f/255f, 39f/255f, 42f/255f);
                foreach(var text in scheduleTexts)
                {
                    text.color = Color.white;
                }
                break;

            case EReservedExperimentState.Finished:
                cardImage.color = new Color(241f/255f, 241f/255f, 242f/255f);
                LeftNumberimage.color = new Color(170f/255f, 169f/255f, 170f/255f);
                RightTextImage.color = new Color(170f/255f, 169f/255f, 170f/255f);
                foreach (var text in scheduleTexts)
                {
                    text.color = Color.white;
                }
                break;
        }
    }
    #endregion

    #region ExperimentInfomationCard
    public void ExperimentdataSetting(ExperimentInfo info)
    {
        experimentInfo = info;

        scheduleNameText.text = info.Name;
        scheduleDescriptionText.text = info.Description;

        // Toggle 초기화
        Initialize(info);

        SetInfoValues(info);
    }
    public void ExperimentScheduleSetting(ExperimentWrapper wrapper)
    {
        if (experimentScheduleNo == null)
        {
            experimentScheduleNo = GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(x => x.name == "ScheduleNo");
        }

        experimentSchedule = wrapper;

        if (experimentScheduleNo != null)
            experimentScheduleNo.text = wrapper.No.ToString();

        scheduleNameText.text = wrapper.Name;
        valueText.text = wrapper.ReservedState.ToString();

        UpdateExperimentStateColor();
    }
    public ExperimentInfo GetExperimentInfo()
    {
        int value = 0;

        if (cardType == EDataCardType.ExperimentData_DoubleToggle)
        {
            if (Toggles != null)
            {
                for (int i = 0; i < Toggles.Count; i++)
                {
                    if (Toggles[i] != null && Toggles[i].isOn)
                    {
                        value |= (1 << i);
                    }
                }
            }
        }
        else
        {
            if (scheduleValue != null)
            {
                int.TryParse(scheduleValue.text, out value);
            }
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
    public void UpdateExperimentInfoCardColor(int processBits)
    {
        if (experimentInfo == null)
            return;

        int bitIndex = experimentInfo.No - 1; // No가 1부터 시작하는 경우

        bool completed = ((processBits >> bitIndex) & 1) == 1;

        if (completed)
        {
            cardImage.color = Color.green;
        }
        else
        {
            cardImage.color = Color.gray;
        }
    }

    public void SetExperimentInfoCardColor(bool completed)
    {
        if (cardImage == null)
            return;

        cardImage.color = completed ? Color.green : Color.gray;
    }

    private void SetInfoValues(ExperimentInfo info)
    {
        if (info.Action == "End")
            return;

        if (cardType == EDataCardType.ExperimentData_DoubleToggle)
        {
            for (int i = 0; i < Toggles.Count; i++)
            {
                if (Toggles[i] == null)
                    continue;

                Toggles[i].isOn = (info.Value & (1 << i)) != 0;
            }
        }
        else
        {
            if (scheduleValue != null)
            {
                scheduleValue.text = info.Value.ToString();
            }
        }
    }
    private object GetInstrumentInfoValue(InstrumentInfo info, string propertyName)
    {
        return propertyName switch
        {
            nameof(InstrumentInfo.NO) => info.NO,
            nameof(InstrumentInfo.Tag) => info.Tag,
            nameof(InstrumentInfo.Function) => info.Function,
            nameof(InstrumentInfo.PointType) => info.PointType,
            nameof(InstrumentInfo.Type) => info.Type,
            nameof(InstrumentInfo.InstrumentType) => info.InstrumentType,
            nameof(InstrumentInfo.Measurement) => info.Measurement,
            nameof(InstrumentInfo.Group) => info.Group,
            nameof(InstrumentInfo.System) => info.System,
            nameof(InstrumentInfo.DataType) => info.DataType,
            nameof(InstrumentInfo.RangeMin) => info.RangeMin,
            nameof(InstrumentInfo.RangeMax) => info.RangeMax,
            nameof(InstrumentInfo.PLCMin) => info.PLCMin,
            nameof(InstrumentInfo.PLCMax) => info.PLCMax,
            nameof(InstrumentInfo.Address) => info.Address,
            nameof(InstrumentInfo.Useable) => info.Useable,
            nameof(InstrumentInfo.Description) => info.Description,
            nameof(InstrumentInfo.Note) => info.Note,
            _ => null
        };
    }

    #endregion
}
