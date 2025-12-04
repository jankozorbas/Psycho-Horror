using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public bool CanMoveBody = true, CanMoveCamera = true;

    
    [SerializeField] private float lookSensitivity = 1f;
    [SerializeField] private float movSpeed = 5f;
    [SerializeField] private float gravityModifier = -5f;
    [SerializeField] private float maxFallingSpeed = 10f;
    [SerializeField] private Camera camera;

    private CharacterController controller;
    private Vector2 moveInput, lookInput;
    private Vector3 velocity;
    private float camPitch = 0f, camYaw = 0f;

    // editor applicable only
    private bool cursorLocked;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();

        // hide mouse & lock to centre (toggle this with the "L" key. ONLY in editor)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (CanMoveBody) { PreformMovement(); }
        if (CanMoveCamera) { RotateCamera(); }



    }

    private void RotateCamera()
    {
        float mouseX = lookInput.x * lookSensitivity;
        float mouseY = lookInput.y * lookSensitivity;

        // Pitch (camera up/down)
        camPitch -= mouseY;
        camYaw -= mouseX;
        camPitch = Mathf.Clamp(camPitch, -80f, 80f);

        camera.transform.localRotation = Quaternion.Euler(camPitch, -camYaw, 0f);
    }


    private void PreformMovement()
    {
        // getting the transform direction
        Vector3 cameraForward = camera.transform.forward;
        Vector3 cameraRight = camera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();


        // Moving horizontaly based on the Transforms direction
        Vector3 move = cameraForward * moveInput.y + cameraRight * moveInput.x;
        controller.Move(move * movSpeed * Time.deltaTime);

        // Falling based on Gravity
        velocity.y += gravityModifier * Time.deltaTime;
        if(velocity.y < -maxFallingSpeed) velocity.y = -maxFallingSpeed; // Clamp the valocity on Y
        controller.Move(velocity * Time.deltaTime);
    }

    public void ToggleCamera(InputAction.CallbackContext context)
    {
        if (Application.isEditor)
        {
            if(cursorLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                cursorLocked = false;
                CanMoveCamera = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                cursorLocked = true;
                CanMoveCamera = true;
            }
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
