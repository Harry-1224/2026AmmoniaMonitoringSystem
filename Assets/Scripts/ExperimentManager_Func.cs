using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public partial class ExperimentManager
{
    // ExperimentManger의 내부 함수를 모아놓은 파일

    public int CurrentScheduleIndex { get; private set; } = 0;
    public EExperimentStateMachine CurrentState { get; private set; } = EExperimentStateMachine.Idle;
    public EExperimentStateMachine commandState { get; private set; } = EExperimentStateMachine.Idle;
    public bool isProcessing { get; private set; }
    private bool waitPLCResponse = false;

    private bool startRequested = false;
    private bool stopRequested = false;
    private bool resetRequested = false;
    private bool shutdownRequested = false;

    private float stateStartTime;
    private float stateTimeout;
    private bool timeoutRunning;

    private Coroutine experimentRoutine; //실험을 진행하는 Coroutine을 저장하는 변수 
    private List<ExperimentWrapper> experimentSchedules = new List<ExperimentWrapper>();
    public Dictionary<string, ExperimentWrapper> experimentDefines = new Dictionary<string, ExperimentWrapper>();
    private Dictionary<string, Datas> UpdatedDataForExperiment = new Dictionary<string, Datas>();

    public event Action<EExperimentStateMachine> ExperimentPLCStateChange;
    public event Action<EExperimentStateMachine> ExperimentStateChange;
    public event Action<List<ExperimentWrapper>> ExperimentScheduleChange;

    #region State Machine

    //1. 현재 State상태 파악( 로깅 시작 )
    //2. PLC의 값에 따른 상태 파악
    //3. 현재 적용해야할 State 상태 파악
    //4. switch입장( 로깅 종료 )
    //5. 상태별 로직 실행
    private IEnumerator RunStateMachine()
    {
        Debug.Log("[Experiment] State Machine Started");

        while (true)
        {
            // 1. PLC 값을 보고 외부 공개용 CurrentState 갱신
            CurrentState = CheckPLCState();

            // 1-1. 현재 PLC상태가 idle로 바꼈을 때, 현재 진행중인 실험의 상태가 Processing이며, witePLCResponse가 False일 경우 로깅 종료
            //CheckAndFinishMainLogging();

            // 2. 버튼 요청 / PLC 상태 / Timeout 보고 controlState 결정
            commandState = CheckMachineState();


            if (commandState != EExperimentStateMachine.Idle) Debug.Log($"Experiment State Machine 명령 변경 - {commandState}");

            //3. commandState를 체크한 후 결과에 따라 1회 명령 수행
            switch (commandState)
            {
                case EExperimentStateMachine.Running:
                    // NOTE : 자동 실험 진행 시작
                    // NOTE : Command가 Running으로 들어왔을 때, 현재 실험이 Running이 아니면 ReservedState를 Processing으로 변경하고, 실험 절차 진행 시작. Running인 경우는 무시.

                    // 1. 예약된 실험 확인
                    if (CurrentScheduleIndex < 0 || CurrentScheduleIndex >= experimentSchedules.Count)
                    {
                        Debug.LogError("[StateMachine] Running - Out of  Schedule Range");
                        break;
                    }

                    var runningSchedule = experimentSchedules[CurrentScheduleIndex];

                    // 2. 검색한 실험 절차 Value 입력
                    if (!SettingValue(runningSchedule))
                    {
                        Debug.LogError("[StateMachine] Running SettingValue 실패");
                        break;
                    }
                    // 3. isProcessing 참 전환 / 다음 실험 절차 검색/ 현재 실험 index 최신화(현재 실험 State를 Processing으로 설정)
                    isProcessing = true;
                    waitPLCResponse = true;

                    SettingExperimentState(runningSchedule, EReservedExperimentState.Processing);

                    // 4. Timeout 횟수와 시간 입력
                    stateStartTime = Time.time;
                    stateTimeout = runningSchedule.Timer;
                    timeoutRunning = true;

                    // 5. Logging 시작
                    Manager.Logging.OnStartLogging();

                    break;
                case EExperimentStateMachine.Stopping:
                    // NOTE : 자동 실험 진행 종료(현재 실험 완료 후 종료)
                    // NOTE : 만약 실험 중이라면 현재 실험 종료(Reset까지) 후 정지.
                    // NOTE : isProcessing을 보고 실험이 자동 진행되는지 판단 따라서 isProcessing이 거짓이면 현재 실험 까지만 진행 후 종료

                    // 1. isProcessing 거짓
                    isProcessing = false;

                    break;
                case EExperimentStateMachine.Resetting:
                    // NOTE : Reset은 즉시 실험 환경 재시작.
                    // NOTE : Reset Data 입력 후 현재 실험중일 경우 Failed로 상태 변경,
                    //        다음 실험 절차 검색/ 현재 실험 index 최신화(현재 실험 State를 Failed로 설정)

                    // 1. isProcessing 거짓(CheckMachineState()함수 중 명령에 의해 작동될 때로 수정)

                    waitPLCResponse = true;

                    // 2. 종료 절차 검색
                    if (!experimentDefines.TryGetValue("Type_End", out var endDefine))
                        break;

                    // 3. Stopping절차 Value 입력
                    if (!SettingValue(endDefine))
                        break;


                    if (CurrentScheduleIndex >= 0 && CurrentScheduleIndex < experimentSchedules.Count)
                    {
                        var resetSchedule = experimentSchedules[CurrentScheduleIndex];

                        if (resetSchedule.ReservedState == EReservedExperimentState.Processing)
                        {

                            if (CurrentState == EExperimentStateMachine.Idle)
                                SettingExperimentState(resetSchedule, EReservedExperimentState.Resetting);

                            else if (CurrentState == EExperimentStateMachine.Running)
                                SettingExperimentState(resetSchedule, EReservedExperimentState.Failed);

                        }
                        
                    }

                    // 5. Timeout 횟수와 시간 입력
                    stateStartTime = Time.time;
                    stateTimeout = endDefine.Timer;
                    timeoutRunning = true;

                    break;
                case EExperimentStateMachine.Shutdown:
                    // NOTE : 즉시 시스템 종료(FAN, Water Supply, MFC 정지)

                    // 즉시 자동 실험 중지
                    isProcessing = false;
                    waitPLCResponse = false;
                    timeoutRunning = false;

                    // 현재 진행중인 실험 상태 변경
                    if (CurrentScheduleIndex >= 0 &&
                        CurrentScheduleIndex < experimentSchedules.Count)
                    {
                        var shutdownSchedule = experimentSchedules[CurrentScheduleIndex];

                        if (shutdownSchedule.ReservedState != EReservedExperimentState.Reserved)
                        {
                            //현재 실험 실패로 설정
                            SettingExperimentState(shutdownSchedule, EReservedExperimentState.Failed);

                            // 현재 실험 다음으로 설정
                            CurrentScheduleIndex++;
                        }
                    }

                    // Ex_ESD = 1 전송
                    var esdData = Manager.Data.CallData<InstrumentInfo>("Ex_ESD");

                    if (esdData == null)
                    {
                        Debug.LogError("[Shutdown] Ex_ESD Instrument 없음");
                        break;
                    }

                    Manager.Network.ReserveDateWriteing(
                        esdData.PointType,
                        (ushort)esdData.Address,
                        1
                    );


                    //Debug.Log("[Shutdown] Ex_ESD = 1 전송");



                    /*

                    // 1. isProcessing 거짓(CheckMachineState()함수 중 명령에 의해 작동될 때로 수정)

                    // 2. 다음 실험 절차 검색/현재 실험 index 최신화(진행했던 실험의 State는 Failed로 설정)
                    if (CurrentScheduleIndex >= 0 && CurrentScheduleIndex < experimentSchedules.Count)
                    {
                        SettingExperimentState(experimentSchedules[CurrentScheduleIndex], EReservedExperimentState.Failed);
                    }

                    // 3. All Shutdown Value 입력if
                    if(!experimentDefines.TryGetValue("Type_ESD", out var esdDefine))
                    {
                        Debug.LogError("[Shutdown] Type_ESD 정의가 없습니다.");
                        break;
                    }

                    if (!SettingValue(esdDefine))
                    {
                        Debug.LogError("[Shutdown] Type_ESD SettingValue 실패");
                        break;
                    }*/
                    break;
            }

            //컨트롤의 입력은 한번만 이루어져야함
            commandState = EExperimentStateMachine.Idle;

            yield return null;
        }
    }

    /// <summary>
    /// 현재 PLC의 상태를 확인하고, CurrentState를 갱신하는 함수(StateMachine안에서 사용)
    /// </summary>
    /// <returns></returns>
    private EExperimentStateMachine CheckPLCState()
    {
        if (!UpdatedDataForExperiment.TryGetValue("Ex_Start", out Datas exStart))
            return CurrentState;

        if (!UpdatedDataForExperiment.TryGetValue("Ex_Reset", out Datas exReset))
            return CurrentState;

        EExperimentStateMachine state = CurrentState;

        if (exStart.Value > 0)
        {
            state = EExperimentStateMachine.Running;
        }
        else if (exReset.Value > 0)
        {
            state = EExperimentStateMachine.Resetting;
        }
        else
        {
            state = EExperimentStateMachine.Idle;
        }
        ELoggingState logState = Manager.Logging.CheckLoggingState();

        // 무슨 경우에도 일단 Running이 아닌데 로깅 중이면 로깅 종료(실험이 시작될 때 로깅이 진행중이면 안되는 관계로 종료)
        // TODO : 실험이 진행중이 아닌 상황에서도 로깅이 종료되는 현상 발생. 실험이 시작될 때만 한정하여 시작하도록 설정
        // if (!waitPLCResponse && state != EExperimentStateMachine.Running && logState == ELoggingState.Logging) StopAndSaveLogging(experimentSchedules[CurrentScheduleIndex]); 

        if (state != CurrentState)
        {
            CurrentState = state;
            ExperimentPLCStateChange?.Invoke(state);
        }

        return CurrentState;
    }

    /// <summary>
    /// PLC상태와 내부 명령을 판단 후, controlState를 결정하는 함수(StateMachine안에서 사용)
    /// </summary>
    /// <returns></returns>
    private EExperimentStateMachine CheckMachineState()
    {

        // 1. PLC 상태 확인(Running, Resetting, Idle) - CheckPLCState에서 이미 체크함

        // 2. 외부 명령 요청 확인(Shutdown, Reset, Stop, Start)
        //      - Start : PLC상태가 Idle일 때만 허용 -> Running 반환
        //      - Stop : isProcessing이 참일 때 허용 -> Stopping 반환
        //      - Reset : Running, Idle에서만 허용(단! 둘다 로직은 다름) -> Resetting 반환
        //      - Shutdown : 어느 상태에서든 허용 -> Shutdown 반환
        if (shutdownRequested)
        {
            ClearRequests();

            isProcessing = false;
            return EExperimentStateMachine.Shutdown;
        }
        if (stopRequested && isProcessing)
        {
            ClearRequests();
            return EExperimentStateMachine.Stopping;
        }
        if (resetRequested && (CurrentState == EExperimentStateMachine.Running || CurrentState == EExperimentStateMachine.Idle))
        {
            ClearRequests();

            isProcessing = false;
            return EExperimentStateMachine.Resetting;
        }
        if (startRequested && CurrentState == EExperimentStateMachine.Idle)
        {
            ClearRequests();

            if (CurrentScheduleIndex < 0 || CurrentScheduleIndex >= experimentSchedules.Count)
            {
                Debug.LogError("[StateMachine] Running - Out of  Schedule Range : Reset Index");
                CurrentScheduleIndex = 0;
            }

            return EExperimentStateMachine.Running;
        }
        ClearRequests();

        // 3. Timeout 확인
        if (IsTimeout())
        {
            if (CurrentState == EExperimentStateMachine.Running)
            {
                Debug.LogError("[Experiment] Running Timeout → Resetting");
                return EExperimentStateMachine.Resetting;
            }

            if (CurrentState == EExperimentStateMachine.Resetting)
            {
                Debug.LogError("[Experiment] Resetting Timeout : Table문서 오류. 종료 절차의 Timeout변수를 수정해 주세요. ");
                return EExperimentStateMachine.Error;
            }
        }


        // 4. CurrentExperiment정보 습득 / 만약 Range에서 문제가 생기면 오류
        ExperimentWrapper currentEx;
        if (CurrentScheduleIndex >= 0 && CurrentScheduleIndex < experimentSchedules.Count)
        {
            currentEx = experimentSchedules[CurrentScheduleIndex];
        }
        else if( CurrentScheduleIndex == -1)
        {
            return EExperimentStateMachine.Idle;
        }
        else
        {
            Debug.LogWarning("[ExperimentManger] Error - CheckMachineState : Out of Experiment Index Range");
            CurrentState = EExperimentStateMachine.Error;
            return EExperimentStateMachine.Shutdown;
        }

        // 5. waitPLCResponse는 상태 머신이 자동으로 바껴야 하는 순간 1회 명령을 주기 위해 구현한 변수이다.
        if ((currentEx.ReservedState == EReservedExperimentState.Processing && CurrentState == EExperimentStateMachine.Running) || 
            (currentEx.ReservedState == EReservedExperimentState.Resetting && CurrentState == EExperimentStateMachine.Resetting))
        {
            waitPLCResponse = false;
        }


        // 6. 명령 없고 Timeout도 없을 때, CurrentState가 Idle이면(Running, Resetting, Idle밖에 없음), isProcessing을 확인할 것.
        //      - isProcessing이 참이면, 현재 실험 진행 중이므로, CurrentScheduleIndex++ 후 Running 반환
        //      - isProcessing이 거짓이면, 현재 실험 진행 중이 아니므로, Idle 반환

        if (waitPLCResponse || CurrentState != EExperimentStateMachine.Idle) //지금 과연 PLC가 아무 실험 동작을 안하는가?
            return EExperimentStateMachine.Idle;

        if (isProcessing) // 지금 실험 자동 진행중인가?
        {
            // 실험이 진행 중이라면 Reset반환
            if (currentEx.ReservedState == EReservedExperimentState.Processing)
                return EExperimentStateMachine.Resetting;

            // 실험이 Reset 중 이였다면, 다음 실험을 위한 Running반환 / 만약 다음 실험이 없는경우 Idle반환
            else if (currentEx.ReservedState == EReservedExperimentState.Resetting)
            {
                timeoutRunning = false;

                SettingExperimentState(currentEx, EReservedExperimentState.Finished);

                if (CurrentScheduleIndex <= experimentSchedules.Count - 1)
                {
                    CurrentScheduleIndex++;
                }
                // 다음 예약된 실험이 있는경우
                if (CurrentScheduleIndex < experimentSchedules.Count)
                {
                    return EExperimentStateMachine.Running;
                }

                // 다음 예약된 실험이 없는경우
                isProcessing = false;
                CurrentScheduleIndex = -1;
                return EExperimentStateMachine.Idle;
                /*
                timeoutRunning = false;
                if (CurrentScheduleIndex < experimentSchedules.Count - 1) return EExperimentStateMachine.Running;
                else return EExperimentStateMachine.Idle;
                */

            }
            // 만약 currentEx.ReservedState에서 오류 상황 (Finish or Failed)
            else
            {
                // 여기선 무조건 로깅이 정지 및 리셋 되어야함.
                //StopAndSaveLogging(currentEx, false);

                timeoutRunning = false;
                CurrentScheduleIndex++;
            }
        }
        else // 실험이 자동진행 중이 아닌가?
        {
            if (currentEx.ReservedState == EReservedExperimentState.Processing)
                return EExperimentStateMachine.Resetting;
            else if (currentEx.ReservedState == EReservedExperimentState.Resetting)
            {
                timeoutRunning = false;

                SettingExperimentState(currentEx, EReservedExperimentState.Finished);

                if (CurrentScheduleIndex <= experimentSchedules.Count - 1)
                {
                    CurrentScheduleIndex++;
                }
                else CurrentScheduleIndex = -1;

                return EExperimentStateMachine.Idle;
            }
            else if (currentEx.ReservedState == EReservedExperimentState.Reserved)
            {
                timeoutRunning = false;
            }
            else 
            {
                timeoutRunning = false;
                CurrentScheduleIndex++;
            }
        }

        return EExperimentStateMachine.Idle;
    }
    private void CheckAndFinishMainLogging()
    {
        if (CurrentState != EExperimentStateMachine.Idle)
            return;

        if (waitPLCResponse)
            return;

        if (CurrentScheduleIndex < 0 || CurrentScheduleIndex >= experimentSchedules.Count)
            return;

        var currentExperiment = experimentSchedules[CurrentScheduleIndex];

        if (currentExperiment.ReservedState != EReservedExperimentState.Processing)
            return;

        Manager.Logging.OnStopLogging();

        Manager.Data.ExportLoggedData(currentExperiment.Name);
        Manager.Data.ClearLoggedData();

        SettingExperimentState(currentExperiment, EReservedExperimentState.Finished);
    }
    private void StopAndSaveLogging(ExperimentWrapper experiment, bool saveData = true)
    {
        if (experiment == null)
            return;

        if (Manager.Logging.CheckLoggingState() == ELoggingState.Stop)
            return;


        if (saveData) Manager.Data.ExportLoggedData(experiment.Name);

        Manager.Logging.OnStopLogging();
        Manager.Data.ClearLoggedData();
        Manager.Logging.OnStopLogging();
    }
    // 모든 요청 reset
    private void ClearRequests()
    {
        startRequested = false;
        stopRequested = false;
        resetRequested = false;
        shutdownRequested = false;
    }

    // 타임아웃을 체크하는 함수 
    private bool IsTimeout()
    {
        if (CurrentState != EExperimentStateMachine.Running && CurrentState != EExperimentStateMachine.Resetting)
            return false;

        if (!timeoutRunning)
            return false;

        if (stateTimeout <= 0)
            return false;

        return Time.time - stateStartTime >= stateTimeout;
    }

    /// <summary>
    /// PLC로 실험 Value를 Network에 전송하는 함수(StateMachine 안에서 사용)
    /// 상태 변경은 하지 않고, PLC Write 요청만 예약한다.
    /// </summary>
    private bool SettingValue(ExperimentWrapper experiment)
    {
        if (experiment == null)
        {
            Debug.LogError("[SettingValue Error] ExperimentWrapper가 null입니다.");
            return false;
        }

        // 실험 타입 설정 리셋
        Manager.Network.ReserveDateWriteing("AI", 45, 0); // 실험 시작 버튼 초기화
        Manager.Network.ReserveDateWriteing("AI", 46, 0); // 실험 설정 버튼 초기화

        // 현재 Schedule의 실험 타입 선택
        string commandKey = "Experiment_" + experiment.Group;
        var instrumentData = Manager.Data.CallData<InstrumentInfo>(commandKey);

        if (instrumentData == null)
        {
            Debug.LogError($"[SettingValue Error] 실험 타입 없음: {commandKey}");
            return false;
        }
        else
        {
            //Debug.Log($"[SettingValue Error] 실험 타입 발견 : {commandKey} => Address - {instrumentData.Address}");
        }
        Manager.Network.ReserveDateWriteing(
              instrumentData.PointType,
              (ushort)instrumentData.Address,
              1
          );


        // 현재 실험 타입에 해당하는 Step 값 전송
        foreach (var step in experiment.Experiments.Where(x => x.Group == experiment.Group).OrderBy(x => x.Process))
        {
            if (!ApplyStepValue(step)) return false;
        }

        string name = "";

        // 실험 시작 트리거
        if(experiment.Group != "Type_End")  name ="Ex_Start";
        else name = "Ex_Reset";

        instrumentData = Manager.Data.CallData<InstrumentInfo>(name);

        if (instrumentData == null)
        {
            Debug.LogError($"[SettingValue Error] {name} Data 없음");
            return false;
        }
        else
        {
            Debug.Log($"[SettingValue] {name} Data 발견 : {name} => Address ( {instrumentData.Address} )");
        }

        Manager.Network.ReserveDateWriteing(
            instrumentData.PointType,
            (ushort)instrumentData.Address,
            1
        );

        Debug.Log($"[SettingValue] Start Command Sent: {experiment.Name}");
        return true;
    }
    private void SettingExperimentState(ExperimentWrapper experiment, EReservedExperimentState setState)
    {
        if (experiment == null)
        {
            Debug.LogError("[SettingExperimentState] experiment가 null입니다.");
            return;
        }

        if (experiment.ReservedState == setState)
            return;

        Debug.Log($"[SettingExperimentState] experiment {experiment.Name} 상태 전환 : {experiment.ReservedState} -> {setState}");
        experiment.ReservedState = setState;

        ExperimentScheduleChange?.Invoke(new List<ExperimentWrapper>(experimentSchedules));

        Debug.Log($"[Experiment State] {experiment.Name} -> {setState}");
    }

    #endregion

    #region Before Machine Code
    /*
    private IEnumerator RunStateMachine()
    {
        Debug.Log("[Experiment] State Machine Started");
        while (true)
        {
            switch (CurrentState)
            {
                case EExperimentStateMachine.Idle:
                    isProcessing = false;

                    if (startRequested)
                    {
                        startRequested = false;
                        CurrentScheduleIndex = 0;
                        isProcessing = true;
                        SetState(EExperimentStateMachine.Running);
                    }
                    break;

                case EExperimentStateMachine.Running:
                    yield return StartCoroutine(RunCurrentScheduleFlow());
                    break;

                case EExperimentStateMachine.Stopping:
                    yield return StartCoroutine(StopRoutine());
                    MoveNextSchedule();
                    break;

                case EExperimentStateMachine.Shutdown:
                    yield return StartCoroutine(ShutdownRoutine());
                    SetState(EExperimentStateMachine.Idle);
                    break;

                case EExperimentStateMachine.Resetting:
                    yield return StartCoroutine(ResetRoutine());
                    SetState(EExperimentStateMachine.Idle);
                    break;

                case EExperimentStateMachine.Error:
                    yield return StartCoroutine(ErrorRoutine());
                    SetState(EExperimentStateMachine.Idle);
                    break;
            }

            yield return null;
        }
    }*/

    /*
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
    }*/

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
        foreach (var step in schedule.Experiments.Where(x => x.Group == schedule.Group).OrderBy(x => x.Process))
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

        currentSchedule.ReservedState = EReservedExperimentState.Resetting;
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
        var instrumentData = Manager.Data.CallData<InstrumentInfo>("Ex_Stop");

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

            schedule.ReservedState = EReservedExperimentState.Resetting;
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
    private bool ApplyStepValue(ExperimentInfo step)
    {
        switch (step.Action)
        {
            case "Set":
            case "Timer":

                var instrumentData = Manager.Data.CallData<InstrumentInfo>(step.Tag);

                if (instrumentData == null)
                {
                    Debug.LogError($"[Step Error] Instrument 없음: {step.Tag}");
                    return false;
                }

                Manager.Network.ReserveDateWriteing(
                    instrumentData.PointType,
                    (ushort)instrumentData.Address,
                    (ushort)step.Value
                );

                break;

            case "SolSet":
                ApplySolSet(step);
                break;

            case "End":
            case "None":
                return true;

            default:
                Debug.LogError($"[Step Error] 알 수 없는 Action: {step.Action}");
                return false;
        }

        return true;
    }
    /*
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
            case "SolSet":
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
    }*/

    private IEnumerator WaitScheduleComplete(ExperimentWrapper schedule, string type = null)
    {
        if (schedule == null)
            yield break;

        float timeout = schedule.Timer;
        float elapsed = 0f;

        bool isEnd = type == "End";

        while (elapsed < timeout)
        {
            if (CurrentState != EExperimentStateMachine.Running &&
                CurrentState != EExperimentStateMachine.Stopping)
            {
                yield break;
            }

            UpdateScheduleProgress(schedule, isEnd);

            if (!UpdatedDataForExperiment.TryGetValue("Ex_Start", out Datas exStart))
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
                continue;
            }

            if (!UpdatedDataForExperiment.TryGetValue("Ex_Reset", out Datas exReset))
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
                continue;
            }

            // 본 실험 완료 판단
            if (!isEnd && exStart.Value == 0 && exReset.Value == 1)
            {
                schedule.ReservedState = EReservedExperimentState.Resetting;

                ExperimentScheduleChange?.Invoke(
                    new List<ExperimentWrapper>(experimentSchedules)
                );

                Debug.Log($"[Schedule Complete] {schedule.Name} / Main");

                yield break;
            }

            // End 절차 완료 판단
            if (isEnd && exStart.Value == 0 && exReset.Value == 0)
            {
                schedule.ReservedState = EReservedExperimentState.Finished;

                ExperimentScheduleChange?.Invoke(
                    new List<ExperimentWrapper>(experimentSchedules)
                );

                Debug.Log($"[Schedule Complete] {schedule.Name} / End");

                yield break;
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogError($"[Schedule Timeout] {schedule.Name} / {(isEnd ? "End" : "Main")}");

        if (isEnd)
        {
            SetState(EExperimentStateMachine.Error);
        }
        else
        {
            schedule.ReservedState = EReservedExperimentState.Resetting;

            ExperimentScheduleChange?.Invoke(
                new List<ExperimentWrapper>(experimentSchedules)
            );

            SetState(EExperimentStateMachine.Stopping);
        }
    }
    /*
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

        if (isEnd)
        {
            SetState(EExperimentStateMachine.Error);
        }
        else
        {
            schedule.ReservedState = EReservedExperimentState.Stopping;
            ExperimentScheduleChange?.Invoke(new List<ExperimentWrapper>(experimentSchedules));

            SetState(EExperimentStateMachine.Stopping);
        }
    }*/

    private int CountCompletedBits(int value, int processCount)
    {
        if (processCount <= 0)
            return 0;

        int count = 0;

        for (int i = 0; i < processCount; i++)
        {
            if ((value & (1 << i)) != 0)
                count++;
        }

        return count;
    }
    private void ApplySolSet(ExperimentInfo step)
    {
        int value = (int)step.Value;

        var allData = Manager.Data.CallData<Dictionary<string, InstrumentInfo>>("Experiment");

        if (allData == null)
        {
            Debug.LogError("[SolSet Error] Instrument 전체 데이터 없음");
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
            //Debug.LogError($"[SolSet Error] Sol 태그 없음: {step.Tag}_1, {step.Tag}_2 ...");
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

            //Debug.Log($"[Sol_Set] {sol.Key} = {(isOn ? 1 : 0)}");
        }
    }

    private void UpdateScheduleProgress(ExperimentWrapper schedule, bool isEnd)
    {
        if (schedule == null)
            return;

        string targetGroup = isEnd ? "Type_End" : schedule.Group;
        string group = targetGroup.Replace("Type_", "");
        string processKey = $"Experiment_Process_{group}";

        if (!UpdatedDataForExperiment.TryGetValue(processKey, out Datas processData))
            return;

        int total = schedule.Experiments.Count(x => x.Group == targetGroup);
        int current = CountCompletedBits((int)processData.Value, total);

        if (schedule.CurrentProcess == current &&
            schedule.TotalProcess == total)
            return;

        schedule.CurrentProcess = current;
        schedule.TotalProcess = total;

        ExperimentScheduleChange?.Invoke(
            new List<ExperimentWrapper>(experimentSchedules)
        );
    }
    #endregion 
}
