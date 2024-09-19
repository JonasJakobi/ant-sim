using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : Singleton<GameManager>
{

    public UnityEvent onPointOneSecondTick = new UnityEvent();
    public UnityEvent onPointTwoSecondTick = new UnityEvent();
    public UnityEvent onOneSecondTick = new UnityEvent();

    private void Start()
    {
        StartCoroutine(PointTwoSecondTickCoroutine());
        StartCoroutine(OneSecondTickCoroutine());
        StartCoroutine(PointOneSecondTickCoroutine());
    }

    private IEnumerator PointTwoSecondTickCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);
        while (true)
        {
            yield return wait;
            onPointTwoSecondTick?.Invoke();
        }
    }
    private IEnumerator PointOneSecondTickCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.1f);
        while (true)
        {
            yield return wait;
            onPointOneSecondTick?.Invoke();
        }
    }

    private IEnumerator OneSecondTickCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(1f);
        while (true)
        {
            yield return wait;
            onOneSecondTick?.Invoke();
        }
    }

}
