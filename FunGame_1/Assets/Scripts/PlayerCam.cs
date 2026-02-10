using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX = 200f;
    public float sensY = 200f;

    public Transform playerBody; // isi dengan PlayerObj

    float xRotation = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Pitch: kamera atas-bawah
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Yaw: putar badan player kiri-kanan
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
