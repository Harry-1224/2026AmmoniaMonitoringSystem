using UnityEditor.Experimental.GraphView;
using UnityEngine;

public partial class UiManager
{
    // - NOTE : UI의 Object들이 UiManager에 접근할 필요가 있는 코드는 여기서 작성.
    // - NOTE : UI의 Object들 간 데이터의 이동이 필요한 경우 여기서 기능을 구현할 것


    #region Control Methods
    public void OnClickUiButton(string func)
    {
        switch (func)
        {
            case "LoggingStart":
                break;
            case "LoggingStop":
                break;
            case "LoggingSave":
                break;
            case "LoggingReset":
                break;
            case "MonitorMain":
                OnMonitorChanged(EUiScreen.Basic);
                background.gameObject.SetActive(false);
                break;
            case "MonitorPNID":
                OnMonitorChanged(EUiScreen.Monitoring);
                background.gameObject.SetActive(true);
                break;
            case "MonitorGraph":
                OnMonitorChanged(EUiScreen.Graph);
                background.gameObject.SetActive(true);
                break;
            case "MonitorSetting":
                OnMonitorChanged(EUiScreen.Setting);
                background.gameObject.SetActive(true);
                break;
            case "MonitorExperiment":
                OnMonitorChanged(EUiScreen.Experiment);
                background.gameObject.SetActive(true);
                break;
            case "Exit":
                Application.Quit();
                break;
        }
    }
    public void OnDataBoxExpanded(DataBox selectedBox)
    {
        if (selectedBox == null)
        {
            Debug.LogWarning("[UiManager] 선택된 DataBox가 null입니다.");
            return;
        }
        if (!uiObjects.TryGetValue("MonitoringBox", out UiObjectBase monitoringObj))
        {
            Debug.LogWarning("[UiManager] MonitoringBox UI 없음");
            return;
        }
        if (!uiObjects.TryGetValue("ExperimentBox", out UiObjectBase experimentObj))
        {
            Debug.LogWarning("[UiManager] ExperimentBox UI 없음");
            return;
        }

        DataBox monitoringBox = monitoringObj as DataBox;
        DataBox experimentBox = experimentObj as DataBox;

        if (monitoringBox == null || experimentBox == null)
            return;

        if (selectedBox == monitoringBox)
        {
            monitoringBox.Expand();
            experimentBox.Shrink();
            Debug.Log("Monitoring Box Expanded, Control Box Shrinked");
        }
        else if (selectedBox == experimentBox)
        {
            experimentBox.Expand();
            monitoringBox.Shrink();
            Debug.Log("Control Box Expanded, Monitoring Box Shrinked");
        }
    }

    public void UiSelect(EUi ui = EUi.HUD)
    {
        // 전체 UI 끄기
        foreach (var obj in UiGameObject.Values)
        {
            obj.SetActive(false);
        }

        // 해당 UI 켜기
        if (!UiGameObject.TryGetValue(ui.ToString(), out var target))
        {
            Debug.LogWarning($"[UiManager] UI 없음: {ui}");
            return;
        }

        target.SetActive(true);
    }
    public void OnMonitorChanged(EUiScreen screen)
    {
        foreach(var obj in HUDScreen.Values)
        {
            obj.SetActive(false);
        }
        if(!HUDScreen.TryGetValue(screen.ToString(), out var target))
        {
            Debug.LogWarning($"[UiManager] UI 없음: {screen}");
            return;
        }
        target.SetActive(true);
    }
    public void OnMonitorChanged(string screen)
    {

    }

    #endregion

    #region Experiment Methods

    /// <summary>
    /// ExperimentBox에서 어떤 실험 스케줄을 선택했는지 Monitor에 전달하는 메서드
    /// </summary>
    /// <param name="num">예약된 Schedule 순서</param>
    public void SetExperimentMonitor(int num) => HUDScreen[EUiScreen.Experiment.ToString()].GetComponent<Monitor>().SetExperimentMonitor(Manager.Experiment.CallSchedule(num));
    /// <summary>
    ///  ExperimentBox에서 어떤 실험 스케줄을 선택했는지 Monitor에 전달하는 메서드
    /// </summary>
    /// <param name="selectedSchedule">선택된 실험 Shedule</param>
    public void SetExperimentMonitor(ExperimentWrapper selectedSchedule) => HUDScreen[EUiScreen.Experiment.ToString()].GetComponent<Monitor>().SetExperimentMonitor(selectedSchedule);
    #endregion


}
