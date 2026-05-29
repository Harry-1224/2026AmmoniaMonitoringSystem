using UnityEngine;
using UnityEngine.EventSystems;

public abstract class UIButtonInteractionBase : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IHoverable,
    IClickable
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickAction();
    }

    public abstract void OnHoverEnter();
    public abstract void OnHoverExit();
    public abstract void OnClickAction();
}