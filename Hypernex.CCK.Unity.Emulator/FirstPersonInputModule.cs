using UnityEngine;
using UnityEngine.EventSystems;

public class FirstPersonInputModule : StandaloneInputModule
{
    protected override MouseState GetMousePointerEventData(int id)
    {
        var lockState = Cursor.lockState;
        Cursor.lockState = CursorLockMode.Confined;
        var mouseState = base.GetMousePointerEventData(id);
        Cursor.lockState = lockState;
        return mouseState;
    }

    protected override void ProcessMove(PointerEventData pointerEvent)
    {
        var lockState = Cursor.lockState;
        Cursor.lockState = CursorLockMode.Confined;
        base.ProcessMove(pointerEvent);
        Cursor.lockState = lockState;
    }

    protected override void ProcessDrag(PointerEventData pointerEvent)
    {
        var lockState = Cursor.lockState;
        Cursor.lockState = CursorLockMode.Confined;
        base.ProcessDrag(pointerEvent);
        Cursor.lockState = lockState;
    }
}