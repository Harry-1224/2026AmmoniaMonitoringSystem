using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Collections;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Rendering;
public class ExperimentWrapper
{
    public int No { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public int Timer { get; set; }
    public List<ExperimentInfo> Experiments { get; set; }
    public EReservedExperimentState ReservedState { get; set; } = EReservedExperimentState.Reserved;
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
    Processing,// 현재 진행중
    Stopping,// 종료 절차 진행중
    Failed,// 실험 실패
    Finished//예약된 실험 종료
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
                processingSchedule.ReservedState = EReservedExperimentState.Stopping;

                ExperimentScheduleChange?.Invoke(
                    new List<ExperimentWrapper>(experimentSchedules)
                );
            }
            return;
        }

        // Stopping 상태인 Schedule 찾기
        ExperimentWrapper stoppingSchedule = experimentSchedules.FirstOrDefault(x => x.ReservedState == EReservedExperimentState.Stopping);

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
        }
    }

    protected override void EventSubscriber()
    {
        Manager.Data.OnDataChanged += DataChangeHandler;
    }
    protected override void EventUnsubscriber()
    {
        Manager.Data.OnDataChanged -= DataChangeHandler;
    }

    private void InitializeExperimentProcessData()
    {
        var measurementData =
            Manager.Data.CallData<Dictionary<string, Datas>>();

        if (measurementData == null)
            return;

        UpdatedDataForExperiment = measurementData.Where(x =>
            x.Key.StartsWith("Experiment_Process_") ||
            x.Key == "Ex_Start" ||
            x.Key == "Ex_Stop" ||
            x.Key == "Ex_Reset")
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
        if (!isProcessing)
            return;

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
        if (CurrentState != EExperimentStateMachine.Idle) return;

        experimentRoutine = StartCoroutine(RunStateMachine());
    }

    public void Pause()
    {
        // 버튼으로 눌렀을 때 Stopping 상태로 변경
        // 버튼으로 정지했을 때 현재 진행중인 실험을 끝내고 Stoppoing 상태로 진입.
        // Stopping 절차가 완료 돼었을 때 Start대기.
        if (CurrentState == EExperimentStateMachine.Running) SetState(EExperimentStateMachine.Stopping);
    }

    public void Resume()
    {
        // 버튼으로 눌렀을 때 Running 상태로 변경
        // 버튼으로 재개했을 때 다음 진행중인 실험을 계속 진행하고 Running 상태로 진입.
        if (CurrentState == EExperimentStateMachine.Stopping) SetState(EExperimentStateMachine.Running);
    }

    public void ESD() =>SetState(EExperimentStateMachine.Shutdown);

    public void ResetExperiment() => SetState(EExperimentStateMachine.Resetting);

    public List<ExperimentWrapper> CallCurrentSchedules() => experimentSchedules;
    public ExperimentWrapper CallCurrentSchedule(int num) => experimentSchedules[num];
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

        // UI 및 Monitor 갱신 이벤트
        ExperimentScheduleChange?.Invoke(
            new List<ExperimentWrapper>(experimentSchedules)
        );

        return true;
    }
    public void RemoveSchedule(int index = -1)
    {
        if (index < 0 || index >= experimentSchedules.Count) return;

        //현재 실행 중이면 삭제 금지
        if (index == CurrentScheduleIndex)
        {
            Debug.LogWarning("현재 실행 중인 스케줄은 삭제 불가");
            return;
        }

        experimentSchedules.RemoveAt(index);

        //앞쪽 삭제 시 index 보정
        if (index < CurrentScheduleIndex)
            CurrentScheduleIndex--;

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
    public bool SaveCurrentSchedules(string fileName = null) => Manager.Data.SaveSchedulesToExsh( experimentSchedules, fileName);

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

    #endregion

}
