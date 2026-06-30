using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

public enum ELoggingState
{
    Start,
    Logging,
    Stop,
    Error,
}
public class LoggingManager : ManagerBase
{
    protected override void Intialize()
    {
        // 초기화 로직 구현
    }
    protected override void EventSubscriber()
    {
        Manager.Experiment.ExperimentStateChange += OnExperimentStateChanged;
    }
    protected override void EventUnsubscriber()
    {           
        Manager.Experiment.ExperimentStateChange -= OnExperimentStateChanged;
    }



    #region Singleton
    public static LoggingManager Instance { get; private set; }
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

    #region Experiment
    private void OnExperimentStateChanged(EExperimentStateMachine state)
    {
        if (state == EExperimentStateMachine.Running)
        {
            OnStartLogging();
        }
        else
        {
            OnStopLogging();
        }
    }

    #endregion

    #region Logging System

    public int LoggingIntervalMs { get; private set; } = 1000;

    private CancellationTokenSource timerCTS;

    public event Action OnLoggingStarted;
    public event Action<DateTime> OnLoggingTimingActed;
    public event Action<Exception> OnLoggingTimingFailed;
    public event Action OnLoggingStopped;
    public event Action<ELoggingState> OnChangeLoggingState;

    public ELoggingState loggingState = ELoggingState.Stop;

    public void SetLoggingIntervalMs(int intervalMs)
    {
        LoggingIntervalMs = intervalMs;
    }
    public void OnErrorLogging(string text)
    {

    }
    public void OnStartLogging()
    {
        if (timerCTS != null) return;

        loggingState = ELoggingState.Start;

        OnLoggingStarted?.Invoke();
        OnChangeLoggingState.Invoke(loggingState);

        timerCTS = new CancellationTokenSource();

        _ = RunTimer(LoggingIntervalMs, timerCTS.Token);

    }
    public void OnStopLogging()
    {
        if (timerCTS == null)
            return;

        timerCTS.Cancel();
        timerCTS.Dispose();
        timerCTS = null;


        loggingState = ELoggingState.Stop;

        OnLoggingStopped?.Invoke();
        OnChangeLoggingState.Invoke(loggingState);
    }

    public ELoggingState CheckLoggingState() => loggingState;

    private async Task RunTimer(int intervalMs, CancellationToken token)
    {
        try
        {
            loggingState = ELoggingState.Logging;
            OnChangeLoggingState.Invoke(loggingState);

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(intervalMs, token);

                OnLoggingTimingActed?.Invoke(DateTime.Now);
            }
        }
        catch (TaskCanceledException)
        {
            // 정상 종료
        }
        catch (Exception ex) 
        {
            OnErrorLogged(nameof(LoggingManager),nameof(RunTimer), ex.Message);

            loggingState = ELoggingState.Error;
            OnChangeLoggingState.Invoke(loggingState);
            OnLoggingTimingFailed?.Invoke(ex);
        }
    }


    #endregion  
}
