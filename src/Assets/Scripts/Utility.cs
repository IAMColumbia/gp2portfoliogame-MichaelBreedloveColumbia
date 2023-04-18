using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utility : MonoBehaviour
{
    float TimeSlow_FadeOutTime = 0f;
    bool TimeSlow_Active = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        TimeSlow_Update();
    }

    public void TimeSlow_Update()
    {
        if (TimeSlow_Active)
        {
           if (Time.time >= TimeSlow_FadeOutTime) { Time.timeScale = 1f; TimeSlow_Active = false; }
        }
    }

    public void SlowTime(float scale, float duration)
    {
        Time.timeScale = scale;
        TimeSlow_Active = true;
        TimeSlow_FadeOutTime = Time.time + duration;
    }
}
