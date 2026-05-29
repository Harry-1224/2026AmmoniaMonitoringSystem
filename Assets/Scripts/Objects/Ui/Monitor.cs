using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public enum EMonitorType
{
    Monitoring,
    Experiment,
    Setting
}

public enum EMonitorBtnFunc
{
    ExperimentStart,
    ExperimentStop,
    ExperimentESD,
    ExperimentSave,
    ExperimentReset,
    ExperimentDelete,
    ExperimentNew,
    ScheduleExport,
    ScheduleImport,
    Exit,
}

public class Monitor : UiObjectBase
{
    public EMonitorType MonitorType;

    protected override void Start()
    {
        base.Start();
        // MonitorType에 따라 필요한 컴포넌트나 데이터를 초기화
        switch (MonitorType)
        {
            case EMonitorType.Monitoring:
                // Monitoring 타입에 대한 초기화 로직
                break;
            case EMonitorType.Experiment:
                // Experiment 타입에 대한 초기화 로직
                break;
            case EMonitorType.Setting:
                // Setting 타입에 대한 초기화 로직
                break;
        }
    }

    protected override void Update()
    {
        switch (MonitorType)
        {
            case EMonitorType.Monitoring:
                // Monitoring 타입에 대한 초기화 로직
                break;
            case EMonitorType.Experiment:
                // Experiment 타입에 대한 초기화 로직

                // 1. Monitor Schedule 없음
                if (MonitorSchedule == null)
                    return;

                // 2. Processing / Stopping 상태만 허용
                bool isRunning =
                    MonitorSchedule.ReservedState == EReservedExperimentState.Processing
                    || MonitorSchedule.ReservedState == EReservedExperimentState.Stopping;

                ExperimnetUpdate(isRunning);

                break;
            case EMonitorType.Setting:
                // Setting 타입에 대한 초기화 로직
                break;
        }
    }

    // ObjectBase에서 상속받았으며, Enable 시점에서 작동.
    protected override void Intialize()
    {
        base.Intialize();
        // MonitorType에 따라 필요한 컴포넌트나 데이터를 초기화
        switch (MonitorType)
        {
            case EMonitorType.Monitoring:
                // Monitoring 타입에 대한 초기화 로직
                InitializeMonitoringCards();
                // 1. Monitor 내의 컨텐츠 (objects, texts 등)를 등록 및 초기화
                // 2. Monitor에 필요한 현재 데이터를 DataManager에서 로드한다.
                break;
            case EMonitorType.Experiment:
                // Experiment 타입에 대한 초기화 로직

                // 1. Type Dropdown의 옵션을 Manager.Experiment.experimentDefines의 Key값으로 등록하는 로직을 작성할 것. (기존 옵션은 ClearOptions()로 제거한 후 등록할 것.)
                Type.ClearOptions();
                List<string> options = new List<string>();
                options.Add("...");
                foreach (var item in Manager.Experiment.experimentDefines)
                {
                    options.Add(item.Key);
                }
                Type.AddOptions(options);

                Type.value = 0;
                Type.RefreshShownValue();

                // 2. 현재 예약된 Schedule의 갯수를 파악하고 ScheduleNo Dropdown의 옵션으로 등록하는 로직을 작성할 것. (기존 옵션은 ClearOptions()로 제거한 후 등록할 것, 처음은 "..."으로 등록 후 초기값은 0)
                ScheduleNo.ClearOptions();
                options.Clear();
                options.Add("...");
                foreach (var item in Manager.Experiment.CallCurrentSchedules())
                {
                    options.Add(item.No.ToString());
                }
                ScheduleNo.AddOptions(options);
                ScheduleNo.value = 0;
                ScheduleNo.RefreshShownValue();

                // 3. Monitor 내의 컨텐츠 (objects, texts 등)를 등록 및 초기화

                break;
            case EMonitorType.Setting:
                // Setting 타입에 대한 초기화 로직
                // 1. Setting에 필요한 데이터(Network 변수, InstrumentInfo)를 DataManager, NetworkManager 등에서 로드.
                break;
        }
    }
    protected override void EventSubscriber()
    {
        base.EventSubscriber();
        // MonitorType에 따라 필요한 이벤트 구독
        switch (MonitorType)
        {
            case EMonitorType.Monitoring:
                Manager.Data.OnDataChanged += OnDataChanged;
                break;
            case EMonitorType.Experiment:
                // Experiment 타입에 대한 이벤트 구독 로직
                Manager.Experiment.ExperimentScheduleChange += ScheduleChangerEventLisener;
                Manager.Data.OnDataChanged += OnDataChanged;
                break;
            case EMonitorType.Setting:
                // Setting 타입에 대한 이벤트 구독 로직
                break;
        }
    }
    protected override void EventUnsubscriber()
    {
        base.EventUnsubscriber();
        // MonitorType에 따라 필요한 이벤트 구독 해제
        switch (MonitorType)
        {
            case EMonitorType.Monitoring:
                Manager.Data.OnDataChanged -= OnDataChanged;
                break;
            case EMonitorType.Experiment:
                // Experiment 타입에 대한 이벤트 구독 해제 로직
                Manager.Experiment.ExperimentScheduleChange -= ScheduleChangerEventLisener;
                Manager.Data.OnDataChanged -= OnDataChanged;
                break;
            case EMonitorType.Setting:
                // Setting 타입에 대한 이벤트 구독 해제 로직
                break;
        }
    }


    protected override void OnDataChanged(Dictionary<string, Datas> obj)
    {
        // DataManager에서 데이터가 변경될 때마다 monitoringTexts에 등록된  컴포넌트들의 텍스트를 업데이트하는 로직
        switch (MonitorType)
        {
            case EMonitorType.Monitoring:

                break;
            case EMonitorType.Experiment:
                if (MonitorSchedule == null)
                    return;

                if (MonitorSchedule.ReservedState != EReservedExperimentState.Processing &&
                    MonitorSchedule.ReservedState != EReservedExperimentState.Stopping)
                    return;

                string key = "";

                if (MonitorSchedule.ReservedState == EReservedExperimentState.Processing)
                {
                    key = $"Experiment_Process_{MonitorSchedule.Group}";
                }
                else if (MonitorSchedule.ReservedState == EReservedExperimentState.Stopping)
                {
                    key = "Experiment_Process_End";
                }

                if (!obj.TryGetValue(key, out processData))
                {
                    processData = null;
                    Debug.LogWarning($"[Monitor] Process Data 없음: {key}");
                    return;
                }
                break;
            case EMonitorType.Setting:
                break;
        }
    }

    // Monitor 내의 버튼이 클릭되었을 때, MonitorType에 따라 적절한 행동을 하는 로직
    public void OnBtnClicked(string func)
    {
        switch (func)
        {
            case nameof(EMonitorBtnFunc.ExperimentStart):
                Manager.Experiment.StartExperiment();
                break;
            case nameof(EMonitorBtnFunc.ExperimentStop):
                Manager.Experiment.ESD();
                break;
            case nameof(EMonitorBtnFunc.ExperimentESD):
                //Manager.Experiment.;
                break;
            case nameof(EMonitorBtnFunc.ExperimentSave):
                Manager.Experiment.SaveSchedule(WrappingCurrentExperiment());
                break;
            case nameof(EMonitorBtnFunc.ExperimentReset):
                Manager.Experiment.ResetExperiment();
                break;
            case nameof(EMonitorBtnFunc.ExperimentDelete):
                Manager.Experiment.RemoveSchedule();
                break;
            case nameof(EMonitorBtnFunc.ExperimentNew):
                ClearExperimentMonitor();
                break;
            case nameof(EMonitorBtnFunc.ScheduleExport):
                // TODO: Implement schedule export functionality
                Manager.Experiment.SaveCurrentSchedules();
                break;
            case nameof(EMonitorBtnFunc.ScheduleImport):
                // TODO: Implement schedule import functionality
                Manager.Experiment.LoadSchedules("");
                break;
            case nameof(EMonitorBtnFunc.Exit):
                // TODO : Monitor창을 닫을 때 문제가 있을 때 문제를 알리기 위해 UiMnaager에 관련 데이터를 보내고 코루틴으로 대기하다 값을 받아와서 종료할지 말지 결정하는 로직을 작성할 것.
                break;
        }

    }


    public override void OnClick()
    {
        switch (MonitorType)
        {
            case EMonitorType.Monitoring:
            case EMonitorType.Experiment:

                Debug.Log($"[Monitor] 클릭됨 : {MonitorType}");

                Manager.Ui.OnDataBoxClicked(this);
                break;

        }
    }



    #region Data Monitoring
    private Dictionary<string, TextMeshProUGUI> monitoringTexts = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, DataCard> monitoringCards = new Dictionary<string, DataCard>();

    // monitoringTexts에 TextMeshProUGUI 컴포넌트를 등록하는 로직, Key는 Instrument의 Tag과 일치해야 함
    public void RegistText(TextMeshProUGUI text, string key) => monitoringTexts[key] = text;
    private void UpdateMonitoringText(string key, string value)
    {
        if (monitoringTexts.TryGetValue(key, out TextMeshProUGUI text))
        {
            text.text = value;
        }
    }
    private void InitializeMonitoringCards()
    {
        monitoringCards.Clear();

        Transform cardRoot = transform.Find("MonitoringCards");

        if (cardRoot == null)
        {
            Debug.LogError("[Monitor] MonitoringCards 오브젝트를 찾을 수 없습니다.");
            return;
        }

        DataCard[] cards = cardRoot.GetComponentsInChildren<DataCard>(true);

        foreach (DataCard card in cards)
        {
            string tagNo = card.gameObject.name.Trim();

            if (string.IsNullOrEmpty(tagNo))
                continue;

            if (monitoringCards.ContainsKey(tagNo))
            {
                Debug.LogWarning($"[Monitor] 중복 DataCard 이름 발견: {tagNo}");
                continue;
            }

            monitoringCards.Add(tagNo, card);
        }

        Debug.Log($"[Monitor] Monitoring DataCard 등록 완료: {monitoringCards.Count}개");
    }
    private void UpdateMonitoringCard(InstrumentInfo info)
    {
        string group = info.Group.Trim();

        if (!monitoringCards.TryGetValue(group, out DataCard card))
        {
            Debug.LogWarning($"[Monitor] Group과 매칭되는 DataCard 없음: {group}");
            return;
        }

        //card.UpdateCard(info);
    }


    #endregion

    #region Experiment Monitoring

    [Header("Experiment Monitor Components")]
    public TMP_Dropdown ScheduleNo;
    public TMP_InputField Name;
    public TMP_Dropdown Type;
    public TMP_InputField TimeOut;
    public Image experimentLampImage;

    public Transform Container;

    private Dictionary<string, DataCard> experimentDataCards = new Dictionary<string, DataCard>();
    private Datas processData;
    private int currentProcess;
    private int totalProcess;


    private string DefaultTimeout = "100";
    private int currentScheduleIndex = -1;
    private bool isProcessing = false;
    private ExperimentWrapper MonitorSchedule;


    private const string ExperimentInfoPrefabPath = "PreFab/UI/ExperimentInfo";

    private void ExperimnetUpdate(bool isRunning)
    {
        if (!isRunning)
            return;
        // =====================================
        // Lamp 색 변경
        // =====================================

        switch (MonitorSchedule.ReservedState)
        {
            case EReservedExperimentState.Processing:
                experimentLampImage.color = Color.green;
                break;

            case EReservedExperimentState.Stopping:
                experimentLampImage.color =
                    new Color(1f, 0.5f, 0f);
                break;

            case EReservedExperimentState.Failed:
                experimentLampImage.color = Color.red;
                break;

            case EReservedExperimentState.Finished:
                experimentLampImage.color = Color.black;
                break;
        }

        // =====================================
        // Process 진행률 계산
        // =====================================


        if (processData == null)
            return;

        int processValue = int.Parse(processData.Value.ToString());

        totalProcess = MonitorSchedule.Experiments.Count;
        currentProcess = CountCompletedBits(processValue, totalProcess);

        Debug.Log($"[Experiment] Progress : {currentProcess} / {totalProcess}"); // TODO : 추후 삭제할 것.
    }

    // - NOTE : ExperimentBox에서 선택된 실험을 화면에 셋팅
    public void SetExperimentMonitor(ExperimentWrapper selectedExperiment)
    {
        if (selectedExperiment == null)
            return;

        int scheduleIndex = ScheduleNo.options.FindIndex(option =>
        {
            if (int.TryParse(option.text, out int number))
            {
                return number == selectedExperiment.No;
            }

            return false;
        });

        if (scheduleIndex >= 0)
        {
            ScheduleNo.SetValueWithoutNotify(scheduleIndex);
        }

        Name.text = selectedExperiment.Name;

        int typeIndex = Type.options.FindIndex(option =>
            option.text == selectedExperiment.Group);

        if (typeIndex >= 0)
        {
            Type.SetValueWithoutNotify(typeIndex);
        }

        TimeOut.text = selectedExperiment.Timer.ToString();

        RefreshExperimentInfoCards(selectedExperiment.Experiments);
    }

    /// <summary>
    /// Type의 Dropdown이 변경되었을 때 호출되는 메서드
    /// </summary>
    public void SetExperimentType(int num)
    {
        if (num < 0 || num >= Type.options.Count)
            return;

        string selectedType = Type.options[num].text;

        // 현재 선택된 Schedule이 없으면 기본 정의 데이터 기준으로 카드 갱신
        if (ScheduleNo.value == 0)
        {
            if (!Manager.Experiment.experimentDefines.ContainsKey(selectedType))
            {
                Debug.LogWarning($"[ExperimentMonitor] 정의되지 않은 실험 타입: {selectedType}");
                RefreshExperimentInfoCards(null);
                return;
            }

            RefreshExperimentInfoCards(
                Manager.Experiment.experimentDefines[selectedType].Experiments
            );

            return;
        }

        // 현재 선택된 Schedule이 있으면 선택된 Schedule 기준으로 카드 갱신
        ExperimentWrapper currentSchedule = Manager.Experiment.CallCurrentSchedule(ScheduleNo.value - 1);

        if (currentSchedule == null)
        {
            Debug.LogWarning($"[ExperimentMonitor] Schedule 없음: {ScheduleNo.value}");
            RefreshExperimentInfoCards(null);
            return;
        }

        RefreshExperimentInfoCards(currentSchedule.Experiments);
    }

    // ScheduleNo Dropdown 값이 변경되었을 때 호출
    public void SetExperimentSchedule(int value)
    {
        if (ScheduleNo == null)
            return;

        if (value < 0 || value >= ScheduleNo.options.Count)
            return;

        string selectedText = ScheduleNo.options[value].text;

        if (selectedText == "...")
        {
            EnterNewScheduleMode(false);
            return;
        }

        if (!int.TryParse(selectedText, out int scheduleNo))
        {
            Debug.LogWarning($"[ExperimentMonitor] 잘못된 ScheduleNo: {selectedText}");
            return;
        }

        int scheduleIndex = scheduleNo - 1;

        ExperimentWrapper schedule =
            Manager.Experiment.CallCurrentSchedule(scheduleIndex);

        if (schedule == null)
        {
            Debug.LogWarning($"[ExperimentMonitor] Schedule 없음: {scheduleNo}");
            RefreshExperimentInfoCards(null);
            return;
        }

        currentScheduleIndex = scheduleIndex;
        MonitorSchedule = schedule;

        SetExperimentMonitor(schedule);
    }
    private void ClearExperimentMonitor()
    {
        if (ScheduleNo != null && ScheduleNo.options.Count > 0)
        {
            int newIndex = ScheduleNo.options.Count - 1;
            ScheduleNo.SetValueWithoutNotify(newIndex);
            ScheduleNo.RefreshShownValue();
        }

        EnterNewScheduleMode(true);
    }
    private void ScheduleChangerEventLisener(List<ExperimentWrapper> schedules)
    {
        if (schedules == null || schedules.Count == 0)
        {
            currentScheduleIndex = -1;
            MonitorSchedule = null;

            ScheduleNo.ClearOptions();

            List<string> emptyOption = new List<string>
            {
                "..."
            };

            ScheduleNo.AddOptions(emptyOption);
            ScheduleNo.SetValueWithoutNotify(0);

            return;
        }

        List<string> options = new List<string>();

        for (int i = 0; i < schedules.Count; i++)
        {
            options.Add((i + 1).ToString());
        }

        // 새 Schedule 추가용 옵션
        options.Add("...");

        ScheduleNo.ClearOptions();
        ScheduleNo.AddOptions(options);

        // 현재 선택값 보정
        if (currentScheduleIndex < 0 || currentScheduleIndex >= schedules.Count)
        {
            currentScheduleIndex = 0;
        }

        ScheduleNo.SetValueWithoutNotify(currentScheduleIndex);
        ScheduleNo.RefreshShownValue();
    }
    private ExperimentWrapper WrappingCurrentExperiment()
    {
        List<ExperimentInfo> experiments = new List<ExperimentInfo>();

        int number = 0;

        string selectedScheduleText = ScheduleNo.options[ScheduleNo.value].text;

        // 현재 Schedule No가 "..."이면 새 Schedule로 추가
        if (selectedScheduleText == "...")
        {
            number = Manager.Experiment.CallCurrentSchedules().Count + 1;
        }
        else
        {
            int.TryParse(selectedScheduleText, out number);
        }

        foreach (var item in experimentDataCards)
        {
            if (!item.Value.gameObject.activeSelf)
                continue;

            experiments.Add(item.Value.GetExperimentInfo());
        }

        return new ExperimentWrapper
        {
            No = number,
            Name = Name.text,
            Group = Type.options[Type.value].text,
            Timer = int.Parse(TimeOut.text),
            Experiments = experiments
        };
    }
    private void RefreshExperimentInfoCards(List<ExperimentInfo> infos)
    {
        if (infos == null || infos.Count == 0)
        {
            Debug.LogWarning("[DataBox] 생성할 Experiment Information이 없습니다.");

            foreach (var card in experimentDataCards.Values)
                card.gameObject.SetActive(false);

            RebuildExperimentInfoLayout();
            return;
        }

        Dictionary<string, int> actionCounts = new Dictionary<string, int>();
        HashSet<string> requiredKeys = new HashSet<string>();

        int visualIndex = 0;

        foreach (ExperimentInfo info in infos)
        {
            if (info == null)
                continue;

            string action = info.Action;

            if (!actionCounts.ContainsKey(action))
                actionCounts[action] = 0;

            int actionIndex = actionCounts[action]++;
            string key = GetExperimentInfoKey(info, actionIndex);

            requiredKeys.Add(key);

            if (!experimentDataCards.TryGetValue(key, out DataCard card))
            {
                card = CreateExperimentInfoCard(info, key);

                if (card == null)
                    continue;
            }

            card.gameObject.SetActive(true);
            card.transform.SetSiblingIndex(visualIndex);
            visualIndex++;

            card.ObjectID = key;
            card.cardType = EDataCardType.ExperimentData;
            card.ExperimentdataSetting(info);
        }

        foreach (var pair in experimentDataCards)
        {
            if (!requiredKeys.Contains(pair.Key))
                pair.Value.gameObject.SetActive(false);
        }

        RebuildExperimentInfoLayout();
    }
    private string GetExperimentInfoKey(ExperimentInfo info, int actionIndex)
    {
        return $"Experiment_{info.Action}_{actionIndex}";
    }
    private string GetExperimentInfoPrefabPath(ExperimentInfo info)
    {
        switch (info.Action)
        {
            case "End":
            case "None":
                return "PreFab/UI/ExperimentInfo";

            default:
                return $"PreFab/UI/ExperimentInfo_{info.Action}";
        }
    }
    private DataCard CreateExperimentInfoCard(ExperimentInfo info, string key)
    {
        if (Container == null)
        {
            Debug.LogError("[DataBox] Container가 null입니다.");
            return null;
        }

        string path = GetExperimentInfoPrefabPath(info);

        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogWarning($"[DataBox] Experiment Prefab Load 실패: {path}");
            return null;
        }

        GameObject obj = Instantiate(prefab, Container, false);
        obj.name = key;

        DataCard card = obj.GetComponent<DataCard>();

        if (card == null)
        {
            Debug.LogWarning($"[DataBox] {key}에 DataCard 스크립트가 없습니다.");
            Destroy(obj);
            return null;
        }

        card.ObjectID = key;
        card.cardType = EDataCardType.ExperimentData;
        card.RegistBox(this);

        experimentDataCards[key] = card;

        return card;
    }
    private void RebuildExperimentInfoLayout()
    {
        if (Container == null)
            return;

        RectTransform rect = Container.GetComponent<RectTransform>();

        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    /*
    private void RefreshExperimentInfoCards(List<ExperimentInfo> infos)
    {
        if (infos == null || infos.Count == 0)
        {
            Debug.LogWarning("[DataBox] 생성할 Experiment Information이 없습니다.");

            EnsureExperimentInfoCards(0);
            return;
        }

        EnsureExperimentInfoCards(infos.Count);
        ApplyExperimentInfoCards(infos);
    }
    private void EnsureExperimentInfoCards(int requiredCount)
    {
        if (Container == null)
        {
            Debug.LogError("[DataBox] Container가 null입니다.");
            return;
        }

        string path = "PreFab/UI/ExperimentInfo";
        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogWarning($"[DataBox] Experiment Prefab Load 실패: {path}");
            return;
        }

        int currentCount = experimentDataCards.Count;

        // 부족한 카드 생성
        for (int i = currentCount; i < requiredCount; i++)
        {
            string key = $"Experiment_{i}";

            GameObject obj = Instantiate(prefab, Container, false);
            obj.name = key;

            DataCard card = obj.GetComponent<DataCard>();

            if (card == null)
            {
                Debug.LogWarning("[DataBox] ExperimentDataCard에 DataCard 스크립트가 없습니다.");
                Destroy(obj);
                continue;
            }

            card.ObjectID = key;
            card.cardType = EDataCardType.ExperimentData;
            card.RegistBox(this);

            experimentDataCards[key] = card;
        }

        // 필요한 개수보다 많은 카드는 비활성화
        for (int i = 0; i < experimentDataCards.Count; i++)
        {
            string key = $"Experiment_{i}";

            if (!experimentDataCards.TryGetValue(key, out DataCard card))
                continue;

            card.gameObject.SetActive(i < requiredCount);
        }
    }
    private void ApplyExperimentInfoCards(List<ExperimentInfo> infos)
    {
        if (infos == null)
            return;

        for (int i = 0; i < infos.Count; i++)
        {
            string key = $"Experiment_{i}";

            if (!experimentDataCards.TryGetValue(key, out DataCard card))
            {
                Debug.LogWarning($"[DataBox] DataCard 없음: {key}");
                continue;
            }

            ExperimentInfo info = infos[i];

            if (info == null)
            {
                card.gameObject.SetActive(false);
                continue;
            }

            card.gameObject.SetActive(true);
            card.ObjectID = key;
            card.cardType = EDataCardType.ExperimentData;

            card.ExperimentdataSetting(info);
        }
    }*/
    private void EnterNewScheduleMode(bool resetType)
    {
        currentScheduleIndex = -1;
        MonitorSchedule = null;

        Name.text = "";
        TimeOut.text = DefaultTimeout;

        if (resetType && Type != null && Type.options.Count > 0)
        {
            Type.SetValueWithoutNotify(0);
            Type.RefreshShownValue();
        }

        LoadDefaultExperimentCardsByCurrentType();
    }
    private void LoadDefaultExperimentCardsByCurrentType()
    {
        if (Type == null || Type.options.Count == 0)
        {
            RefreshExperimentInfoCards(null);
            return;
        }

        string selectedType = Type.options[Type.value].text;

        if (Manager.Experiment.experimentDefines.ContainsKey(selectedType))
        {
            RefreshExperimentInfoCards(
                Manager.Experiment.experimentDefines[selectedType].Experiments
            );
        }
        else
        {
            RefreshExperimentInfoCards(null);
        }
    }
    private int CountCompletedBits(int value, int maxStepCount)
    {
        int count = 0;

        for (int i = 0; i < maxStepCount; i++)
        {
            if ((value & (1 << i)) != 0)
            {
                count++;
            }
        }

        return count;
    }
    #endregion

    #region Setting


    #endregion
}