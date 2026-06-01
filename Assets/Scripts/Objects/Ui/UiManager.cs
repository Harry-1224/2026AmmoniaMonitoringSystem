using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.SceneManagement;
using UnityEngine;

public enum EUi
{
    HUD,
    MainLoading,
}

public enum EUiScreen
{
    Basic,
    PNID,
    Graph,
    Setting,
    Experiment,
}

public partial class UiManager : ManagerBase
{
    // UiManager
    //  - Main Canvas에 그려지는 UI를 담당.
    //  - 각 상황에 따라 적절한 UI를 표출.
    //  - 데이터 수집 시 

    
    private Transform canvasTransform;
    Dictionary<string, GameObject> UiGameObject = new Dictionary<string, GameObject>();
    Dictionary<string, GameObject> HUDScreen = new Dictionary<string, GameObject>();


    protected override void Intialize()
    {
        base.Intialize();
        InitCanvas();
        RegisterUIObjects();
        RegisterHUDScreens();
    }

    protected override void Update()
    {
        base.Update();
        while (dataQueue.TryDequeue(out var data))
        {
            ProcessData(data);
        }
    }

    protected override void EventSubscriber()
    {
        Manager.Data.OnDataChanged += OnDataChanged;
    }

    protected void OnDataChanged(object obj)
    {
        // DataManager에서 Data가 변경될 때마다 DataBox는 DataManager에서 Data를 받아와서 DataCard를 업데이트한다.
        if (obj == null || obj.GetType() != typeof(Dictionary<string, Datas>)) return;

        var data = (Dictionary<string, Datas>)obj;

        dataQueue.Enqueue(data);
    }

    #region Singleton
    public static UiManager Instance { get; private set; }
    protected override void SetSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
        }
        else
        {
            Destroy(gameObject); // 중복 방지
        }
    }
    #endregion

    #region UI Management System

    private ConcurrentQueue<Dictionary<string, Datas>> dataQueue = new ConcurrentQueue<Dictionary<string, Datas>>();

    private Dictionary<string, UiObjectBase> uiObjects = new Dictionary<string, UiObjectBase>();

    public void RegistUiObject(string key, UiObjectBase obj) => uiObjects[key] = obj;


    private void ProcessData(Dictionary<string, Datas> data)
    {
        foreach (var item in data)
        {
            /*
            switch (item.Value)
            {
                case ""
            }
            */
        }


    }
    private void InitCanvas()
    {
        GameObject canvas = GameObject.FindWithTag("MainCanvas");

        if (canvas == null)
        {
            Debug.LogError("[UiManager] Canvas 없음");
            return;
        }

        canvasTransform = canvas.transform;
    }
    private void RegisterUIObjects()
    {
        UiGameObject.Clear();

        var transforms = canvasTransform.GetComponentsInChildren<Transform>(true);

        // 이름 → 오브젝트 맵
        Dictionary<string, GameObject> map = new();

        foreach (var t in transforms)
        {
            if (!map.ContainsKey(t.name))
                map[t.name] = t.gameObject;
        }

        foreach (EUi uiType in System.Enum.GetValues(typeof(EUi)))
        {
            string name = uiType.ToString();

            if (!map.TryGetValue(name, out var obj))
            {
                Debug.LogWarning($"[UiManager] UI 오브젝트 없음: {name}");
                continue;
            }

            UiGameObject[name] = obj;
        }
    }
    private void RegisterHUDScreens()
    {
        HUDScreen.Clear();

        Monitor[] monitors = canvasTransform.GetComponentsInChildren<Monitor>(true);

        foreach (Monitor monitor in monitors)
        {
            EMonitorType screen = monitor.MonitorType;

            if (HUDScreen.ContainsKey(screen.ToString()))
            {
                Debug.LogWarning($"[UiManager] HUDScreen 중복: {screen}");
                continue;
            }

            HUDScreen[screen.ToString()] = monitor.gameObject;
        }
    }


    #endregion


    #region DataBox Methods

    public RectTransform MonitoringBox;
    public RectTransform ExperimentBox;

    [SerializeField] private float expandedWidth = 1400f;
    [SerializeField] private float collapsedWidth = 500f;


    private void ResizeDataBoxes(RectTransform expandBox, RectTransform collapseBox)
    {
        if (expandBox != null)
        {
            expandBox.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                expandedWidth
            );
        }

        if (collapseBox != null)
        {
            collapseBox.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                collapsedWidth
            );
        }
    }

    #endregion
}
