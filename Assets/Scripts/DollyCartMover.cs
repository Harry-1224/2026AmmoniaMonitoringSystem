using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Splines;

public class DollyCartMover : MonoBehaviour
{
    public enum MoveTargetType
    {
        Knot,           // 0, 1, 2, 3...
        Distance,       // 실제 거리(m)
        Normalized      // 0.0 ~ 1.0
    }

    [Header("References")]
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
            Debug.LogError(
                "CinemachineSplineCart가 연결되지 않았습니다.",
                this
            );
    }

    // =========================================================
    // INT 데이터
    // Knot 번호로 이동
    // =========================================================

    public void MoveToKnot(int knotIndex)
    {
        if (!TryGetSpline(out Spline spline))
            return;

        if (knotIndex < 0 || knotIndex >= spline.Count)
        {
            Debug.LogWarning(
                $"잘못된 Knot Index입니다. " +
                $"입력: {knotIndex}, " +
                $"유효범위: 0 ~ {spline.Count - 1}",
                this
            );

            return;
        }

        float targetDistance =
            spline.ConvertIndexUnit(
                knotIndex,
                PathIndexUnit.Knot,
                PathIndexUnit.Distance
            );

        MoveToDistance(targetDistance);
    }

    // =========================================================
    // FLOAT 데이터
    // 실제 거리(m)로 이동
    // =========================================================

    public void MoveToDistance(float targetDistance)
    {
        if (!TryGetSpline(out Spline spline))
            return;

        float splineLength = spline.GetLength();

        targetDistance =
            Mathf.Clamp(
                targetDistance,
                0f,
                splineLength
            );

        float currentDistance =
            GetCurrentDistance(spline);

        StartMove(
            currentDistance,
            targetDistance
        );
    }

    // =========================================================
    // FLOAT 데이터
    // 0 ~ 1 값으로 이동
    //
    // 0   = 시작
    // 0.5 = 중간
    // 1   = 끝
    // =========================================================

    public void MoveToNormalized(float normalizedPosition)
    {
        if (!TryGetSpline(out Spline spline))
            return;

        normalizedPosition =
            Mathf.Clamp01(normalizedPosition);

        float targetDistance =
            spline.ConvertIndexUnit(
                normalizedPosition,
                PathIndexUnit.Normalized,
                PathIndexUnit.Distance
            );

        MoveToDistance(targetDistance);
    }

    // =========================================================
    // 데이터 타입까지 같이 받을 경우
    // =========================================================

    public void MoveTo(
        float value,
        MoveTargetType targetType)
    {
        switch (targetType)
        {
            case MoveTargetType.Knot:

                MoveToKnot(
                    Mathf.RoundToInt(value)
                );

                break;


            case MoveTargetType.Distance:

                MoveToDistance(value);

                break;


            case MoveTargetType.Normalized:

                MoveToNormalized(value);

                break;
        }
    }

    // =========================================================
    // 현재 위치 -> Distance
    // =========================================================

    private float GetCurrentDistance(Spline spline)
    {
        return spline.ConvertIndexUnit(
            cart.SplinePosition,
            cart.PositionUnits,
            PathIndexUnit.Distance
        );
    }

    // =========================================================
    // 실제 이동 시작
    // =========================================================

    private void StartMove(
        float startDistance,
        float targetDistance)
    {
        cart.PositionUnits =
            PathIndexUnit.Distance;

        cart.SplinePosition =
            startDistance;

        float distance =
            Mathf.Abs(
                targetDistance -
                startDistance
            );

        float duration =
            distance /
            Mathf.Max(
                moveSpeed,
                0.01f
            );

        if (moveCoroutine != null)
        {
            StopCoroutine(
                moveCoroutine
            );
        }

        moveCoroutine =
            StartCoroutine(
                MoveCart(
                    startDistance,
                    targetDistance,
                    duration
                )
            );
    }

    // =========================================================
    // 이동 Coroutine
    // =========================================================

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
            elapsedTime +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsedTime /
                    duration
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

    // =========================================================
    // Spline 확인
    // =========================================================

    private bool TryGetSpline(
        out Spline spline)
    {
        spline = null;

        if (cart == null)
        {
            Debug.LogError(
                "CinemachineSplineCart가 없습니다.",
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

        spline = cart.Spline.Spline;

        return true;
    }

    // =========================================================
    // 외부에서 이동 중지할 때
    // =========================================================

    public void StopMove()
    {
        if (moveCoroutine == null)
            return;

        StopCoroutine(moveCoroutine);

        moveCoroutine = null;
    }
}