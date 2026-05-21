using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;

/// <summary>
/// DataBox의 Type을 구분하는 Enum
/// </summary>
public enum EDataBoxType
{
    Monitoring,
    Control,
    Experiment
} 
public enum EInsturmentType
{
    Sensor_Analog,
    Sensor_Digital,
    Valve_Digital,
    Inverter,
    MFC,
}
public class DataBox : UiObjectBase
{
    #region DataBox 관련
    // DataBox
    //  - NOTE : DataBox는 InputData를 보는 Monitoring Box, Output Data를 제어하는 Control Box, 실험의 정보를 보여주는 Experiment Info Box로 나뉜다.
    //  - NOTE : 각 Box의 Type은 변수 EDataBoxType으로 구분한다.
    //  - NOTE : 각 EDataBoxType에 따라 보여지는 DataCard가 MonitoringDataCard, ControlDataCard, ExperimentDataCard로 달라진다.
    //  - NOTE : DataBox는 초기화 시 IoInfo에서 DataCard의 정보를 받아와서 DataCard를 생성한다.(생성된 DataCard를 Dictionary로 관리한다.)
    //  - NOTE : DataCard 생성 시 Prefab을 이용하여 생성한다.
    //  - NOTE : DataManager에서 Data가 변경될 때마다 DataBox는 DataManager에서 Data를 받아와서 DataCard를 업데이트한다.

    public EDataBoxType dataBoxType;
    public Transform content; // DataCard가 생성될 부모 오브젝트
    private Dictionary<string, DataCard> dataCards = new Dictionary<string, DataCard>();
    private Dictionary<string, InstrumentInfo> instruments = new();
    private List<ExperimentWrapper> schedules = new List<ExperimentWrapper>();

    public Transform Container; // ScrollView의 Content오브젝트

    private bool isSherinkged = false; // DataBox가 최소화 상태인지 여부를 나타내는 변수

    [SerializeField] private GameObject scrollViewObject;
    [SerializeField] private float expandedHeight = 600f;
    [SerializeField] private float shrinkHeight = 80f;

    private RectTransform rectTransform;

    protected override void Update()
    {
        base.Update();
    }

    protected override void Intialize()
    {
        base.Intialize();

        rectTransform = GetComponent<RectTransform>();

        if (scrollViewObject == null)
            scrollViewObject = transform.Find("Scroll View").gameObject;

        if (Container == null)
        {
            Container = transform.Find("Scroll View/Viewport/Content");
        }

        if (Container == null)
        {
            Debug.LogError("[DataBox] Container를 찾을 수 없습니다. Inspector에 직접 할당하세요.");
            return;
        }
        switch (dataBoxType)
        {
            case EDataBoxType.Monitoring :
                // - NOTE : Monitoring Box는 DataManager에서 IoInfo를 받아와서 MonitoringDataCard를 생성한다.
                // - NOTE : 받아온 IoInfo중 dataBoxType과 Function이 "Monitoring"인 경우 MonitoringDataCard를 생성한다.

                // 1. Control Box에서 ControlDataCard를 생성하기 위해 DataManager에서 IoInfo를 받아온다.
                var tempMonitor = Manager.Data.CallData<Dictionary<string, InstrumentInfo>>(dataBoxType.ToString());
                instruments = tempMonitor
                    .Where(x => x.Value != null 
                    &&  x.Value.Useable 
                    && x.Value.Function 
                    == dataBoxType.ToString())
                    .ToDictionary(x => x.Key, x => x.Value);//관련 Info만 추출

                if (instruments == null)
                {
                    Debug.LogError($"[DataBox] {dataBoxType}에 대한 InstrumentInfo를 찾을 수 없습니다.");
                    return;
                }// 예외처리

                // 2. Control Box에서 ControlDataCard를 생성한다.
                OnDataCardGenerated(dataBoxType.ToString(), instruments);

                break;
            case EDataBoxType.Control:
                // - NOTE : Control Box는 DataManager에서 IoInfo를 받아와서 ControlDataCard를 생성한다.
                // - NOTE : 받아온 IoInfo중 dataBoxType과 Function이 "Control"인 경우 ControlDataCard를 생성한다.

                // 1. Control Box에서 ControlDataCard를 생성하기 위해 DataManager에서 IoInfo를 받아온다.
                var tempControl = Manager.Data.CallData<Dictionary<string, InstrumentInfo>>(dataBoxType.ToString());
                instruments = tempControl
                    .Where(x => x.Value != null
                    && x.Value.Useable
                    && x.Value.Function
                    == dataBoxType.ToString())
                    .ToDictionary(x => x.Key, x => x.Value);//관련 Info만 추출

                if (instruments == null)
                {
                    Debug.LogError($"[DataBox] {dataBoxType}에 대한 InstrumentInfo를 찾을 수 없습니다.");
                    return;
                }// 예외처리

                // 2. Control Box에서 ControlDataCard를 생성한다.
                OnDataCardGenerated(dataBoxType.ToString(), instruments);

                break;
            case EDataBoxType.Experiment:
                // - NOTE : Experiment Box에서 InstrumentInfo는 진행중인 실험의 진행도를 파악하기 위해 사용된다.
                // - NOTE : Experiment Box는 ExperimentManager에서 실험 Schedule을 받아와서 순서대로 ExperimentDataCard를 생성한다.

                // 1. 실험 진행도 파악에 사용될 InstrumentInfo를 받아온다. (실험 진행도는 IoInfo의 Value값을 이용하여 표현한다.)
                var tempExper = Manager.Data.CallData<Dictionary<string, InstrumentInfo>>(dataBoxType.ToString());
                instruments = tempExper
                    .Where(x => x.Value != null
                    && x.Value.Useable
                    && x.Value.Function
                    == dataBoxType.ToString())
                    .ToDictionary(x => x.Key, x => x.Value);//실험의 진행도를 파악하기 위해 사용되는 InsturmentInfo

                // 2. 실험 Schedule을 받아와서 순서대로 ExperimentDataCard를 생성한다.
                schedules = Manager.Experiment.CallCurrentSchedules(); //실험 Schedule을 받아오는 함수 호출
                OnExperimentDataCardGenerated(schedules);

                break;
        }

        return;
    }

    protected override void EventSubscriber()
    {
        if (dataBoxType == EDataBoxType.Experiment) Manager.Experiment.ExperimentScheduleChange += OnExperimentScheduleChanged;
        Manager.Data.OnDataChanged += OnDataChanged;
    }

    protected override void EventUnsubscriber()
    {
        if (dataBoxType == EDataBoxType.Experiment) Manager.Experiment.ExperimentScheduleChange -= OnExperimentScheduleChanged; 
        Manager.Data.OnDataChanged -= OnDataChanged;
    }

    protected override void OnDataChanged(Dictionary<string, Datas> datas)
    {
        // DataManager에서 Data가 변경될 때마다 DataBox는 DataManager에서 Data를 받아와서 DataCard를 업데이트한다.
        foreach(var item in instruments)
        {
            string key = item.Key;
            if (datas.ContainsKey(key))
            {
                Datas data = datas[key];
                if(dataBoxType != EDataBoxType.Experiment) dataCards[item.Value.Group].OnFunctionCalled(data);
            }
        }
    }

    public override void OnClick()
    {
        base.OnClick();
        if (dataBoxType == EDataBoxType.Experiment) return;

        ToggleBox();
    }

    /// <summary>
    /// DataCard가 클릭되었을 때 Card에서 호출되는 함수,
    /// Monitoring, Control에서는 Data의 Group을 이용, Experiment에서는 Schedule의 key를 이용하여 어떤 DataCard가 클릭되었는지 판단한다.
    /// </summary>
    /// <param name="cardId">Monitoring, Control -> Data Group, Experiment -> Schedule Key</param>
    public void OnClickedDataCard(EDataCardType cardType, string cardId)
    {
        if (dataBoxType == EDataBoxType.Experiment)
        {
            // TODO : ExperimentDataCard가 클릭 되었을 때 Experiment Monitor를 활성화 시키고 현재 클릭된 Schedule의 상세 정보를 보여주는 기능 추가 필요

            // 1. Experiment Monitor 활성화
            Manager.Ui.OnMonitorChanged("Experiment");

            // 2. 클릭된 Schedule의 상세 정보를 보여주는 기능 추가 (예: 새로운 UI 패널, 팝업 등)
            Manager.Ui.SetExperimentMonitor(dataCards[cardId].experimentSchedule);
        }
        else
        {
            // TODO : DataCard가 클릭 되었을 때 3D View에서 해당 System으로 화면을 옮기는 기능 추가 필요
        }
    }

    private void OnDataCardGenerated(string type, Dictionary<string, InstrumentInfo> instruments)
    {
        if (Container == null)
        {
            Debug.LogError("[DataBox] Container가 null입니다.");
            return;
        }

        foreach (Transform child in Container)
        {
            Destroy(child.gameObject);
        }

        dataCards.Clear();

        foreach (var item in instruments)
        {
            InstrumentInfo info = item.Value;

            if (info == null) continue;
            if (!info.Useable) continue;
            if (info.Function != type) continue; 
            if (dataCards.TryGetValue(info.Group, out DataCard existingCard))
            {
                //만약 같은 Group의 DataCard가 이미 존재한다면, 해당 DataCard를 업데이트한다.
                existingCard.Intialize(info);
                continue;
            }

            string prefabName = info.InstrumentType.ToString();
            string path = $"PreFab/UI/{prefabName}";

            GameObject prefab = Resources.Load<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning($"[DataBox] Prefab Load 실패: {path}");
                continue;
            }

            GameObject obj = Instantiate(prefab, Container);
            obj.name = info.Group;

            DataCard card = obj.GetComponent<DataCard>();

            if (card == null)
            {
                Debug.LogWarning($"[DataBox] DataCard 스크립트가 없습니다: {prefabName}");
                Destroy(obj);
                continue;
            }

            card.Intialize(info);
            card.RegistBox(this);
            dataCards[info.Group] = card;
        }
    }
    private void OnExperimentDataCardGenerated(List<ExperimentWrapper> schedules)
    {
        if (Container == null)
        {
            Debug.LogError("[DataBox] Container가 null입니다.");
            return;
        }

        foreach (Transform child in Container)
        {
            Destroy(child.gameObject);
        }

        dataCards.Clear();

        if (schedules == null || schedules.Count == 0)
        {
            Debug.LogWarning("[DataBox] 생성할 Experiment Schedule이 없습니다.");
            return;
        }

        string path = "PreFab/UI/Experiment";

        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogWarning($"[DataBox] Experiment Prefab Load 실패: {path}");
            return;
        }

        for (int i = 0; i < schedules.Count; i++)
        {
            ExperimentWrapper schedule = schedules[i];

            if (schedule == null) continue;

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

            // 최소 구조: ObjectID만 Schedule용 key로 설정
            card.ObjectID = key;

            // 필요하면 DataCard에 Experiment 전용 초기화 함수 추가
            // card.Intialize(schedule, instruments);
            card.ExperimentScheduleSetting(schedule);

            card.RegistBox(this);
            dataCards[key] = card;
        }
    }

    private void ToggleBox()
    {
        isSherinkged = !isSherinkged;

        // ScrollView 활성/비활성
        scrollViewObject.SetActive(!isSherinkged);

        // Box 크기 변경
        Vector2 size = rectTransform.sizeDelta;

        size.y = isSherinkged
            ? shrinkHeight
            : expandedHeight;

        rectTransform.sizeDelta = size;
    }


    #endregion

    #region MonitoringBox 관련
    public void OnMonitoringMonitor()
    {
        // 만약 현재 DataBox의 Type이 Monitoring이고, 최소화 상태이면 최대화 상태로 변경한다.
        if (dataBoxType == EDataBoxType.Monitoring && isSherinkged)
        {
            isSherinkged = false;
            // 최대화 상태로 변경하는 로직 추가
        }
        Manager.Ui.OnMonitorChanged("Monitoring"); 
    }
    #endregion

    #region ControlBox 관련

    #endregion

    #region ExperimentBox 관련

    // NOTE : Experiment Box가 최소, 최대로 변경될 때 사용된다.
    public void OnExperimentMonitor() 
    {
        // 만약 현재 DataBox의 Type이 Experiment이고, 최소화 상태이면 최대화 상태로 변경한다.
        if (dataBoxType == EDataBoxType.Experiment && isSherinkged)
        {
            isSherinkged = false;
            // 최대화 상태로 변경하는 로직 추가
        }
        Manager.Ui.OnMonitorChanged("Experiment");
    }
    private void OnExperimentScheduleChanged(List<ExperimentWrapper> newSchedules)
    {
        if (dataBoxType != EDataBoxType.Experiment) return;

        if (Container == null)
        {
            Debug.LogError("[DataBox] Container가 null입니다.");
            return;
        }

        schedules = newSchedules ?? new List<ExperimentWrapper>();

        string path = "PreFab/UI/Experiment";
        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogWarning($"[DataBox] Experiment Prefab Load 실패: {path}");
            return;
        }

        int scheduleCount = schedules.Count;
        int cardCount = dataCards.Count;

        // 1. DataCard가 부족하면 새로 생성
        for (int i = cardCount; i < scheduleCount; i++)
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
            dataCards[key] = card;
        }

        // 2. DataCard가 많으면 초과분 비활성화
        for (int i = 0; i < dataCards.Count; i++)
        {
            string key = $"Experiment_{i}";

            if (!dataCards.TryGetValue(key, out DataCard card)) continue;

            bool isActive = i < scheduleCount;
            card.gameObject.SetActive(isActive);
        }

        // 3. 스케줄 순서대로 DataCard 업데이트
        for (int i = 0; i < scheduleCount; i++)
        {
            string key = $"Experiment_{i}";

            if (!dataCards.TryGetValue(key, out DataCard card)) continue;

            ExperimentWrapper schedule = schedules[i];

            card.gameObject.SetActive(true);
            card.ObjectID = key;
            card.RegistBox(this);
            card.transform.SetSiblingIndex(i);

            // TODO: ExperimentDataCard 전용 업데이트 함수가 필요함
            // 예시:
            // card.Intialize(schedule);
            // card.OnExperimentScheduleUpdated(schedule);
            card.ExperimentScheduleSetting(schedule);
        }
    }

    #endregion
}
