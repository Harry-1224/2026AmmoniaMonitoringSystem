using TMPro;
using UnityEngine;

public enum UiAreaType
{
    None,
    TopBar,
    BottomBar,
    MainMenu,
    Settings,
    PauseMenu,
    GameOver,
    Inventory,
    Map,
    QuestLog,
    CharacterStats,
    Dialogue,
    Shop,
    Crafting,
    Achievements,
    Leaderboard
}

public class UiArea : UiObjectBase
{
    public UiAreaType type;

    [Header("TopBar Setting")]
    public TextMeshProUGUI Clock;

    protected override void Initialize()
    {
        base.Initialize();

        if (type == UiAreaType.TopBar)
        {
            UpdateClock();
        }
    }

    protected override void Update()
    {
        base.Update();

        if (type == UiAreaType.TopBar)
        {
            UpdateClock();
        }
    }
    private void UpdateClock()
    {
        if (Clock == null) return;

        Clock.text = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
