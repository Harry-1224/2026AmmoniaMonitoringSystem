using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
public class ExperimentWrapper
{
    public int No { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public int Timer { get; set; }
    public List<ExperimentInfo> Experiments { get; set; }
    public EReservedExperimentState ReservedState { get; set; } = EReservedExperimentState.Reserved;
    public int CurrentProcess { get; set; } = 0;
    public int TotalProcess { get; set; } = 0;
    public float ProgressRatio
    {
        get
        {
            if (TotalProcess <= 0)
                return 0f;

            return (float)CurrentProcess / TotalProcess;
        }
    }
}

public class ExperimentInfo
{
    public int No { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Group { get; set; }
    public string Tag { get; set; }
    public string Action { get; set; }
    public int Value { get; set; }
    public int Process { get; set; }
}

public enum EExperimentStateMachine
{
    Idle,
    Running,
    Stopping,
    Shutdown,
    Resetting,
    Error
}

public enum  EReservedExperimentState
{
    Reserved,
    Processing, // 현재 진행중
    Resetting, // 종료 절차 진행중
    Failed, // 실험 실패
    Finished //예약된 실험 종료
}

public partial class ExperimentManager : ManagerBase
{
    // ExperimentManager
    //  - 싱글톤 패턴으로 구현하여 어디서든 접근 가능하도록 함
    //  - 전반적인 Control System 관리(오브젝트에서 온 제어 신호는 Experiment 메니저에서 판단 후 예약)
    //  - 실험 관리 시스템 구현, 실험 스케줄에 따라 실험 진행 및 관리

    protected override void Start()
    {
        base.Start();

        experimentDefines = Manager.Data.CallData<Dictionary<string, ExperimentWrapper>>();

        InitializeExperimentProcessData();

        //만약 저장된 실험 스케줄이 있다면 불러오기
        string savedPath = Path.Combine(Application.streamingAssetsPath,"Schedules","SavedSchedule.exsh");

        if (File.Exists(savedPath))
        {
            LoadSchedules(savedPath);

            Debug.Log($"[Experiment] Saved Schedule Load : {savedPath}");
        }
        else
        {
            Debug.Log("[Experiment] 저장된 Schedule 없음");
        }

        experimentRoutine = StartCoroutine(RunStateMachine());
    }
    protected override void Update()
    {
        base.Update();

        // 현재 진행 중인 스케줄이 없으면 종료
        if (experimentSchedules == null || experimentSchedules.Count == 0)
            return;

        // Processing 상태인 Schedule 찾기
        ExperimentWrapper processingSchedule = experimentSchedules.FirstOrDefault(x => x.ReservedState == EReservedExperimentState.Processing);

        if (processingSchedule != null)
        {
            // Ex_Start 확인
            if (!UpdatedDataForExperiment.TryGetValue("Ex_Start", out Datas exStart))
                return;

            // Ex_Reset 확인
            if (!UpdatedDataForExperiment.TryGetValue("Ex_Reset", out Datas exReset))
                return;

            // Ex_Start == 0 && Ex_Reset == 1
            if (exStart.Value == 0 && exReset.Value == 1)
            {
                processingSchedule.ReservedState = EReservedExperimentState.Resetting;

                ExperimentScheduleChange?.Invoke(
                    new List<ExperimentWrapper>(experimentSchedules)
                );
            }
            return;
        }

        /*
        // Stopping 상태인 Schedule 찾기
        ExperimentWrapper stoppingSchedule = experimentSchedules.FirstOrDefault(x => x.ReservedState == EReservedExperimentState.Resetting);

        if (stoppingSchedule != null)
        {
            // Ex_Start 확인
            if (!UpdatedDataForExperiment.TryGetValue("Ex_Start", out Datas exStart))
                return;

            // Ex_Reset 확인
            if (!UpdatedDataForExperiment.TryGetValue("Ex_Reset", out Datas exReset))
                return;

            // Ex_Start == 0 && Ex_Reset == 1
            if (exStart.Value == 0 && exReset.Value == 0)
            {
                stoppingSchedule.ReservedState = EReservedExperimentState.Finished;

                ExperimentScheduleChange?.Invoke(
                    new List<ExperimentWrapper>(experimentSchedules)
                );
            }
            return;
        }*/
    }
    private void OnApplicationQuit()
    {
        // 현재 진행중인 실험을 살리고, List중 결과가 Reserved인 것들만 저장
        // TODO : 개발중에는 잠깐 꺼놓을것.
        //SaveRemainSchedulesOnQuit();
    }

    protected override void EventSubscriber()
    {
        Manager.Data.OnDataChanged += DataChangeHandler;
        Manager.Network.OnNetworkConnected += HandleNetworkConnected;
    }
    protected override void EventUnsubscriber()
    {
        Manager.Data.OnDataChanged -= DataChangeHandler;
        Manager.Network.OnNetworkConnected -= HandleNetworkConnected;
    }

    private void InitializeExperimentProcessData()
    {
        var measurementData = Manager.Data.CallData<Dictionary<string, Datas>>();

        if (measurementData == null)
            return;

        UpdatedDataForExperiment = measurementData.Where(x =>
            x.Key.StartsWith("Experiment_Process_") ||
            x.Key == "Ex_Start" ||
            x.Key == "Ex_Stop" ||
            x.Key == "Ex_Reset" ||
            x.Key == "Ex_ESD")
            .ToDictionary(x => x.Key, x => x.Value);

        Debug.Log($"[Experiment] Process 데이터 초기화 : {UpdatedDataForExperiment.Count}");
    }

    #region Singleton
    public static ExperimentManager Instance { get; private set; }
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

    #region Monitoring System
    private void DataChangeHandler(Dictionary<string, Datas> data)
    {

        if (data == null)
            return;

        foreach (var item in data)
        {
            if (!UpdatedDataForExperiment.ContainsKey(item.Key))
                continue;

            UpdatedDataForExperiment[item.Key] = item.Value;
        }
    }
    #endregion

    #region Experiment Management System

    public void StartExperiment()
    {
        if (CurrentState != EExperimentStateMachine.Idle)
            return;

        if (experimentSchedules == null || experimentSchedules.Count == 0)
        {
            Debug.LogWarning("[Experiment] 실행할 Schedule이 없습니다.");
            return;
        }
        // TODO : CurrentExperimentIndex를 보고 현재까지 진행한 실험 판단 후 Index를 재설정할 것.
        //      1. 모든 예약된 실험을 보고 State가 Reserved인 것 부터 실험 시작.
        //      2. 만약 모든 실험이 끝나거나 실험이 종료 되어있다면, 오류 절차 실행

        startRequested = true;
    }

    public void Pause() => stopRequested = true;

    public void ESD() => shutdownRequested = true;

    public void ResetExperiment() => resetRequested = true;

    public List<ExperimentWrapper> CallCurrentSchedules() => experimentSchedules;
    public ExperimentWrapper CallSchedule(int num) => experimentSchedules[num];
    public ExperimentWrapper CallCurrentSchedule() => experimentSchedules[CurrentScheduleIndex];
    public int CallCurrentScheduleIndex() => CurrentScheduleIndex;
    public bool SaveSchedule(ExperimentWrapper schedule)
    {
        if (schedule == null)
            return false;

        // 같은 No를 가진 Schedule 찾기
        int index = experimentSchedules.FindIndex(x => x.No == schedule.No);

        // 기존 Schedule 수정
        if (index >= 0)
        {
            experimentSchedules[index] = schedule;

            Debug.Log($"[Experiment] Schedule 수정 : {schedule.No}");
        }
        // 새 Schedule 추가
        else
        {
            // No가 비정상이면 마지막 번호 자동 부여
            if (schedule.No <= 0)
            {
                schedule.No = experimentSchedules.Count + 1;
            }

            experimentSchedules.Add(schedule);

            Debug.Log($"[Experiment] Schedule 추가 : {schedule.No}");
        }

        if (ExperimentScheduleChange == null)
        {
            Debug.Log("[Experiment] Listener Count : 0");
        }
        else
        {
            Delegate[] listeners = ExperimentScheduleChange.GetInvocationList();

            Debug.Log($"[Experiment] Listener Count : {listeners.Length}");

            for (int i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];

                Debug.Log(
                    $"[{i}] " +
                    $"Target = {listener.Target}, " +
                    $"Type = {listener.Target?.GetType().Name}, " +
                    $"Method = {listener.Method.Name}"
                );
            }
        }

        // UI 및 Monitor 갱신 이벤트
        ExperimentScheduleChange?.Invoke(
            new List<ExperimentWrapper>(experimentSchedules)
        );

        return true;
    }
    public void RemoveSchedule(int no = -1)
    {

        int index = experimentSchedules.FindIndex(x => x.No == no);

        if (index < 0) return;

        bool isRunningState = CurrentState == EExperimentStateMachine.Running || CurrentState == EExperimentStateMachine.Stopping || waitPLCResponse;

        // 현재 실행 중이면 삭제 금지
        if (isRunningState && index == CurrentScheduleIndex)
        {
            Debug.LogWarning("현재 실행 중인 스케줄은 삭제 불가");
            return;
        }

        experimentSchedules.RemoveAt(index);

        //앞쪽 삭제 시 index 보정
        if (index < CurrentScheduleIndex)
            CurrentScheduleIndex--;

        for (int i = 0; i < experimentSchedules.Count; i++)
        {
            experimentSchedules[i].No = i + 1;
        }

        ExperimentScheduleChange?.Invoke(new List<ExperimentWrapper>(experimentSchedules));
    }
    public void ChangeSchedule(int from, int to)
    {
        if (from < 0 || from >= experimentSchedules.Count) return;
        if (to < 0 || to >= experimentSchedules.Count) return;

        //현재 진행 영역 건드리면 금지
        if (from <= CurrentScheduleIndex || to <= CurrentScheduleIndex)
        {
            Debug.LogWarning("진행 중인 영역은 순서 변경 불가");
            return;
        }

        var item = experimentSchedules[from];
        experimentSchedules.RemoveAt(from);
        experimentSchedules.Insert(to, item);

        ExperimentScheduleChange?.Invoke(new List<ExperimentWrapper>(experimentSchedules));
    }
    public void ClearSchedule()
    {
        if (CurrentState == EExperimentStateMachine.Running)
        {
            Debug.LogWarning("실험 중에는 전체 삭제 불가");
            return;
        }

        experimentSchedules.Clear();
        CurrentScheduleIndex = 0;

        ExperimentScheduleChange?.Invoke(new List<ExperimentWrapper>(experimentSchedules));
    }
    public bool SaveCurrentSchedules(List<ExperimentWrapper> Schedules = null, string fileName = null)
    {
        if(Schedules == null) return Manager.Data.SaveSchedulesToExsh(experimentSchedules, fileName);

        return Manager.Data.SaveSchedulesToExsh(Schedules, fileName);
    }

    public bool LoadSchedules(string filePath)
    {
        var loadedSchedules = Manager.Data.LoadSchedulesFromExsh(filePath);

        if (loadedSchedules == null)
            return false;

        experimentSchedules = loadedSchedules;
        CurrentScheduleIndex = 0;

        ExperimentScheduleChange?.Invoke(
            new List<ExperimentWrapper>(experimentSchedules)
        );

        return true;
    }
    /// <summary>
    /// 프로그램 종료 시 현재 진행중인 실험은 Reserved로 바꾸고 Reserved만 저장
    /// </summary>
    private void SaveRemainSchedulesOnQuit()
    {
        if (experimentSchedules == null || experimentSchedules.Count == 0)
            return;

        List<ExperimentWrapper> remainSchedules = experimentSchedules
            .Where(x => x != null &&
                        x.ReservedState != EReservedExperimentState.Finished
                        &&
                        x.ReservedState != EReservedExperimentState.Failed)
            .ToList();

        for (int i = 0; i < remainSchedules.Count; i++)
        {
            ExperimentWrapper schedule = remainSchedules[i];

            // Schedule 번호 재정렬
            schedule.No = i + 1;

            // 상태 초기화
            schedule.ReservedState = EReservedExperimentState.Reserved;

            // 진행률 초기화
            schedule.CurrentProcess = 0;
            schedule.TotalProcess = 0;
        }

        SaveCurrentSchedules(remainSchedules);
    }

    #endregion

    #region Network System
    private Coroutine networkRecoveryRoutine;

    /// <summary>
    /// 네트워크 연결시 실험에 필요한 통신 초기화를 담당하는 코드
    /// </summary>
    private void HandleNetworkConnected(string test)
    {
        // 만약에 네트워크 연결이 되었을 때 실험이 작동하고 있다면(Ex_Start가 1이라면), 취소 후 종료절차 실행 

        //1. 만약 상태머신이 꺼져있다면 켜질 때 까지 대기
        //2. 만약 Ex_Start의 상태값이 1일 경우 0으로 바꾸고 Pause실행
    }

    private void HandleNetworkConnected()
    {
        if (networkRecoveryRoutine != null)
            StopCoroutine(networkRecoveryRoutine);

        networkRecoveryRoutine = StartCoroutine(NetworkRecoveryRoutine());
    }
    private IEnumerator NetworkRecoveryRoutine()
    {
        yield return null;

        ResetStateMachineForNetworkRecovery();

        if (!UpdatedDataForExperiment.TryGetValue("Ex_Start", out Datas exStart))
        {
            Debug.LogWarning("[Network Recovery] Ex_Start 데이터 없음");
            networkRecoveryRoutine = null;
            yield break;
        }

        if (exStart.Value > 0)
        {
            Debug.LogWarning("[Network Recovery] PLC 실험 진행 감지 → 종료 절차 요청");

            resetRequested = true;
        }

        networkRecoveryRoutine = null;
    }
    private void ResetStateMachineForNetworkRecovery()
    {
        commandState = EExperimentStateMachine.Idle;

        startRequested = false;
        stopRequested = false;
        resetRequested = false;
        shutdownRequested = false;

        timeoutRunning = false;
        stateStartTime = 0f;
        stateTimeout = 0f;

        waitPLCResponse = false;
        isProcessing = false;
    }
    /*
    private IEnumerator NetworkRecoveryRoutine()
    {
        // 1. 상태머신 코루틴이 없으면 시작
        if (experimentRoutine == null)
        {
            experimentRoutine = StartCoroutine(RunStateMachine());
        }

        // 2. 상태머신이 완전히 준비될 때까지 1프레임 대기
        yield return null;

        // 3. Ex_Start 데이터 확인
        if (!UpdatedDataForExperiment.TryGetValue("Ex_Start", out Datas exStart))
        {
            Debug.LogWarning("[Network Recovery] Ex_Start 데이터 없음");
            yield break;
        }

        // 4. PLC가 이미 실험 중이면
        if (exStart.Value > 0)
        {
            Debug.LogWarning("[Network Recovery] PLC 실험 진행 감지 → Ex_Start OFF 후 종료 절차 진입");

            InstrumentInfo startInfo = Manager.Data.CallData<InstrumentInfo>("Ex_Start");

            if (startInfo == null)
            {
                Debug.LogError("[Network Recovery] Ex_Start Instrument 없음");
                SetState(EExperimentStateMachine.Error);
                yield break;
            }

            // Ex_Start = 0
            Manager.Network.ReserveDateWriteing(
                startInfo.PointType,
                (ushort)startInfo.Address,
                0
            );

            // 현재 스케줄이 없다면 첫 번째 스케줄 기준
            if (CurrentScheduleIndex < 0 && experimentSchedules.Count > 0)
                CurrentScheduleIndex = 0;

            // 상태머신을 종료 절차로 전환
            SetState(EExperimentStateMachine.Stopping);
        }

        networkRecoveryRoutine = null;
    }*/
    #endregion

}
