using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MachineControl : MonoBehaviour
{
    [SerializeField] private IceCreamCreator creamCreator;

    private bool _isStrawberry;
    private bool _isChocolate;
    private bool _isVanilla;
    private void Update()
    {
        if (_isStrawberry||Input.GetKey(KeyCode.S))
        {
            if (_isChocolate||Input.GetKey(KeyCode.C))
            {
                creamCreator.ChangeState(IceCreamCreator.MachineState.StrawberryAndChocolate);
            }
            else if (_isVanilla || Input.GetKey(KeyCode.V))
            {
                creamCreator.ChangeState(IceCreamCreator.MachineState.VanillaAndStrawberry);
            }
            else
            {
                creamCreator.ChangeState(IceCreamCreator.MachineState.Strawberry);
            }
        }
        else if (_isChocolate || Input.GetKey(KeyCode.C))
        {
            if (_isVanilla || Input.GetKey(KeyCode.V))
            {
                creamCreator.ChangeState(IceCreamCreator.MachineState.VanillaAndChocolate);
            }
            else
            {
                creamCreator.ChangeState(IceCreamCreator.MachineState.Chocolate);
            }
        }
        else if(_isVanilla || Input.GetKey(KeyCode.V))
        {
            creamCreator.ChangeState(IceCreamCreator.MachineState.Vanilla);
        }
        else
        {
            creamCreator.ChangeState(IceCreamCreator.MachineState.None);
        }
    }

    public void StrawberryPressed()
    {
        
    }

    public void ChocolatePressed()
    {
        
    }

    public void VanillaPressed()
    {
        
    }
}
