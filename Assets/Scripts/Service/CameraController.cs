using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Splines;

public enum ECameraPosition
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
    [Header("Cinemachine")]
    [SerializeField] private CinemachineSplineCart cart;

    [Header("Move Setting")]
    [SerializeField] private float moveDuration = 3f;

    private Coroutine moveCoroutine;

    private void Start()
    {
        InitCart();
    }

    private bool InitCart()
    {
        if (cart == null)
            cart = GetComponent<CinemachineSplineCart>();

        if (cart == null)
        {
            Debug.LogError("CinemachineSplineCart가 연결되지 않았습니다.");
            return false;
        }

        cart.PositionUnits = PathIndexUnit.Knot;
        return true;
    }

    public void SetCameraPosition(string str)
    {
        if (!System.Enum.TryParse(str, out ECameraPosition cameraPosition))
        {
            Debug.LogWarning($"존재하지 않는 카메라 위치입니다: {str}");
            return;
        }

        SetCameraPosition(cameraPosition);
    }

    public void SetCameraPosition(ECameraPosition cameraPosition)
    {
        switch (cameraPosition)
        {
            case ECameraPosition.Idle:
                MoveToKnot(0);
                break;

            case ECameraPosition.TestBad:
                MoveToKnot(1);
                break;

            case ECameraPosition.AmmoniaSupply:
                MoveToKnot(2);
                break;

            case ECameraPosition.WaterSupply:
                MoveToKnot(3);
                break;

            case ECameraPosition.Experiemnt:
                MoveToKnot(4);
                break;

            case ECameraPosition.TreatedWater:
                MoveToKnot(5);
                break;
        }
    }

    public void SetCameraPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void MoveToKnot(int knotIndex)
    {
        if (!InitCart())
            return;

        cart.PositionUnits = PathIndexUnit.Knot;

        float start = cart.SplinePosition;
        float end = knotIndex;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveCart(start, end));
    }

    private IEnumerator MoveCart(float start, float end)
    {
        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.deltaTime;

            float t = time / moveDuration;
            t = Mathf.Clamp01(t);
            t = Mathf.SmoothStep(0f, 1f, t);

            cart.SplinePosition = Mathf.Lerp(start, end, t);

            yield return null;
        }

        cart.SplinePosition = end;
        moveCoroutine = null;
    }
}