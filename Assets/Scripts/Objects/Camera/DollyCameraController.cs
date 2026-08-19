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
    Experiment,
    TreatedWater,
}

public class DollyCameraController : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineSplineCart cart;

    [Header("이동 속도 (m/s)")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("가속 / 감속")]
    [SerializeField] private bool useSmoothStep = true;

    private Coroutine moveCoroutine;

    private void Awake()
    {
        InitCart();
    }

    private bool InitCart()
    {
        if (cart == null)
            cart = GetComponent<CinemachineSplineCart>();

        if (cart == null)
        {
            Debug.LogError(
                "CinemachineSplineCart가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (cart.Spline == null)
        {
            Debug.LogError(
                "SplineContainer가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        return true;
    }

    // =========================================================
    // 문자열 데이터 수신용
    // 문자열로 호출을 원할 시 
    //SetDollyPosition("AmmoniaSupply");
    // =========================================================
    public void SetDollyPosition(string str)
    {
        if (!System.Enum.TryParse(str, true, out ECameraPosition dollyPosition))
        {
            Debug.LogWarning( $"존재하지 않는 Dolly 위치입니다: {str}",this);

            return;
        }

        SetDollyPosition(dollyPosition);
    }


    // =========================================================
    // Enum -> Knot 매핑
    // =========================================================
    public void SetDollyPosition(ECameraPosition dollyPosition)
    {
        switch (dollyPosition)
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

            case ECameraPosition.TreatedWater:
                MoveToKnot(3);
                break;

            case ECameraPosition.WaterSupply:
                MoveToKnot(4);
                break;

            case ECameraPosition.Experiment:
                MoveToKnot(5);
                break;


            default:
                Debug.LogWarning(
                    $"처리되지 않은 Dolly 위치입니다: {dollyPosition}",
                    this
                );
                break;
        }
    }

    // =========================================================
    // 실제 Knot 이동
    // =========================================================
    public void MoveToKnot(int knotIndex)
    {
        if (!InitCart())
            return;

        Spline spline = cart.Spline.Spline;

        if (knotIndex < 0 || knotIndex >= spline.Count)
        {
            Debug.LogWarning(
                $"잘못된 Knot Index입니다. " +
                $"입력: {knotIndex}, " +
                $"범위: 0 ~ {spline.Count - 1}",
                this
            );

            return;
        }

        // 현재 위치를 Distance 단위로 변환
        float currentDistance =
            spline.ConvertIndexUnit(
                cart.SplinePosition,
                cart.PositionUnits,
                PathIndexUnit.Distance
            );

        // 목표 Knot을 Distance 단위로 변환
        float targetDistance =
            spline.ConvertIndexUnit(
                knotIndex,
                PathIndexUnit.Knot,
                PathIndexUnit.Distance
            );

        cart.PositionUnits =
            PathIndexUnit.Distance;

        cart.SplinePosition =
            currentDistance;

        float distance =
            Mathf.Abs(
                targetDistance -
                currentDistance
            );

        float duration =
            distance /
            Mathf.Max(
                moveSpeed,
                0.01f
            );

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine =
            StartCoroutine(
                MoveCart(
                    currentDistance,
                    targetDistance,
                    duration
                )
            );
    }

    private IEnumerator MoveCart(
        float start,
        float end,
        float duration)
    {
        if (Mathf.Approximately(start, end))
        {
            cart.SplinePosition = end;
            moveCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsedTime / duration
                );

            if (useSmoothStep)
            {
                t =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t
                    );
            }

            cart.SplinePosition =
                Mathf.Lerp(
                    start,
                    end,
                    t
                );

            yield return null;
        }

        cart.SplinePosition = end;
        moveCoroutine = null;
    }

    public void StopMove()
    {
        if (moveCoroutine == null)
            return;

        StopCoroutine(moveCoroutine);
        moveCoroutine = null;
    }
}