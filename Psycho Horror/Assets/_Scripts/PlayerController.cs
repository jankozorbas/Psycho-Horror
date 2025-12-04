using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public bool CanMoveBody = true, CanMoveCamera = true, CanMoveObjects = true;

    
    [SerializeField] private float lookSensitivity = 1f;
    [SerializeField] private float movSpeed = 5f;
    [SerializeField] private float gravityModifier = -5f;
    [SerializeField] private float maxFallingSpeed = 10f;
    [SerializeField] private float objectMovingStrenght = 2f;
    [SerializeField] private Camera camera;
    [SerializeField] private GameObject crosshairImageObject, objectMoverReferancePointObject;
    [SerializeField] private LayerMask interactableLayerMask;
    [SerializeField] private float interactDistance = 5f;

    private CharacterController controller;
    private Vector2 moveInput, lookInput;
    private Vector3 velocity;
    private float camPitch = 0f, camYaw = 0f;
    

    // used for object movement
    private bool isLookingAtInteractable = false, isMovingInteractable = false, clickHeld = false;
    private GameObject objectBeingLookedAt;
    private Rigidbody movingObjectRB;
    private InteractableObjectStatistics objectStats;


    // editor variables 
    private bool cursorLocked;


    void Start()
    {
        // disable the crosshair image
        crosshairImageObject.SetActive(false);

        // Get the stuff that is on the object that we need
        controller = GetComponent<CharacterController>();

        // hide mouse & lock to centre (toggle this with the "L" key. ONLY in editor)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    // Did you know Update is called once per frame?
    void Update()
    {
        if (CanMoveBody) { PerformMovement(); }
        if (CanMoveCamera) { RotateCamera(); }
        if (CanMoveObjects) { MoveObjectBasedOnInput(); }
        else { if(crosshairImageObject.activeInHierarchy) crosshairImageObject.SetActive(false); } 

    }

    private void MoveObjectBasedOnInput()
    {
        LookforObjects();
        if (clickHeld)
        {
            if(movingObjectRB != null)
            {
                Vector3 dir = objectMoverReferancePointObject.transform.position - movingObjectRB.transform.position;
                dir = dir.normalized;
                float distanceFactor = Vector3.Distance(objectMoverReferancePointObject.transform.position, movingObjectRB.transform.position);
                if (distanceFactor > 1) { distanceFactor = 1; }

                float weightModifier = 1 / objectStats.myWeight;

                // if the object is too heavy to lift, push
                if (objectStats.myWeight > 2) 
                {
                    dir = new Vector3(dir.x, 0, dir.y);
                }

                Vector3 targetVelocity = dir * objectMovingStrenght * distanceFactor * weightModifier;
                movingObjectRB.linearVelocity = Vector3.Lerp(
                    movingObjectRB.linearVelocity,
                    targetVelocity,
                    10f * Time.fixedDeltaTime);

                

                movingObjectRB.angularVelocity = Vector3.Lerp(
                    movingObjectRB.angularVelocity,
                    Vector3.zero,
                    2f * Time.fixedDeltaTime);

            }
        }
    }

    private void LookforObjects()
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayerMask))
        {
            if (hit.collider.CompareTag("Interactable"))
            {
                crosshairImageObject.SetActive(true);
                isLookingAtInteractable = true;
                objectBeingLookedAt = hit.collider.gameObject;
                Debug.Log($"looking at object: {objectBeingLookedAt}");
            }
            else
            {
                if (!isMovingInteractable)
                {
                    crosshairImageObject.SetActive(false);
                }
                isLookingAtInteractable = false;
            }
        }
        else
        {
            if (!isMovingInteractable)
            {
                crosshairImageObject.SetActive(false);
                objectBeingLookedAt = null;
            }
            isLookingAtInteractable = false;
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (camera == null) return;

        Gizmos.color = Color.yellow;

        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * interactDistance);
    }
#endif

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


    private void PerformMovement()
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
    public void OnInteract(InputAction.CallbackContext context)
    {
        // if the interact input is registered, DEFAULT 'E' key

    }
    public void OnClick(InputAction.CallbackContext context)
    {
        onMouseClick();
        if(context.performed)
        {
            clickHeld = true;
        }

        if(context.canceled)
        {
            movingObjectRB = null;
            clickHeld = false;
        }
    }
    private void onMouseClick()
    {
        if(movingObjectRB == null)
        {
            if(objectBeingLookedAt != null)
            {
                movingObjectRB = objectBeingLookedAt.GetComponent<Rigidbody>();
                objectStats = objectBeingLookedAt.GetComponent<InteractableObjectStatistics>();
                objectMoverReferancePointObject.transform.position = objectBeingLookedAt.transform.position;
            }
        }
    }
}
