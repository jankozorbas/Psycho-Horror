using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviour
{
    [Tooltip("Enable/disable movement")]
    public bool CanMoveBody = true;

    [Tooltip("Enable/disable camera rotation")]
    public bool CanMoveCamera = true;

    [Tooltip("Enable/disable Moving objects")]
    public bool CanMoveObjects = true;

    [Tooltip("Enable/disable Mantling")]
    public bool CanMantle = true;
    

    // Camera movement
    [SerializeField] private Camera camera;
    [SerializeField] private float lookSensitivity = 1f;
    private Vector2 lookInput;
    private float camPitch = 0f, camYaw = 0f;
    private float minCamYaw = -80;
    
    // movement values
    [SerializeField] private float movSpeed = 5f;
    [SerializeField] private float gravityModifier = -5f;
    [SerializeField] private float maxFallingSpeed = 10f;
    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isFalling = false;

    // used for object movement
    [SerializeField] private float objectMovingStrenght = 2f;
    [SerializeField] private GameObject crosshairImageObject, objectMoverReferancePointObject, cameraPivotBaseObject;
    [SerializeField] private LayerMask interactableLayerMask;
    [SerializeField] private float interactDistance = 5f;
    private bool isLookingAtInteractable = false, isMovingInteractable = false, clickHeld = false;
    private GameObject objectBeingLookedAt;
    private Rigidbody movingObjectRB;
    private InteractableObjectStatistics objectStats;

    // Variables for Mantling
    [SerializeField] private float mantleSpeed = 2f;
    private float mantleCompletionValue;
    private bool isMantling = false, handsInPosition = false;
    private Vector3 startingPosWhenMantling;
    private GameObject mantleAreaObject;
    private MantleAreaController mantleAreaController;

    // to move the arms
    [SerializeField] private GameObject leftHandTarget, rightHandTarget, leftHandObject, rightHandObject;
    [SerializeField] private TwoBoneIKConstraint leftHandIK, rightHandIK;
    

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
        else { if (crosshairImageObject.activeInHierarchy) crosshairImageObject.SetActive(false); }

        if (CanMantle && !isFalling) { PerformMantleAction(); }
        

    }

    private void MoveObjectBasedOnInput()
    {
        LookforObjects();
        if (clickHeld)
        {
            if(movingObjectRB != null)
            {
                isMovingInteractable = true;
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
        camPitch = Mathf.Clamp(camPitch, minCamYaw, 80f);

        cameraPivotBaseObject.transform.localRotation = Quaternion.Euler(camPitch, -camYaw, 0f);
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



    #region Getting Inputs
    public void ToggleCamera(InputAction.CallbackContext context)
    {
        // if the UlockMouse input is registered, DEFAULT 'L' key
        if (Application.isEditor)
        {
            if (cursorLocked)
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
        // if the Look input is registered, DEFAULT 'Mouse Movement' 
        lookInput = context.ReadValue<Vector2>();
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        // if the Move input is registered, DEFAULT 'WASD' key(s)
        moveInput = context.ReadValue<Vector2>();
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        // if the interact input is registered, DEFAULT 'E' key

    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // if the Jump input is registered, DEFAULT 'SPACE' key
    }
    public void OnClick(InputAction.CallbackContext context)
    {
        // if the Attack input is registered, DEFAULT 'LMB' key
        onMouseClick();
        if(context.performed)
        {
            clickHeld = true;
        }

        if(context.canceled)
        {
            movingObjectRB = null;
            isMovingInteractable = false;
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
    #endregion

    private void PerformMantleAction()
    {
        if(mantleAreaObject == null) { return; }

        Vector3 dirToLedge = (mantleAreaController.LedgeObject.transform.position - transform.position).normalized;
        float dotValue = Vector3.Dot(cameraPivotBaseObject.transform.right, dirToLedge);

        leftHandTarget.transform.position = mantleAreaController.GrabLeftLocationObject.transform.position;
        leftHandTarget.transform.rotation = mantleAreaController.GrabLeftLocationObject.transform.rotation;

        rightHandTarget.transform.position = mantleAreaController.GrabRightLocationObject.transform.position;
        rightHandTarget.transform.rotation = mantleAreaController.GrabRightLocationObject.transform.rotation;

        if (!handsInPosition)
        {
            float distanceToWallFactor = Mathf.Clamp01(Vector3.Distance(transform.position, mantleAreaObject.transform.position));

            float right = Mathf.Clamp01((dotValue + 1f) * 0.5f);
            float left = Mathf.Clamp01((1f - dotValue) * 0.5f);

            float leftOut = Mathf.Clamp01((1f - right) * 2f * (1f - distanceToWallFactor * 1.3f));
            float rightOut = Mathf.Clamp01((1f - left) * 2f * (1f - distanceToWallFactor * 1.3f));

            rightHandIK.weight = rightOut * 2;
            leftHandIK.weight = leftOut * 2;

            Mathf.Clamp01(rightHandIK.weight);
            Mathf.Clamp01(leftHandIK.weight);


            if (Vector3.Distance(rightHandObject.transform.position, rightHandTarget.transform.position) < 0.3f
                &&
                Vector3.Distance(leftHandObject.transform.position, leftHandTarget.transform.position) < 0.3f)
            {
                handsInPosition = true;
                minCamYaw = -10f;
                CanMoveBody = false;
                CanMoveObjects = false;
                isMantling = true;
                mantleCompletionValue = 0.05f;
                startingPosWhenMantling = transform.position;
                Debug.Log($"starting climb");
            }
        }
        else
        {
            if ((dotValue > -0.9f && dotValue < 0.9f))
            {
                if (moveInput.y > 0.1)
                {
                    // Lift yourself up
                    mantleCompletionValue += Time.deltaTime * 0.1f * mantleSpeed * (1 + mantleCompletionValue * 0.5f);
                }
                if (moveInput.y < -0.1  )
                {
                    // Lower yourself down
                    mantleCompletionValue -= Time.deltaTime * 0.1f * mantleSpeed * (1 + mantleCompletionValue * 0.5f);
                }
                if (mantleCompletionValue >= 0.95f)
                {
                    handsInPosition = false;
                    minCamYaw = -80f;
                    CanMoveBody = true;
                    CanMoveObjects = true;
                    isMantling = false;
                    Debug.Log($"Mantle Complete!");
                }
                if(mantleCompletionValue < 0.05f)
                {
                    Vector3 dir = transform.position - mantleAreaObject.transform.position;
                    transform.position += dir;
                    Debug.Log($"down on floor, sending back");
                    isMantling = false;
                    handsInPosition = false;
                    minCamYaw = -80f;
                    CanMoveBody = true;
                    CanMoveObjects = true;
                }

                float t = mantleCompletionValue;

                if (t <= 0.7f)
                {
                    float segT = t / 0.7f;
                    transform.position = Vector3.Lerp(startingPosWhenMantling, 
                        mantleAreaController.ClimbPoint1.transform.position, 
                        segT);
                }
                else if (t <= 0.9f)
                {
                    float segT = (t - 0.7f) / (0.2f);
                    transform.position = Vector3.Lerp(mantleAreaController.ClimbPoint1.transform.position, 
                        mantleAreaController.ClimbPoint2.transform.position, 
                        segT);
                }
                else
                {
                    float segT = (t - 0.9f) / (0.2f);
                    transform.position = Vector3.Lerp(mantleAreaController.ClimbPoint2.transform.position,
                        mantleAreaController.ClimbPoint3.transform.position,
                        segT);
                }
            }
            else
            {
                // looking away from the wall, cancel the climb
                isMantling = false;
                handsInPosition = false;
                minCamYaw = -80f;
                CanMoveBody = true;
                CanMoveObjects = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Mantle"))
        {
            mantleAreaObject = other.gameObject;
            mantleAreaController = other.GetComponent<MantleAreaController>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Mantle"))
        {
            mantleAreaObject = null;
            mantleAreaController = null;
        }
    }

}
