using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : Singleton<GameManager>
{


    public UnityEvent onPointTwoSecondTick = new UnityEvent();
    public UnityEvent onOneSecondTick = new UnityEvent();

    private void Start()
    {
        StartCoroutine(PointTwoSecondTickCoroutine());
        StartCoroutine(OneSecondTickCoroutine());
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
