using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class ExperimentManager
{
    // ExperimentManger의 내부 함수를 모아놓은 파일

    public int CurrentScheduleIndex { get; private set; } = 0;
    public EExperimentStateMachine CurrentState { get; private set; } = EExperimentStateMachine.Idle;
    public bool isProcessing { get; private set; }

    private Coroutine experimentRoutine; //실험을 진행하는 Coroutine을 저장하는 변수 
    private List<ExperimentWrapper> experimentSchedules = new List<ExperimentWrapper>();
    public Dictionary<string, ExperimentWrapper> experimentDefines = new Dictionary<string, ExperimentWrapper>();
    private Dictionary<string, Datas> UpdatedDataForExperiment = new Dictionary<string, Datas>();

    public event Action<EExperimentStateMachine> ExperimentStateChange;
    public event Action<List<ExperimentWrapper>> ExperimentScheduleChange;

    private IEnumerator RunStateMachine()
    {
        SetState(EExperimentStateMachine.Running);
        isProcessing = true;

        while (true)
        {
            switch (CurrentState)
            {
                case EExperimentStateMachine.Running:
                    yield return StartCoroutine(RunCurrentScheduleFlow());
                    break;

                case EExperimentStateMachine.Stopping:
                    yield return StartCoroutine(StopRoutine());
                    // CurrentScheduleIndex 증가 및 다음 스케줄로 이동
                    MoveNextSchedule();
                    break;

                case EExperimentStateMachine.Shutdown:
                    yield return StartCoroutine(ShutdownRoutine());
                    yield break;

                case EExperimentStateMachine.Resetting:
                    yield return StartCoroutine(ResetRoutine());
                    SetState(EExperimentStateMachine.Idle);
                    break;

                case EExperimentStateMachine.Error:
                    yield return StartCoroutine(ErrorRoutine());
                    yield break;

                case EExperimentStateMachine.Idle:
                    isProcessing = false;
                    SetState(CurrentState);
                    yield break;
            }

            SetState(CurrentState);
            yield return null;
        }
    }
    private IEnumerator RunCurrentScheduleFlow()
    {
        if (!Manager.Network.isConnected)
        {
            Debug.LogError("Network not connected");
            SetState(EExperimentStateMachine.Error);
            yield break;
        }

        if (CurrentScheduleIndex < 0 || CurrentScheduleIndex >= experimentSchedules.Count)
        {
            SetState(EExperimentStateMachine.Idle);
            yield break;
        }

        ExperimentWrapper schedule = experimentSchedules[CurrentScheduleIndex];

        Debug.Log($"[Schedule] Start: {schedule.Name}");

        yield return StartCoroutine(RunSchedule(schedule));

        if (CurrentState == EExperimentStateMachine.Shutdown || CurrentState == EExperimentStateMachine.Resetting || CurrentState == EExperimentStateMachine.Error)
        {
            yield break;
        }

        SetState(EExperimentStateMachine.Stopping);
    }
    private IEnumerator RunSchedule(ExperimentWrapper schedule)
    {
        if (schedule == null)
            yield break;

        schedule.ReservedState = EReservedExperimentState.Processing;
        ExperimentScheduleChange?.Invoke(new List<ExperimentWrapper>(experimentSchedules));

        // 실험 타입 설정 리셋
        Manager.Network.ReserveDateWriteing("AI", 46, 0);

        // 현재 Schedule의 실험 타입 선택
        string commandKey = "Experiment_" + schedule.Group;
        var instrumentData = Manager.Data.CallData<InstrumentInfo>(commandKey);

        if (instrumentData == null)
        {
            Debug.LogError($"[RunSchedule Error] 실험 타입 Instrument 없음: {commandKey}");
            SetState(EExperimentStateMachine.Error);
            yield break;
        }

        Manager.Network.ReserveDateWriteing(
            instrumentData.PointType,
            (ushort)instrumentData.Address,
            1
        );

        // 현재 실험 타입에 해당하는 Step만 전송
        foreach (var step in schedule.Experiments
            .Where(x => x.Group == schedule.Group)
            .OrderBy(x => x.Process))
        {
            ApplyStepValue(step);

            if (CurrentState != EExperimentStateMachine.Running)
                yield break;
        }

        // 실험 시작 트리거
        instrumentData = Manager.Data.CallData<InstrumentInfo>("Ex_Start");

        if (instrumentData == null)
        {
            Debug.LogError("[RunSchedule Error] Ex_Start Instrument 없음");
            SetState(EExperimentStateMachine.Error);
            yield break;
        }

        Manager.Network.ReserveDateWriteing(
            instrumentData.PointType,
            (ushort)instrumentData.Address,
            1
        );

        // 현재 실험 타입 완료 대기
        yield return StartCoroutine(WaitScheduleComplete(schedule));

        if (CurrentState != EExperimentStateMachine.Running)
            yield break;
    }
    private IEnumerator StopRoutine()
    {
        if (CurrentScheduleIndex < 0 || CurrentScheduleIndex >= experimentSchedules.Count)
        {
            SetState(EExperimentStateMachine.Idle);
            yield break;
        }

        ExperimentWrapper currentSchedule = experimentSchedules[CurrentScheduleIndex];

        if (!experimentDefines.TryGetValue("Type_End", out ExperimentWrapper endDefine))
        {
            Debug.LogError("[Schedule Stop] Type_End 정의가 없습니다.");
            SetState(EExperimentStateMachine.Error);
            yield break;
        }

        currentSchedule.ReservedState = EReservedExperimentState.Stopping;
        ExperimentScheduleChange?.Invoke(new List<ExperimentWrapper>(experimentSchedules));

        Debug.Log($"[Schedule Stop] Type_End Start: {currentSchedule.Name}");

        yield return StartCoroutine(RunEndProcedure(endDefine));

        if (CurrentState == EExperimentStateMachine.Shutdown ||
            CurrentState == EExperimentStateMachine.Resetting ||
            CurrentState == EExperimentStateMachine.Error)
        {
            yield break;
        }

        currentSchedule.ReservedState = EReservedExperimentState.Finished;
        ExperimentScheduleChange?.Invoke(new List<ExperimentWrapper>(experimentSchedules));

        Debug.Log($"[Schedule Stop] Type_End Finished: {currentSchedule.Name}");
    }
    private IEnumerator RunEndProcedure(ExperimentWrapper Defines)
    {
        if (Defines == null)
            yield break;

        // 현재 실험 타입 정의 찾기
        if (!experimentDefines.TryGetValue(Defines.Group, out ExperimentWrapper define))
        {
            Debug.LogError($"[EndProcedure] Define 없음: {Defines.Group}");
            SetState(EExperimentStateMachine.Error);
            yield break;
        }

        // Define에 있는 Type_End만 가져오기
        var endSteps = define.Experiments
            .Where(x => x.Group == "Type_End")
            .OrderBy(x => x.Process)
            .ToList();

        if (endSteps.Count == 0)
        {
            Debug.LogWarning($"[EndProcedure] Type_End 없음: {Defines.Group}");
            yield break;
        }

        Debug.Log($"[EndProcedure] Start: {Defines.Group}");

        foreach (var step in endSteps)
        {
            ApplyStepValue(step);

            if (CurrentState == EExperimentStateMachine.Shutdown ||
                CurrentState == EExperimentStateMachine.Resetting ||
                CurrentState == EExperimentStateMachine.Error)
            {
                yield break;
            }
        }

        // 종료 절차 시작 트리거
        var instrumentData = Manager.Data.CallData<InstrumentInfo>("Ex_Start");

        if (instrumentData == null)
        {
            Debug.LogError("[EndProcedure] Ex_Start 없음");
            SetState(EExperimentStateMachine.Error);
            yield break;
        }

        Manager.Network.ReserveDateWriteing(
            instrumentData.PointType,
            (ushort)instrumentData.Address,
            1
        );

        // Type_End 완료 대기
        yield return StartCoroutine(WaitScheduleComplete(Defines, "End"));

        Debug.Log($"[EndProcedure] Finished: {Defines.Group}");
    }
    private void MoveNextSchedule()
    {
        CurrentScheduleIndex++;

        if (CurrentScheduleIndex >= experimentSchedules.Count)
        {
            SetState(EExperimentStateMachine.Idle);
            return;
        }

        SetState(EExperimentStateMachine.Running);
    }
    private IEnumerator ShutdownRoutine()
    {
        Debug.Log("[Experiment] Shutdown");

        if (CurrentScheduleIndex >= 0 && CurrentScheduleIndex < experimentSchedules.Count)
        {
            var schedule = experimentSchedules[CurrentScheduleIndex];

            schedule.ReservedState = EReservedExperimentState.Stopping;
            ExperimentScheduleChange?.Invoke(new List<ExperimentWrapper>(experimentSchedules));

            yield return StartCoroutine(RunEndProcedure(schedule));

            schedule.ReservedState = EReservedExperimentState.Finished;
            ExperimentScheduleChange?.Invoke(new List<ExperimentWrapper>(experimentSchedules));

            CurrentScheduleIndex++;
        }

        isProcessing = false;
        SetState(EExperimentStateMachine.Idle);
    }
    private IEnumerator StopRoutine(string test)
    {
        Debug.Log("Stopping safely...");

        // 모든 Output Off
        //Manager.Network.AllOff();

        // 3. 종료 절차 대기
        // 종료 절차 시작 트리거
        //yield return StartCoroutine(WaitScheduleComplete(schedule, "End")); //종료 절차 진행 확인 및 대기

        yield return new WaitForSeconds(1f);
    }
    private IEnumerator ResetRoutine()
    {
        Debug.Log("Resetting system...");

        // 모든 Output Off
        //Manager.Network.AllOff();

        yield return new WaitForSeconds(2f);

        CurrentScheduleIndex = 0;
    }

    // TODO : 에러 발생 시 안전하게 시스템을 멈추고, 에러 상태로 진입하여 추가 조치 필요 여부 판단
    private IEnumerator ErrorRoutine()
    {
        Debug.LogError("Experiment Error!");

        // 모든 Output Off
        //Manager.Network.AllOff();

        yield break;
    }
    private void SetState(EExperimentStateMachine newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        ExperimentStateChange?.Invoke(CurrentState);
    }

    /// <summary>
    /// 저장된 데이터를 적용하는 코드
    /// </summary>
    /// <param name="step"></param>
    private void ApplyStepValue(ExperimentInfo step)
    {
        switch (step.Action)
        {
            case "Set":
            case "Timer":
                var instrumentData = Manager.Data.CallData<InstrumentInfo>(step.Tag);
                if (instrumentData == null)
                {
                    Debug.LogError($"[Step Error] Instrument 없음: {step.Tag}");
                    SetState(EExperimentStateMachine.Error);
                    return;
                }

                Manager.Network.ReserveDateWriteing(
                    instrumentData.PointType,
                    (ushort)instrumentData.Address,
                    (ushort)step.Value
                );
                break;
            case "Sol_Set":
                ApplySolSet(step);
                break;
            case "End":
            case "None":
                return;

            default:
                Debug.LogError($"[Step Error] 알 수 없는 Action: {step.Action}");
                SetState(EExperimentStateMachine.Error);
                return;
        }

        Debug.Log($"[Step Apply] {step.Name} / {step.Tag} = {step.Value}");
    }
    private IEnumerator WaitScheduleComplete(ExperimentWrapper schedule, string type = null)
    {
        if (schedule == null)
            yield break;

        float timeout = schedule.Timer;
        float elapsed = 0f;

        bool isEnd = type == "End";

        string targetGroup = isEnd
            ? "Type_End"
            : schedule.Group;

        string group = targetGroup.Replace("Type_", "");
        string processKey = $"Experiment_Process_{group}";

        int processCount = schedule.Experiments.Count(
            x => x.Group == targetGroup
        );

        while (elapsed < timeout)
        {
            if (CurrentState != EExperimentStateMachine.Running &&
                CurrentState != EExperimentStateMachine.Stopping)
            {
                yield break;
            }

            if (UpdatedDataForExperiment.TryGetValue(processKey, out Datas processData))
            {
                if (IsProcessComplete((int)processData.Value, processCount))
                {
                    schedule.ReservedState = isEnd
                        ? EReservedExperimentState.Finished
                        : EReservedExperimentState.Stopping;

                    Debug.Log($"[Schedule Complete] {schedule.Name} / {targetGroup}");

                    yield break;
                }
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogError($"[Schedule Timeout] {schedule.Name} / {targetGroup}");
        SetState(EExperimentStateMachine.Error);
    }
    private bool IsProcessComplete(int value, int processCount)
    {
        if (processCount <= 0)
            return true;

        if (processCount >= 31)
        {
            Debug.LogError($"[Process Error] processCount 초과: {processCount}");
            SetState(EExperimentStateMachine.Error);
            return false;
        }

        int completeMask = (1 << processCount) - 1;

        return (value & completeMask) == completeMask;
    }
    private void ApplySolSet(ExperimentInfo step)
    {
        int value = (int)step.Value;

        var allData = Manager.Data.CallData<Dictionary<string, InstrumentInfo>>("Experiment");

        if (allData == null)
        {
            Debug.LogError("[Sol_Set Error] Instrument 전체 데이터 없음");
            SetState(EExperimentStateMachine.Error);
            return;
        }

        var solList = allData
            .Where(x => x.Key.StartsWith($"{step.Tag}_"))
            .OrderBy(x =>
            {
                string numberText = x.Key.Replace($"{step.Tag}_", "");

                if (int.TryParse(numberText, out int number))
                    return number;

                return int.MaxValue;
            })
            .ToList();

        if (solList.Count == 0)
        {
            Debug.LogError($"[Sol_Set Error] Sol 태그 없음: {step.Tag}_1, {step.Tag}_2 ...");
            SetState(EExperimentStateMachine.Error);
            return;
        }

        for (int i = 0; i < solList.Count; i++)
        {
            var sol = solList[i];

            bool isOn = (value & (1 << i)) != 0;

            Manager.Network.ReserveDateWriteing(
                sol.Value.PointType,
                (ushort)sol.Value.Address,
                (ushort)(isOn ? 1 : 0)
            );

            Debug.Log($"[Sol_Set] {sol.Key} = {(isOn ? 1 : 0)}");
        }
    }
}
