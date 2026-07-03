using UnityEngine;

enum ECameraPosition
{
    Idle,
    TestBad,
    AmmoniaSupply,
    WaterSupply,
    Experiemnt,
    TreatedWater,
}

public class CameraController : MonoBehaviour
{
    public void SetCameraPosition(string str)
    {
        switch (str)
        {
            case nameof(ECameraPosition.Idle):
                break;
            case nameof(ECameraPosition.TestBad):
                break;
            case nameof(ECameraPosition.AmmoniaSupply):
                break;
            case nameof(ECameraPosition.WaterSupply):
                break;
            case nameof(ECameraPosition.TreatedWater):
                break;
        }
    }

    public void SetCameraPosition(Vector3 position)
    {
        transform.position = position;
    }
}
