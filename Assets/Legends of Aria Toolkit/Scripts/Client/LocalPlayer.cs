using UnityEngine;

public class LocalPlayer : MonoBehaviour
{
    [SerializeField] private PlayerCamera _playerCameraPrefab;
    [SerializeField] private CharacterController _characterController;
    private const float _moveSpeed = 5.0f;
    private const float _gravity = -9.8f;
    private Vector3 _lastMousePos = Vector3.zero;
    private float _mouseXDelta = 0f;
    private PlayerCamera _playerCamera;
    
    void Awake()
    {
        _playerCamera = Instantiate(_playerCameraPrefab);
        
    }

    void Start()
    {
        _playerCamera.SetPlayerTarget(this);
        _characterController.SimpleMove(Vector3.forward);
    }

    void Update()
    {
        ProcessInput();
    }

    void LateUpdate()
    {
        if (_mouseXDelta != 0f)
        {
            _playerCamera.RotateCamera(_mouseXDelta);
        }
    }

    private void ProcessInput()
    {
        Vector3 moveVector = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        Vector3 cameraMoveVector = _playerCamera.transform.TransformDirection(moveVector) * _moveSpeed;

        _mouseXDelta = 0f;
        cameraMoveVector.y = _gravity;
        _characterController.Move(cameraMoveVector * Time.deltaTime);
        
        if (Input.GetMouseButtonDown(1))
        {
            _lastMousePos = Input.mousePosition;
        } else if (Input.GetMouseButton(1))
        {
            Vector3 mouseDelta = (Input.mousePosition - _lastMousePos);
            _mouseXDelta = mouseDelta.x;
        }

        if (Input.mouseScrollDelta != Vector2.zero)
        {
            _playerCamera.ZoomCamera(Input.mouseScrollDelta.y);
        }
        
        _lastMousePos = Input.mousePosition;
    }
}