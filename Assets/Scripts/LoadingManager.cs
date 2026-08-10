using System;
using UnityEngine;

public class LoadingManager : ManagerBase
{
    // LoadingManager
    //  - 싱글톤 패턴으로 구현하여 어디서든 접근 가능하도록 함
    //  - 로딩 관리 시스템
    //  - 로딩 시작 시 OnStartLoading 이벤트 발생

    protected override void Start()
    {
        OnStartLoading();
    }

    #region Singleton
    public static LoadingManager Instance { get; private set; }

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

    #region Loading System

    public event Action<string> OnLoading;
    public event Action OnLoadingComplete;

    public void OnStartLoading()
    {
        //1. Data Load
        //Manager.Data.LoadDocument();

        //2. Network Loop 시작
        Manager.Network.StartNetworkLoop();

        //3. ExperimentManager 초기화
        //Manager.Experiment.StartExperiment();

        //3. UI 초기화
        Manager.Ui.UiSelect();
    }

    #endregion
}
