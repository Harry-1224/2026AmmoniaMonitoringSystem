using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum EDataCardType
{

    InputDataA,
    InputDataB,
    InputDataC,
    OutputDataA,
    OutputDataB,
    OutputData_Inverter,
    ExperimentSchedule,
    ExperimentData,
    ExperimentData_DoubleToggle,
    //Monitor에 들어가는 카드
    MonitoringCardA,
    MonitoringCardB,
    MonitoringCardC,
    MonitoringCard_Button,
    SettingCard,
}

public enum EDataCardButtonType
{
    ButtonToggle,
    ButtonTimeSet,
    ButtonDataSet,
    ButtonTogggleMode,
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
    Toggle,
    InteReset,
    Mode,
    Command
}

public class DataCard : UiObjectBase
{
    // DataCard
    //  - DataCard는 DataBox에 속하는 UI 요소로, 각 DataCard는 IoInfo의 정보를 보여준다.

    public EDataCardType cardType;

    public TextMeshProUGUI tagText;
    public TextMeshProUGUI valueText;
    public int decimalPoint = 2;

    public TMP_InputField DataSettingField;
    public Toggle DataUseableSet;
    public Button StateButton;

    // Timer Parametter
    private int timerValue = 0;
    private Coroutine timerCoroutine;

    [Header("MultiSlot")]
    // MultiSlot TextMeshProUGUI를 관리하기 위한 규칙
    public string MultiSlotTextRule = "MultiText_";
    public string MultiSlotValueRule = "MultiValueText_";
    public string MultiSlotLampRule = "MultiLamp_";
    public List<string> MultiSlotNames = new List<string>();


    // Main Value 외에도 여러 개의 TextMeshProUGUI를 관리하기 위한 리스트
    private List<TextMeshProUGUI> MultiSlotTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> MultiSlotValueTexts = new List<TextMeshProUGUI>();
    private List<TMP_InputField> MultiSlotInputFields = new List<TMP_InputField>();
    private List<Image> MultiSlotImange = new List<Image>();


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
        if (cardType == EDataCardType.SettingCard)
        {
            MultiSlotInputFields.Clear();

            TMP_InputField[] fields = GetComponentsInChildren<TMP_InputField>(true);

            MultiSlotInputFields = GetComponentsByRule<TMP_InputField>(fields, MultiSlotValueRule);
        }
        else if (cardType == EDataCardType.MonitoringCard_Button) 
        {
            MultiSlotImange.Clear();

            Image[] images = GetComponentsInChildren<Image>(true);

            MultiSlotImange = GetComponentsByRule<Image>(images, MultiSlotLampRule);

        }
        else MultiSlotValueTexts = GetComponentsByRule<TextMeshProUGUI>(texts, MultiSlotValueRule);
        

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
        // LoggingManger
        //Manager.Logging.OnLoggingStarted += LoggingStartEventHandler;
        //Manager.Logging.OnLoggingStopped += LoggingStopEventHandler;
    }
    protected override void EventUnsubscriber()
    {
        base.EventUnsubscriber();
        //Manager.Logging.OnLoggingStarted -= LoggingStartEventHandler;
        //Manager.Logging.OnLoggingStopped -= LoggingStopEventHandler;
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
    public override void OnFunctionCalled( object obj = null)
    {
        // obj가 Dictionary<string, Datas>가 아닐경우 return
        if (obj is not Datas datas)  return;

        string name = datas.Name;

        if (!info.ContainsKey(name)) return;


        currentData[name] = datas; 

        OnTextUpdate(datas, info[name].Type);
    }
    /**/
    public  void OnFunctionCalled(bool before, object obj = null)
    {
        // obj가 Dictionary<string, Datas>가 아닐경우 return
        if (obj is not Datas datas) return;

        string name = datas.Name;

        if (!info.ContainsKey(name)) return;


        currentData[name] = datas;
        if (info[name].PointType == "DI" && info[name].PointType == "DO") HandleDigitalData(datas);
        else HandleAnalogData(datas);
    }

    private void HandleDigitalData(Datas datas)
    {

    }

    private void HandleAnalogData(Datas datas)
    {

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
            case nameof(EDataCardButtonType.ButtonTogggleMode):
                target = info.Values.FirstOrDefault(x => x.Type == EDataCategory.Mode);

                if (target == null)
                {
                    Debug.LogWarning(
                        $"[DataCard/{ObjectID}] Mode 타입을 찾을 수 없습니다.");
                    return;
                }

                if (DataUseableSet == null)
                {
                    Debug.LogWarning(
                        $"[DataCard/{ObjectID}] DataUseableSet이 없습니다.");
                    return;
                }

                // Toggle에서 원하는 값
                // ON  = 1
                // OFF = 0
                value = DataUseableSet.isOn? (ushort)1 : (ushort)0;

                // 현재 Network에서 받은 값 확인
                if (currentData.TryGetValue(target.Tag, out Datas data))
                {
                    bool currentState = data.Value > 0;

                    // 현재 값과 Toggle 값이 같으면 전송할 필요 없음
                    if (currentState == DataUseableSet.isOn)
                    {
                        Debug.Log( $"[{target.Tag}] 이미 같은 상태입니다. " + $"State = {value}");
                        return;
                    }
                }

                // 값이 다를 때만 PLC로 전송
                Manager.Network.ReserveDateWriteing(target.PointType, (ushort)target.Address, value);

                Debug.Log( $"[{target.Tag}] Mode Change → {value}");

                break;

            case nameof(EDataCardButtonType.ButtonToggle):

                target = info.Values.FirstOrDefault(x => x.PointType == "DO");

                if (target == null)
                {
                    Debug.LogWarning($"[DataCard/{ObjectID}] DO 타입을 찾을 수 없습니다.");
                    return;
                }

                // 현재 DO 상태를 기준으로 다음 값 결정
                if (currentData.TryGetValue(target.Tag, out var _data))
                {
                    value = _data.Value > 0
                        ? (ushort)0
                        : (ushort)1;
                }
                else
                {
                    // 현재 상태를 모르면 ON으로 시작
                    value = 1;
                }

                Debug.Log($"[{target.Tag}] State Change : {value} / Timer : {timerValue}");

                // ======================================================
                // OFF
                // ======================================================
                if (value == 0)
                {
                    // 타이머 실행 중이면 중단
                    if (timerCoroutine != null)
                    {
                        StopCoroutine(timerCoroutine);
                        timerCoroutine = null;
                    }

                    Manager.Network.ReserveDateWriteing(
                        target.PointType,
                        (ushort)target.Address,
                        0);

                    Debug.Log($"[{target.Tag}] DO OFF");

                    break;
                }

                // ======================================================
                // ON
                // ======================================================

                // 타이머가 설정되어 있는 경우
                if (timerValue > 0)
                {
                    // 이전 타이머가 혹시 남아있다면 중단
                    if (timerCoroutine != null)
                    {
                        StopCoroutine(timerCoroutine);
                    }

                    timerCoroutine = StartCoroutine(
                        TimerDOControl(target, timerValue));
                }
                else
                {
                    // 타이머가 없으면 일반 Toggle
                    Manager.Network.ReserveDateWriteing(
                        target.PointType,
                        (ushort)target.Address,
                        1);

                    Debug.Log($"[{target.Tag}] DO ON");
                }
                break;
            case nameof(EDataCardButtonType.ButtonDataSet):
                target = info.Values.FirstOrDefault(x => x.Type == EDataCategory.Setting);
                value = ConvertDataToPLC(float.Parse(DataSettingField.text), target);
                Manager.Network.ReserveDateWriteing(target.PointType, (ushort)target.Address, value);

                break;

            case nameof(EDataCardButtonType.ButtonTimeSet):
                if (DataSettingField == null)
                {
                    Debug.LogWarning($"[DataCard/{ObjectID}] Timer InputField가 없습니다.");
                    return;
                }

                if (!int.TryParse(DataSettingField.text,out int inputTimerValue))
                {
                    Debug.LogWarning( $"[DataCard/{ObjectID}] 잘못된 Timer 값: " + $"{DataSettingField.text}");
                    return;
                }

                if (inputTimerValue < 0)
                {
                    Debug.LogWarning($"[DataCard/{ObjectID}] Timer 값은 " + $"0 이상이어야 합니다.");
                    return;
                }

                // 실제 동작하지 않고 값만 저장
                timerValue = inputTimerValue;

                if (timerValue == 0)
                {
                    Debug.Log( $"[DataCard/{ObjectID}] Timer 해제 → 일반 Toggle 모드");
                }
                else
                {
                    Debug.Log($"[DataCard/{ObjectID}] Timer 설정 : " + $"{timerValue} → {timerValue / 100f:F2}초");
                }

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
            case EDataCategory.Toggle:
                UpdateValueText(data);
                break;
            case EDataCategory.Mode:
                if (DataUseableSet != null) DataUseableSet.isOn = data.Value > 0;
                break;
            default:
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

    private void UpdateValueText(Datas data, EDataCategory dataCategory = 0)
    {
        if (valueText == null && cardType != EDataCardType.MonitoringCard_Button)
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
        else if (cardType == EDataCardType.OutputData_Inverter)
        {
            if(dataCategory == EDataCategory.Toggle)
            {
                if (StateButton != null) StateButton.GetComponentInChildren<TMP_Text>().text = data.Value > 0 ? "Run" : "Stop";
                else Debug.LogWarning($"[DataCard - {data.Name}] State Button is Null");
            }
            else if(dataCategory == EDataCategory.Value)
            {
                valueText.text = data.Value.ToString($"F{decimalPoint}");
            }
            else
            {

                valueText.text = data.Value.ToString($"F{decimalPoint}");
            }
        }
        else if (cardType == EDataCardType.MonitoringCard_Button)
        {
            bool state = data.Value > 0;
            TurnLampState(MultiSlotImange[0], state);
        }
        else
        {
            valueText.text = data.Value.ToString($"F{decimalPoint}");
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
            MultiSlotValueTexts[index].text = data.Value.ToString($"F{decimalPoint}");
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

    #region Logging

    private Coroutine loggingCoroutine;

    private float integratedValue = 0f;

    private const float LoggingInterval = 0.5f;

    // Flow 시간 단위 환산값
    // /sec = 1
    // /min = 60
    // /hour = 3600
    private float TimeUnit = 60f;

    private void LoggingStartEventHandler()
    {
        if (!info.Values.Any(x => x != null && x.Type == EDataCategory.Inte))
            return;

        // 이미 실행 중이면 중복 실행 방지
        if (loggingCoroutine != null)
            return;

        loggingCoroutine = StartCoroutine(LoggingRoutine());

        Debug.Log($"[DataCard/{ObjectID}] Logging Coroutine Start");
    }

    private void LoggingStopEventHandler()
    {
        if (loggingCoroutine == null)
            return;

        StopCoroutine(loggingCoroutine);
        loggingCoroutine = null;

        Debug.Log($"[DataCard/{ObjectID}] Logging Coroutine Stop");
    }
    private IEnumerator LoggingRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(LoggingInterval);

        while (true)
        {
            yield return wait;

            //LoggingUpdate();
        }
    }
    /*
    public void OnDataUseableChanged(bool isOn)
    {
        integratedValue = 0f;

        InstrumentInfo inteInfo = info.Values.FirstOrDefault(x =>
            x != null &&
            x.Type == EDataCategory.Inte);

        if (inteInfo == null)
            return;

        Datas resetData = new Datas
        {
            Name = inteInfo.Tag,
            Value = 0f
        };

        // DataCard
        currentData[inteInfo.Tag] = resetData;

        // DataManager
        Manager.Data.UpdateData(
            inteInfo.Tag,
            resetData);

        // UI
        OnTextUpdate(
            resetData,
            EDataCategory.Inte);

        Debug.Log(
            $"[DataCard/{ObjectID}] " +
            $"적산 Reset → {inteInfo.Tag} = 0");
    }


    private void LoggingUpdate()
    {
        // =====================================================
        // 1. 적산 기능이 활성화된 상태인지 확인
        // 현재 요구사항: Toggle이 OFF일 때 적산
        // =====================================================
        if (DataUseableSet == null)
            return;

        if (DataUseableSet.isOn)
            return;


        // =====================================================
        // 2. 현재 Flow에 해당하는 Value 정보 찾기
        // =====================================================
        InstrumentInfo valueInfo = info.Values.FirstOrDefault(x =>
            x != null &&
            x.Type == EDataCategory.Value);

        if (valueInfo == null)
        {
            Debug.LogWarning(
                $"[DataCard/{ObjectID}] Value 타입 InstrumentInfo가 없습니다.");

            return;
        }


        // =====================================================
        // 3. currentData에서 현재 Flow 값 가져오기
        // =====================================================
        if (!currentData.TryGetValue(
                valueInfo.Tag,
                out Datas flowData))
        {
            Debug.LogWarning(
                $"[DataCard/{ObjectID}] Flow 데이터가 없습니다. " +
                $"Tag={valueInfo.Tag}");

            return;
        }


        // =====================================================
        // 4. Inte 정보 찾기
        // =====================================================
        InstrumentInfo inteInfo = info.Values.FirstOrDefault(x =>
            x != null &&
            x.Type == EDataCategory.Inte);

        if (inteInfo == null)
        {
            Debug.LogWarning(
                $"[DataCard/{ObjectID}] Inte 타입 InstrumentInfo가 없습니다.");

            return;
        }


        // =====================================================
        // 5. Flow 적산
        //
        // Flow × (경과시간 / Flow 시간단위)
        //
        // L/min이라면:
        // Flow × (0.5 / 60)
        //
        // Nm3/h라면:
        // Flow × (0.5 / 3600)
        // =====================================================
        float deltaValue =
            flowData.Value * (LoggingInterval / TimeUnit);

        integratedValue += deltaValue;


        // =====================================================
        // 6. Inte의 Tag를 Key로 Datas 생성
        // =====================================================
        Datas integratedData = new Datas
        {
            Name = inteInfo.Tag,
            Value = integratedValue
        };


        // =====================================================
        // 7. DataCard의 currentData 업데이트
        // =====================================================
        currentData[inteInfo.Tag] = integratedData;


        // =====================================================
        // 8. DataManager 업데이트
        // =====================================================

        // ↓ 이 부분은 DataManager의 실제 데이터 변경 함수명에
        // 맞춰서 사용해야 함.
        Manager.Data.UpdateData(
            inteInfo.Tag,
            integratedData);


        // =====================================================
        // 9. UI 업데이트
        // =====================================================
        OnTextUpdate(
            integratedData,
            EDataCategory.Inte);


        Debug.Log(
            $"[DataCard/{ObjectID}] Flow 적산\n" +
            $"Flow Tag={valueInfo.Tag}\n" +
            $"Flow={flowData.Value}\n" +
            $"Delta={deltaValue}\n" +
            $"Inte Tag={inteInfo.Tag}\n" +
            $"Integrated={integratedValue}");
    }*/
    #endregion

    #region Utility
    // TODO : Setting Field 작성 시 숫자만 찍히는 코드

    // TODO : Setting Field의 숫자를 Data To PLC로 Converting하는 코드
    private ushort ConvertDataToPLC(float setData, InstrumentInfo info)
    {
        ushort value = (ushort)(((setData - info.RangeMin) * (info.PLCMax - info.PLCMin) / (info.RangeMax - info.RangeMin)) + info.PLCMin);
        return value;
    }

    /// <summary>
    /// Lamp의 색은 Red, Green이다.
    /// </summary>
    /// <param name="state"></param>
    private void TurnLampState(Image img, bool state)
    {
        if (state) img.color = Color.green;
        else img.color = Color.red;
    }

    private IEnumerator TimerDOControl(InstrumentInfo target, int timerValue)
    {
        float waitSeconds = timerValue / 100f;

        // ON
        Manager.Network.ReserveDateWriteing(target.PointType, (ushort)target.Address, 1);

        Debug.Log( $"[{target.Tag}] Timer Start : " +  $"{timerValue} ({waitSeconds:F2}s)");

        yield return new WaitForSeconds(waitSeconds);

        // OFF
        Manager.Network.ReserveDateWriteing( target.PointType,(ushort)target.Address,0);

        Debug.Log( $"[{target.Tag}] Timer Finish → OFF");

        timerCoroutine = null;
    }

    #endregion
}
