using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OrbitCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = Vector3.zero;
    [SerializeField] private bool autoFindTower = true;
    [SerializeField] private string towerName = "Tower";

    [Header("Orbit")]
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float pitch = 45f;
    [SerializeField] private float minPitch = 20f;
    [SerializeField] private float maxPitch = 75f;
    [SerializeField] private float orbitSpeed = 90f;

    [Header("Zoom")]
    [SerializeField] private float distance = 25f;
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 45f;
    [SerializeField] private float zoomSpeed = 18f;

    [Header("Smoothing")]
    [SerializeField] private float positionLerpSpeed = 12f;
    [SerializeField] private float rotationLerpSpeed = 12f;

    [Header("Input")]
    [SerializeField] private bool inputEnabled = true;

    private float desiredYaw;
    private float desiredPitch;
    private float desiredDistance;
    private bool hasInitialized;

    public Transform Target => target;

    private void Awake()
    {
        desiredYaw = yaw;
        desiredPitch = pitch;
        desiredDistance = distance;
    }

    private void Start()
    {
        EnsureTarget();
        InitializeFromCurrent();
    }

    private void Update()
    {
        if (!inputEnabled)
        {
            return;
        }

        float orbitInput = 0f;
        if (IsKeyPressed(KeyCode.A) || IsKeyPressed(KeyCode.LeftArrow))
        {
            orbitInput -= 1f;
        }
        if (IsKeyPressed(KeyCode.D) || IsKeyPressed(KeyCode.RightArrow))
        {
            orbitInput += 1f;
        }

        float zoomInput = 0f;
        if (IsKeyPressed(KeyCode.W) || IsKeyPressed(KeyCode.UpArrow))
        {
            zoomInput -= 1f;
        }
        if (IsKeyPressed(KeyCode.S) || IsKeyPressed(KeyCode.DownArrow))
        {
            zoomInput += 1f;
        }

        desiredYaw += orbitInput * orbitSpeed * Time.deltaTime;
        desiredDistance = Mathf.Clamp(desiredDistance + zoomInput * zoomSpeed * Time.deltaTime, minDistance, maxDistance);
        desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);
    }

    private void LateUpdate()
    {
        EnsureTarget();
        if (target == null)
        {
            return;
        }

        if (!hasInitialized)
        {
            InitializeFromCurrent();
        }

        Vector3 focus = target.position + targetOffset;
        Quaternion orbit = Quaternion.Euler(desiredPitch, desiredYaw, 0f);
        Vector3 desiredPosition = focus + orbit * (Vector3.back * desiredDistance);

        float positionT = 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionT);

        Quaternion desiredRotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        float rotationT = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
    }

    public void SetTarget(Transform newTarget, bool snapToTarget = true)
    {
        target = newTarget;
        hasInitialized = false;
        if (snapToTarget)
        {
            SnapToTarget();
        }
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        InitializeFromCurrent();
        Vector3 focus = target.position + targetOffset;
        Quaternion orbit = Quaternion.Euler(desiredPitch, desiredYaw, 0f);
        transform.position = focus + orbit * (Vector3.back * desiredDistance);
        transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
    }

    private void EnsureTarget()
    {
        if (target != null || !autoFindTower)
        {
            return;
        }

        TowerHealth tower = FindFirstObjectByType<TowerHealth>();
        if (tower != null)
        {
            target = tower.transform;
            return;
        }

        if (!string.IsNullOrEmpty(towerName))
        {
            GameObject towerObject = GameObject.Find(towerName);
            if (towerObject != null)
            {
                target = towerObject.transform;
            }
        }
    }

    private void InitializeFromCurrent()
    {
        if (target == null)
        {
            return;
        }

        Vector3 focus = target.position + targetOffset;
        Vector3 toCamera = transform.position - focus;
        if (toCamera.sqrMagnitude < 0.001f)
        {
            desiredDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            desiredYaw = yaw;
            desiredPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            hasInitialized = true;
            return;
        }

        desiredDistance = Mathf.Clamp(toCamera.magnitude, minDistance, maxDistance);

        Vector3 planar = new Vector3(toCamera.x, 0f, toCamera.z);
        if (planar.sqrMagnitude > 0.001f)
        {
            desiredYaw = Mathf.Atan2(planar.x, planar.z) * Mathf.Rad2Deg;
        }

        desiredPitch = Mathf.Asin(Mathf.Clamp(toCamera.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);
        hasInitialized = true;
    }

    private bool IsKeyPressed(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        switch (key)
        {
            case KeyCode.A:
                return Keyboard.current.aKey.isPressed;
            case KeyCode.D:
                return Keyboard.current.dKey.isPressed;
            case KeyCode.W:
                return Keyboard.current.wKey.isPressed;
            case KeyCode.S:
                return Keyboard.current.sKey.isPressed;
            case KeyCode.LeftArrow:
                return Keyboard.current.leftArrowKey.isPressed;
            case KeyCode.RightArrow:
                return Keyboard.current.rightArrowKey.isPressed;
            case KeyCode.UpArrow:
                return Keyboard.current.upArrowKey.isPressed;
            case KeyCode.DownArrow:
                return Keyboard.current.downArrowKey.isPressed;
            default:
                return false;
        }
#else
        return Input.GetKey(key);
#endif
    }
}
