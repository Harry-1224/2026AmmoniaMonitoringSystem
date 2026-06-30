using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EUiLampType
{
    ExperimentLamp,
    LoggingLamp,

}

public enum EUiLampColor
{
    Off,
    Green,
    Red,
    Yellow
}

public class Lamp : UiObjectBase
{
    public EUiLampType lampType;

    public Image lampLight;

    protected override void OnEnable()
    {
        base.OnEnable();
        switch (lampType)
        {
            case EUiLampType.ExperimentLamp:
                ExperimentPLCStateChangeHandler(Manager.Experiment.CurrentState);
                break;
            case EUiLampType.LoggingLamp:
                OnChangeLoggingStateHandler(Manager.Logging.CheckLoggingState());
                break;
        }
    }

    protected override void Initialize()
    {
        base.Initialize();
    }


    protected override void EventSubscriber()
    {
        base.EventSubscriber();
        switch (lampType)
        {
            case EUiLampType.ExperimentLamp :
                Manager.Experiment.ExperimentPLCStateChange += ExperimentPLCStateChangeHandler;
                break;
            case EUiLampType.LoggingLamp:
                Manager.Logging.OnChangeLoggingState += OnChangeLoggingStateHandler;
                break;
        }
    }
    protected override void EventUnsubscriber()
    {
        base.EventUnsubscriber();
        switch (lampType)
        {
            case EUiLampType.ExperimentLamp:

                Manager.Experiment.ExperimentPLCStateChange -= ExperimentPLCStateChangeHandler;
                break;
            case EUiLampType.LoggingLamp:
                Manager.Logging.OnChangeLoggingState -= OnChangeLoggingStateHandler;
                break;
        }
    }

    private void OnDataChangeHandler(Dictionary<string, Datas> data) 
    {

    }
    private void ExperimentPLCStateChangeHandler(EExperimentStateMachine state)
    {
        switch (state)
        {
            case EExperimentStateMachine.Idle:
                ChangeLampColor(EUiLampColor.Off);
                break;

            case EExperimentStateMachine.Running:
                ChangeLampColor(EUiLampColor.Green);
                break;

            case EExperimentStateMachine.Stopping:
            case EExperimentStateMachine.Resetting:
                ChangeLampColor(EUiLampColor.Yellow);
                break;

            case EExperimentStateMachine.Error:
            case EExperimentStateMachine.Shutdown:
                ChangeLampColor(EUiLampColor.Red);
                break;

            default:
                ChangeLampColor(EUiLampColor.Off);
                break;
        }
    }

    private void OnChangeLoggingStateHandler(ELoggingState state)
    {
        switch (state)
        {
            case ELoggingState.Start:
                ChangeLampColor(EUiLampColor.Yellow);
                break;
            case ELoggingState.Logging:
                ChangeLampColor(EUiLampColor.Green);
                break;
            case ELoggingState.Stop:
                ChangeLampColor(EUiLampColor.Off);
                break;
            case ELoggingState.Error:
                ChangeLampColor(EUiLampColor.Red);
                break;
        }
    }
    private void ChangeLampColor(EUiLampColor color)
    {
        if (lampLight == null) return;

        switch (color)
        {
            case EUiLampColor.Off:
                lampLight.color = Color.gray;
                break;

            case EUiLampColor.Green:
                lampLight.color = Color.green;
                break;

            case EUiLampColor.Red:
                lampLight.color = Color.red;
                break;

            case EUiLampColor.Yellow:
                lampLight.color = Color.yellow;
                break;
        }
    }
}
