using UnityEditor;
using UnityEngine;

public class ManagerBase : MonoBehaviour
{
    protected SceneControlManager sceneControlManager;
    private bool isInitialized = false;

    protected virtual void Awake()
    {
        SetSingleton();
        Intialize();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        EventSubscriber();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        
    }

    protected virtual void OnEnable()
    {

    }

    protected virtual void OnDisable()
    {
        EventUnsubscriber();
    }

    /// <summary>
    /// Manager를 초기화하는 코드 Object들과 다르게 Awake에서 초기화하는 이유는 Manager는 게임 전체에서 하나만 존재하기 때문에, 다른 Object들이 Manager의 초기화가 완료된 후에 접근할 수 있도록 하기 위함이다.
    /// </summary>
    protected virtual void Intialize()
    {

    }

    protected virtual void EventSubscriber()
    {

    }
    protected virtual void EventUnsubscriber()
    {

    }

    #region Singleton
    protected virtual void SetSingleton()
    {

    }

    #endregion

    public virtual void SetSceneControlManager(SceneControlManager sceneManager) => sceneControlManager = sceneManager;

    protected virtual void OnErrorLogged(string ClassName, string methodName,string errorMessage) => Debug.LogError($"{ClassName} / {methodName}  :  {errorMessage}");
}
