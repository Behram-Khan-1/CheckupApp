using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public static float TimerStart( float time)
    {
        time = time - Time.deltaTime;

        return time;
    }
}
