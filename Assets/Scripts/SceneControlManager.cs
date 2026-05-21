using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneControlManager : ManagerBase
{
    // SceneControlManager
    //  - 각 씬마다 하나씩 존재하는 매니저로, 씬의 전반적인 제어를 담당한다.(싱글톤 구현은 하지 않는다.)
    //  - SceneControlManager는 씬의 전반적인 제어를 담당하는 매니저로, 씬의 초기화, 씬 전환, 씬 내 이벤트 관리 등을 담당한다.
    //  - SceneControlManager는 각 씬의 모든 오브젝트를 관리한다.

    protected override void OnEnable()
    {
        base.OnEnable();
        //Manager.SetSceneManager(this);
    }

    protected override void Intialize()
    {
        MainCamera = Camera.main;
    }

    #region Scene Management System
    // TODO : 씬 관리 시스템을 구현하는 메서드들을 추가할 수 있다. 예를 들어, 씬 전환, 씬 초기화, 씬 내 이벤트 관리 등을 담당하는 메서드들을 추가할 수 있다.

    #endregion

    #region Camera Control System

    private Camera MainCamera;

    public void OnChangeCameraView(object obj)
    {
        // TODO : 카메라 뷰 변경 로직을 구현하는 메서드.
        if (obj.GetType() != typeof(Datas)) return;
    }
    #endregion
}
