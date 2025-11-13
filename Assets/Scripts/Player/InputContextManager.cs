using System;
using UnityEngine;

public class InputContextManager : MonoBehaviour
{
    public static InputContextManager Instance { get; private set; }

    public enum InputMode { Normal, Build, Connect }
    public InputMode CurrentMode { get; private set; } = InputMode.Normal;

    public InputSystem_Actions input; // your generated class
    
    public event Action OnContextChange;

    private void Awake()
    {
        Instance = this;
        input = new InputSystem_Actions();
        input.Enable();

        SetInputMode(InputMode.Normal);
    }

    public void SetInputMode(InputMode mode)
    {
        CurrentMode = mode;
        OnContextChange?.Invoke();
        
        switch (mode)
        {
            case InputMode.Normal:
                input.Player.Attack.Enable();

                input.Player.Place.Disable();
                input.Player.Cancel.Disable();
                
                input.Player.BuildMenu.Enable();
                input.Player.ConnectMode.Enable();
                break;

            case InputMode.Build:
                input.Player.Attack.Disable();
                input.Player.ConnectMode.Disable();

                input.Player.Place.Enable();
                input.Player.Cancel.Enable();
                break;
            
            case InputMode.Connect:
                input.Player.Attack.Disable();

                input.Player.BuildMenu.Disable();
                input.Player.Place.Enable();
                input.Player.Cancel.Enable();
                break;
        }
    }
}