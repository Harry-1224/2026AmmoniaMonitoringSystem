using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Splines;

public class DollyCartMover : MonoBehaviour
{
    [SerializeField] private CinemachineSplineCart cart;
    [SerializeField] private float moveDuration = 3f;

    private Coroutine moveCoroutine;

    private void Start()
    {
        if (cart == null)
            cart = GetComponent<CinemachineSplineCart>();

        if (cart == null)
        {
            Debug.LogError("CinemachineSplineCart가 연결되지 않았습니다.");
            return;
        }
    }
    public void MoveToKnot(int knotIndex)
    {
        if (cart == null)
            cart = GetComponent<CinemachineSplineCart>();

        if (cart == null)
        {
            Debug.LogError("CinemachineSplineCart가 연결되지 않았습니다.");
            return;
        }

        cart.PositionUnits = PathIndexUnit.Knot;

        float start = cart.SplinePosition;
        float end = knotIndex;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveCart(start, end));
    }

    public void MoveToKnot0()
    {
        MoveToKnot(0);
    }

    public void MoveToKnot1()
    {
        MoveToKnot(1);
    }

    public void MoveToKnot2()
    {
        MoveToKnot(2);
    }
    public void MoveToKnot3()
    {
        MoveToKnot(3);
    }
    public void MoveToKnot4()
    {
        MoveToKnot(4);
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