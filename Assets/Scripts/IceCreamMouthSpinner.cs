using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class IceCreamMouthSpinner : MonoBehaviour
{
    [SerializeField] private Transform center;
    [SerializeField] private Transform mouth;
    [Tooltip("Rotations per seconds")]
    [SerializeField] private float rotateSpeed;
    [SerializeField] private float closeMoveSpeed;
    private float _radius;
    private float _angle;
    
    private void Start()
    {
        mouth.transform.Translate(0,center.transform.position.y-mouth.transform.position.y,0);
        _radius = Vector3.Distance(mouth.transform.position, center.transform.position);
    }

    private void Update()
    {
        if (_radius<.1)
        {
            return;
        }
        mouth.transform.position = new Vector3(_radius*Mathf.Cos(_angle*2*Mathf.PI),mouth.transform.position.y+Time.deltaTime*.2f,_radius*Mathf.Sin(_angle*2*Mathf.PI));
        _angle += rotateSpeed * Time.deltaTime;
        _radius -= closeMoveSpeed * Time.deltaTime;
    }
}
