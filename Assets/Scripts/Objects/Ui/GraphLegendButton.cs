using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GraphLegendButton : UiObjectBase
{
    [SerializeField] private Image CheckImage;
    [SerializeField] private TMP_Text TagText;
    [SerializeField] private bool IsActive = true;
    [SerializeField] private string Tag;

    public event Action<string ,bool> OnClickButton;

    public void SetButtonTag(string tag)
    {
        Tag = tag;
        
        gameObject.name = Tag;
        TagText.text = Tag;
    }

    public override void OnClick()
    {
        IsActive = !IsActive;
        CheckImage.enabled = IsActive;
        OnClickButton?.Invoke(Tag, IsActive);

        EventSystem.current?.SetSelectedGameObject(null);
    }
}
