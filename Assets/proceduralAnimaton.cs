using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class proceduralAnimaton : MonoBehaviour
{
    [SerializeField, Range(0,1)] float _seek;
    [SerializeField] bool _enableSeek;

    [SerializeField] float _duration;
    [SerializeField] Vector3 _baseEuler;
    [SerializeField] Vector3 _ampEuler;
    [SerializeField] Vector3 _phaseEuler;


    void Start()
    {
        
    }

    void Evaluate(float t) 
    {
        Vector3 e = _baseEuler + new Vector3(
            _ampEuler.x * Mathf.Sin(t * Mathf.PI + _phaseEuler.x),
            _ampEuler.y * Mathf.Sin(t * Mathf.PI + _phaseEuler.y),
            _ampEuler.z * Mathf.Sin(t * Mathf.PI + _phaseEuler.z));
        

        transform.localRotation = Quaternion.Euler(e.x, e.y, e.z);
    }

    void Update()
    {
        if(_enableSeek) {
            Evaluate(_seek);
        }
        else
        {
            Evaluate(Time.time / _duration);
        }
    }
}
