using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class UIButtonColorInteraction : UiObjectBase 
{ [Header("Target")][SerializeField] private Image targetImage; 
    [Header("Interaction Colors")]
    [SerializeField] private Color hoverColor = new Color(1f, 0.55f, 0f, 1f); 
    [SerializeField] private Color clickColor = new Color(0.1f, 0.35f, 1f, 1f);
    [Header("Option")][SerializeField] private float clickColorDuration = 0.12f;
    private Color originalColor; private bool isHovering; 
    private Coroutine clickRoutine;
    private void Awake() 
    { 
        if (targetImage == null) 
        { 
            targetImage = GetComponent<Image>(); 
        } 
        if(targetImage == null) 
        { 
            Debug.LogError($"{name} : Image 컴포넌트가 없습니다."); 
            enabled = false; return; 
        } 
        originalColor = targetImage.color; 
    } 
    public override void OnHoverEnter() 
    { 
        isHovering = true;
        if (clickRoutine != null)
        {
            StopCoroutine(clickRoutine);
            clickRoutine = null; 
        } targetImage.color = hoverColor; 
    } 
    public override void OnHoverExit() 
    { 
        isHovering = false; 
        if (clickRoutine != null)
        { 
            StopCoroutine(clickRoutine);
            clickRoutine = null; 
        } targetImage.color = originalColor; 
    } 
    public override void OnClick() 
    { 
        if (clickRoutine != null)
        { 
            StopCoroutine(clickRoutine); 
        } 
        clickRoutine = StartCoroutine(ClickColorRoutine());
    } 
    private IEnumerator ClickColorRoutine() 
    { 
        targetImage.color = clickColor;
        yield return new WaitForSeconds(clickColorDuration); 
        targetImage.color = isHovering ? hoverColor : originalColor;
        clickRoutine = null;
    } 
}