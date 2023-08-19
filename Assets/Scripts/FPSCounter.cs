using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
        text.text = ((int)(1 / Time.deltaTime)).ToString();
    }
}
