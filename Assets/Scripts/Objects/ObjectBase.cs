using UnityEngine;

public static class Manager
{
    public static DataManager Data => DataManager.Instance;
    public static ExperimentManager Experiment => ExperimentManager.Instance;
    public static LoggingManager Logging => LoggingManager.Instance;
    public static NetworkManager Network => NetworkManager.Instance;
    public static UiManager Ui => UiManager.Instance;
}


public class ObjectBase : MonoBehaviour, IObject
{
    private bool isInitialized = false;

    [Header("Object Setting")]
    public string ObjectID;

    protected virtual void Awake()
    {
    }

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {
        
    }
    protected virtual void OnEnable()
    {
        if(!isInitialized) Initialize();
        EventSubscriber();
    }
    protected virtual void OnDisable()
    {
        EventUnsubscriber();
    }

    /// <summary>
    /// OnEnable 시점에서 작동하며, Object의 종류에 따라 필요한 컴포넌트나 데이터를 초기화한다.
    /// </summary>
    protected virtual void Initialize()
    {
        isInitialized = true;
    }
    protected virtual void ReInitialize()
    {
        Initialize();
    }

    public virtual void OnFunctionCalled(object obj = null)
    {

    }
    protected virtual void EventSubscriber()
    {
        // 1. DataManager
        // 2. NetworkManager
    }
    protected virtual void EventUnsubscriber()
    {
        // 1. DataManager
        // 2. NetworkManager
    }
}
