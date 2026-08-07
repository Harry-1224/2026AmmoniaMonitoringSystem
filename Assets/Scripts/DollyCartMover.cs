using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Splines;

public class DollyCartMover : MonoBehaviour
{
    [SerializeField] private CinemachineSplineCart cart;

    [Header("이동 속도 (m/s)")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("가속 / 감속")]
    [SerializeField] private bool useSmoothStep = true;

    private Coroutine moveCoroutine;

    private void Awake()
    {
        if (cart == null)
            cart = GetComponent<CinemachineSplineCart>();

        if (cart == null)
            Debug.LogError("CinemachineSplineCart가 연결되지 않았습니다.");
    }

    public void MoveToKnot(int knotIndex)
    {
        if (cart == null)
        {
            Debug.LogError("CinemachineSplineCart가 연결되지 않았습니다.");
            return;
        }

        if (cart.Spline == null)
        {
            Debug.LogError("SplineContainer가 연결되지 않았습니다.");
            return;
        }

        Spline spline = cart.Spline.Spline;

        // 현재 위치를 Distance 단위로 변환
        float currentDistance =
            spline.ConvertIndexUnit(
                cart.SplinePosition,
                cart.PositionUnits,
                PathIndexUnit.Distance
            );

        // 목적 Knot을 Distance 단위로 변환
        float targetDistance =
            spline.ConvertIndexUnit(
                knotIndex,
                PathIndexUnit.Knot,
                PathIndexUnit.Distance
            );

        cart.PositionUnits = PathIndexUnit.Distance;
        cart.SplinePosition = currentDistance;

        float distance =
            Mathf.Abs(targetDistance - currentDistance);

        float duration =
            distance / Mathf.Max(moveSpeed, 0.01f);

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(
            MoveCart(
                currentDistance,
                targetDistance,
                duration
            )
        );
    }

    public void MoveToKnot0() => MoveToKnot(0);
    public void MoveToKnot1() => MoveToKnot(1);
    public void MoveToKnot2() => MoveToKnot(2);
    public void MoveToKnot3() => MoveToKnot(3);
    public void MoveToKnot4() => MoveToKnot(4);

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
                t = Mathf.SmoothStep(0f, 1f, t);

            cart.SplinePosition =
                Mathf.Lerp(start, end, t);

            yield return null;
        }

        cart.SplinePosition = end;
        moveCoroutine = null;
    }
}