using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MachineControl : MonoBehaviour
{
    [SerializeField] private IceCreamCreator creamCreator;

    [SerializeField] private CustomButton isStrawberry;
    [SerializeField] private CustomButton isChocolate;
    [SerializeField] private CustomButton isVanilla;
    [SerializeField] private float inputCheckRate; // for buffering input for 2 buttons

    private void Start()
    {
        StartCoroutine(InputUpdate());
    }

    private IEnumerator InputUpdate()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(1 / inputCheckRate);
        while (true)
        {
            if (isStrawberry.IsDown||Input.GetKey(KeyCode.S))
            {
                if (isChocolate.IsDown||Input.GetKey(KeyCode.C))
                {
                    creamCreator.ChangeState(IceCreamCreator.MachineState.StrawberryAndChocolate);
                }
                else if (isVanilla.IsDown || Input.GetKey(KeyCode.V))
                {
                    creamCreator.ChangeState(IceCreamCreator.MachineState.VanillaAndStrawberry);
                }
                else
                {
                    creamCreator.ChangeState(IceCreamCreator.MachineState.Strawberry);
                }
            }
            else if (isChocolate.IsDown || Input.GetKey(KeyCode.C))
            {
                if (isVanilla.IsDown || Input.GetKey(KeyCode.V))
                {
                    creamCreator.ChangeState(IceCreamCreator.MachineState.VanillaAndChocolate);
                }
                else
                {
                    creamCreator.ChangeState(IceCreamCreator.MachineState.Chocolate);
                }
            }
            else if(isVanilla.IsDown || Input.GetKey(KeyCode.V))
            {
                creamCreator.ChangeState(IceCreamCreator.MachineState.Vanilla);
            }
            else
            {
                creamCreator.ChangeState(IceCreamCreator.MachineState.None);
            }

            yield return waitForSeconds;
        }
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
