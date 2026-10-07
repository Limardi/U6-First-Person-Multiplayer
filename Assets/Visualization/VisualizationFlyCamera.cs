using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleWitches.Visualization
{
    public sealed class VisualizationFlyCamera : MonoBehaviour
    {
        float yaw, pitch;

        void Start() { yaw = transform.eulerAngles.y; pitch = transform.eulerAngles.x; }

        void Update()
        {
            if (Keyboard.current == null || Mouse.current == null || !Mouse.current.rightButton.isPressed) return;
            Vector2 delta = Mouse.current.delta.ReadValue() * 0.09f;
            yaw += delta.x; pitch = Mathf.Clamp(Mathf.DeltaAngle(0, pitch - delta.y), -85, 85);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            var keys = Keyboard.current;
            Vector3 direction = new Vector3((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.eKey.isPressed ? 1 : 0) - (keys.qKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            transform.position += transform.TransformDirection(direction) * Time.deltaTime * (keys.leftShiftKey.isPressed ? 9 : 3);
        }
    }
}
