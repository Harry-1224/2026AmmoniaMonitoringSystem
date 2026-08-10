using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UiObjectBase : ObjectBase, IUiObject,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("UI Setting")]
    public bool isManagable = false; // UiManager에서 관리할지 여부
    protected DollyCartMover cameraController;

    protected override void Initialize()
    {
        base.Initialize();

        if(isManagable) Manager.Ui.RegistUiObject(ObjectID, this);

        if (Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<DollyCartMover>();

            if (cameraController == null)
            {
                Debug.LogWarning("[UiObject] MainCamera에 CameraController가 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[UiObject] MainCamera를 찾을 수 없습니다.");
        }
    }
    
    #region Ui System
    public virtual void OnClick()
    {

    }
    public virtual void OnHoverEnter()
    {

    }
    public virtual void OnHoverExit()
    {

    }
    public virtual void OnPointerDown()
    {

    }
    public virtual void OnPointerUp()
    {

    }
    public virtual void OnDrag()
    {

    }

    // Unity 이벤트 → 인터페이스 연결
    public void OnPointerClick(PointerEventData eventData) => OnClick();
    public void OnPointerEnter(PointerEventData eventData) => OnHoverEnter();
    public void OnPointerExit(PointerEventData eventData) => OnHoverExit();
    public void OnPointerDown(PointerEventData eventData) => OnPointerDown();
    public void OnPointerUp(PointerEventData eventData) => OnPointerUp();

    #endregion


    #region Monitoring System
    private Datas currentData = new Datas();

    protected virtual void OnDataChanged(Dictionary<string, Datas> obj) => currentData = obj[ObjectID];
    #endregion
}
