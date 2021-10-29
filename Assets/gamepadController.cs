using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gamepadController : MonoBehaviour
{

    public float _rotationSensitivity= 5f;
    public float _translationSensitivity= 5f;

    [Space]
    public float vAxis;
    public float hAxis;

    public float movex;
    public float movez;

    public float lTrigger;
    public float rTrigger;


    void Start()
    {
        
    }

    void Update()
    {
        movez = Input.GetAxis("Vertical");
        movex = Input.GetAxis("Horizontal");

        hAxis = Input.GetAxis("MoveX");
        vAxis = Input.GetAxis("MoveY");

        // dpadx = Input.GetAxis("DPADHorizontal");
        // dpady = Input.GetAxis("DPADVertical");

        lTrigger = Input.GetAxis("LeftBumper");
        rTrigger = Input.GetAxis("RightBumper");

        if ( Mathf.Abs(hAxis) > 0.001f)
        {
            transform.Rotate(Vector3.down, -hAxis * Time.deltaTime * _rotationSensitivity);
        }

        if ( Mathf.Abs(vAxis) > 0.001f)
        {
            transform.Rotate(Vector3.left, -vAxis * Time.deltaTime * _rotationSensitivity);
        }

        if ( Mathf.Abs(movex) > 0.001f)
        {
            transform.localPosition += transform.right * movex * Time.deltaTime * _translationSensitivity;
        }

        if ( Mathf.Abs(movez) > 0.001f)
        {
            transform.localPosition -= transform.forward * movez * Time.deltaTime * _translationSensitivity;
        }
        
        if ( Mathf.Abs(lTrigger) > 0.001f)
        {
            transform.localPosition += transform.up * lTrigger * Time.deltaTime * _translationSensitivity;
        }
                
        if ( Mathf.Abs(rTrigger) > 0.001f)
        {
            transform.localPosition += transform.up * rTrigger * Time.deltaTime * _translationSensitivity;
        }

    }
}
