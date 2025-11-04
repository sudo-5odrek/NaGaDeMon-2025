using UnityEngine;

public class InputContextManager : MonoBehaviour
{
    public static InputContextManager Instance { get; private set; }

    public enum InputMode { Normal, Build }
    public InputMode CurrentMode { get; private set; } = InputMode.Normal;

    public InputSystem_Actions input; // your generated class

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

        switch (mode)
        {
            case InputMode.Normal:
                input.Player.Attack.Enable();
                //input.Player.Move.Enable();
                //input.Player.Look.Enable();

                input.Player.Place.Disable();
                input.Player.Cancel.Disable();
                break;

            case InputMode.Build:
                input.Player.Attack.Disable();
                //input.Player.Move.Disable();
                //input.Player.Look.Disable();

                input.Player.Place.Enable();
                input.Player.Cancel.Enable();
                break;
        }
    }
}