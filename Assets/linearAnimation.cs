using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class linearAnimation : MonoBehaviour
{

    public float _duration = 1;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float t = Mathf.Repeat(Time.time, _duration)/_duration;
        transform.rotation = Quaternion.Euler(0, 360f * t, 0);
    }
}
