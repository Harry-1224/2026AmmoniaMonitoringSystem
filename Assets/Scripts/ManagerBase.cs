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
