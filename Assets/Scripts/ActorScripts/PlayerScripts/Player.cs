﻿using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Actor
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _revolverLegPlacement;
    [SerializeField] private Transform _revolverHandPlacement;
    [SerializeField] private Transform _leftHand;
    [SerializeField] private Transform _leftFoot;
    [SerializeField] private Transform _rightFoot;
    [SerializeField] private Transform _rightArm;
    [SerializeField] private Transform _armStabilizerPoint;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private GameObject _camera;
    [SerializeField] private Revolver _revolver;
    [SerializeField] private LayerMask _groundMask;
    private Vector2 _movementInput;
    private Vector3 _velocity;
    private float _xAxis;
    private float _yAxis;
    private readonly int _gravity = -20;
    private readonly float _jumpHeight = 1.0f;
    private float _walkSpeed = 2f;
    private readonly float _aimSpeed = 2f;
    private readonly float _crouchSpeed = 1.7f;
    private readonly float _proneSpeed = 1.0f;
    private readonly float _runSpeed = 5f;
    private readonly float _noSlipDistance = 0.05f;
    private float _moveSpeed = 2;
    private bool _isHoldRunning;
    private bool _isProne;
    private bool _isAiming;
    private bool _isCrouching;
    private bool _isReloading;
    private bool _isCurrentArmRight = true;
    private bool _isArmStabilized;
    private bool _isJoggingOn;
    private bool _hasRaisedLeftFoot = true;
    private bool _hasRaisedRightFoot = true;

    private Vector3 _lastPosition;

    public bool HasPickable { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool CanShoot { get; set; } = true;
    public bool LockMovement { get; set; }
    public bool IsRunning { get; private set; }
    public bool IsHolster { get; private set; } = true;
    public Vector3 Move { get; private set; }
    public Vector2 MovementInput { get; set; }


    void Start()
    {
        SetHealth();
    }

    public override void SetHealth()
    {
        Health.MaxHealth = 100;
        Health.MinimumMassMultiplier = 3;
    }

    void Update()
    {
        Movement();
        CheckGrounded();
        Gravity();
        Footsteps();
    }

    void LateUpdate()
    {
        EdgeFalling();
    }

    public void Holster()
    {
        IsHolster = !IsHolster;
        _animator.SetBool("IsHolstering", IsHolster);
        if (IsHolster)
        {
            _revolver.transform.SetParent(_revolverLegPlacement);
            _revolver.transform.position = _revolverLegPlacement.position;
            _revolver.transform.rotation = _revolverLegPlacement.rotation;
        }
        else
        {
            _revolver.transform.SetParent(_revolverHandPlacement);
            _revolver.transform.position = _revolverHandPlacement.position;
            _revolver.transform.rotation = _revolverHandPlacement.rotation;
        }
        _isArmStabilized = !_isArmStabilized;
    }

    private void Movement()
    {
        if (!_isReloading || _isHoldRunning)
        {
            if (!IsRunning)
            {
                _xAxis = MovementInput.x;
            }
            else
            {
                _xAxis = 0.0f;
            }
            _yAxis = MovementInput.y;

            Move = transform.right * _xAxis + transform.forward * _yAxis;
            _characterController.Move(Move * _moveSpeed * Time.deltaTime);

            float dist = Vector3.Distance(_lastPosition, transform.position);
            if (dist > 0.01f)
            {
                _animator.SetFloat("VelocityX", _xAxis, 0.1f, Time.deltaTime * 1.0f);
                _animator.SetFloat("VelocityY", _yAxis, 0.1f, Time.deltaTime * 1.0f);
            }
            else
            {
                _animator.SetFloat("VelocityX", 0.0f, 0.1f, Time.deltaTime * 1.0f);
                _animator.SetFloat("VelocityY", 0.0f, 0.1f, Time.deltaTime * 1.0f);
            }
            _lastPosition = gameObject.transform.position;
        }
    }

    private void CheckGrounded()
    {
        Vector3 checkGroundPosition = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z - 0.1f);
        IsGrounded = Physics.Raycast(checkGroundPosition, Vector3.down, 1.0f, _groundMask );
        if (IsGrounded && _velocity.y < 0)
        {
            _velocity.y = -2.0f;
            _animator.SetBool("IsFalling", false);
        }
        else
        {
            _animator.SetBool("IsFalling", true);
        }
    }

    private void Gravity()
    {
        _velocity.y += _gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void EdgeFalling()
    {
        if (!IsGrounded)
        {
            if (Physics.SphereCast(transform.position + _characterController.center, _characterController.radius - _characterController.skinWidth, Vector3.down, out RaycastHit hitInfo, _characterController.height))
            {
                Vector3 relativeHitPoint = hitInfo.point - (transform.position + _characterController.center);
                relativeHitPoint.y = 0;
                if (relativeHitPoint.magnitude > _noSlipDistance)
                {
                    Vector3 edgeFallMovement = transform.position - hitInfo.point;
                    edgeFallMovement.y = 0;
                    _velocity += (edgeFallMovement * Time.deltaTime * 20);
                }
                else
                {
                    _velocity.z = 0.0f;
                    _velocity.x = 0.0f;
                }
            }
        }
    }

    public void Jump()
    {
        if (IsGrounded)
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -3.0f * _gravity);
            _animator.SetTrigger("Jump");
        }
    }

    public void Crouch()
    {
        if (!_isProne)
        {
            if (_isCrouching)
            {
                bool cantStandUp = Physics.Raycast(transform.position, Vector3.up, 3.0f, _groundMask);
                if (!cantStandUp)
                {
                    SetCrouch(false, _walkSpeed);
                }
            }
            else
            {
                SetCrouch(true, _crouchSpeed);
            }
        }
        else
        {
            bool cantStandUp = Physics.Raycast(transform.position, Vector3.up, 1.5f, _groundMask);
            if (!cantStandUp)
            {
                _characterController.height = 1.0f;
                _characterController.center = new Vector3(0.0f, _characterController.center.y, 0.0f);
                _moveSpeed = _walkSpeed;
                _isProne = false;
                _animator.SetBool("IsProne", _isProne);
            }
        }
    }

    public void SetCrouch(bool isCrouching, float moveSpeed, InputAction.CallbackContext context = default)
    {
        if (isCrouching)
        {
            _characterController.height = 1.0f;
            _moveSpeed = moveSpeed;
        }
        else
        {
            _characterController.height = 2.0f;
            _moveSpeed = moveSpeed;
        }
        _isCrouching = isCrouching;
        _animator.SetBool("IsCrouching", _isCrouching);
    }

    public void Prone()
    {
        if (_isProne)
        {
            bool cantStandUp = Physics.Raycast(transform.position, Vector3.up, 1.5f, _groundMask);
            if (!cantStandUp)
            {
                _characterController.height = 1.0f;
                _characterController.center = new Vector3(0.0f, _characterController.center.y, 0.0f);
                _moveSpeed = _walkSpeed;
                _isProne = false;
                _animator.SetBool("IsProne", _isProne);
            }
        }
        else
        {
            _characterController.height = 1.0f;
            _characterController.radius = 0.5f;
            _characterController.center = new Vector3(0.0f, _characterController.center.y, 0.4f);
            _moveSpeed = _proneSpeed;
            _isProne = true;
            _animator.SetBool("IsProne", _isProne);
        }
    }

    public void Slide()
    {
        if (IsRunning)
        {
            _characterController.height = 1.0f;
            _characterController.radius = 0.5f;
            _animator.SetTrigger("Slide");
        }
    }

    public void Run()
    {
        if (_isCrouching)
        {
            bool cantStandUp = Physics.Raycast(transform.position, Vector3.up, 3.0f, _groundMask);
            if (!cantStandUp)
            {
                SetCrouch(false, _runSpeed);
            }
        }
        if (!_isProne)
        {
            _xAxis = 0.0f;
            _isHoldRunning = false;
            if (!_isCrouching)
            {
                _moveSpeed = _runSpeed;
            }
            _animator.SetBool("IsRunning", true);
            IsRunning = true;
        }
    }

    public void StopRun()
    {
        if (IsRunning)
        {
            IsRunning = false;
            _isHoldRunning = false;
            _isJoggingOn = true;
            _walkSpeed = 3.5f;
            _moveSpeed = _walkSpeed;
            _animator.SetBool("IsRunning", false);
            _animator.SetBool("IsJoggingOn", _isJoggingOn);
        }
    }

    public void PauseRun()
    {
        if (IsRunning)
        {
            Debug.Log(IsRunning);
            _isHoldRunning = true;
            _moveSpeed = 0.0f;
            _animator.SetBool("IsRunning", false);
            _animator.SetFloat("VelocityX", 0.0f);
            _animator.SetFloat("VelocityY", 0.0f);
        }
    }

    public void Shoot()
    {
        if (_isCurrentArmRight && !IsHolster)
        {
            if (!_isReloading)
            {
                _animator.SetBool("IsChamberEmpty", _revolver.IsChamberEmpty());
                if (CanShoot)
                {
                    //CanShoot = false;
                    _animator.SetTrigger("Shoot");
                    _revolver.Shoot();
                }
            }
        }
        else if (!_isCurrentArmRight)
        {
            if (CanShoot)
            {
                _isCurrentArmRight = true;
                GameObject pickableObject = _leftHand.GetChild(0).gameObject;
                pickableObject.GetComponent<Rigidbody>().isKinematic = false;
                pickableObject.transform.SetParent(null);
                pickableObject.GetComponent<Rigidbody>().AddForce(_camera.transform.forward * 500.0f);
            }
        }
    }

    private void Aim()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _moveSpeed = _aimSpeed;
            _isAiming = true;
            _animator.SetBool("IsAiming", true);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            _moveSpeed = _walkSpeed;
            _isAiming = false;
            _animator.SetBool("IsAiming", false);
        }
        if (_isAiming && Input.GetKeyDown(KeyCode.Q))
        {
            _animator.SetBool("IsAimingLeftSideways", true);
        }
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            _moveSpeed = _walkSpeed;
            _animator.SetBool("IsAimingLeftSideways", false);
        }
        if (_isAiming && Input.GetKeyDown(KeyCode.E))
        {
            _animator.SetBool("IsAimingRightSideways", true);
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            _moveSpeed = _walkSpeed;
            _animator.SetBool("IsAimingRightSideways", false);
        }
    }

    private void Reload()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (_isReloading)
            {
                _isReloading = false;
                _animator.SetBool("IsReloading", _isReloading);
            }
            else
            {
                _isReloading = true;
                _animator.SetBool("IsReloading", _isReloading);
            }
        }
    }

    private void SwitchArm()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (_isCurrentArmRight)
            {
                _isCurrentArmRight = false;
                if (_leftHand.childCount > 0)
                {
                    _leftHand.gameObject.SetActive(true);
                }
            }
            else if (!_isCurrentArmRight)
            {
                _isCurrentArmRight = true;
                _leftHand.gameObject.SetActive(false);
            }
        }
    }

    private void Footsteps()
    {
        Physics.Raycast(_leftFoot.transform.position, Vector3.down, out RaycastHit hitLeft, 0.21f);
        if (hitLeft.collider != null)
        {
            if (hitLeft.collider.gameObject.layer == LayerMask.NameToLayer("Environment"))
            {
                if (_hasRaisedLeftFoot)
                {
                    _hasRaisedLeftFoot = false;
                    AudioManager.Instance.PlayRandomFromSoundGroup("PlayerConcreteFootsteps");
                }
            }
        }
        else
        {
            if (!_hasRaisedLeftFoot)
            {
                _hasRaisedLeftFoot = true;
            }
        }

        Physics.Raycast(_rightFoot.transform.position, Vector3.down, out RaycastHit hitRight, 0.21f);
        if (hitRight.collider != null)
        {
            if (hitRight.collider.gameObject.layer == LayerMask.NameToLayer("Environment"))
            {
                if (_hasRaisedRightFoot)
                {
                    _hasRaisedRightFoot = false;
                    AudioManager.Instance.PlayRandomFromSoundGroup("PlayerConcreteFootsteps");
                }
            }
        }
        else
        {
            if (!_hasRaisedRightFoot)
            {
                _hasRaisedRightFoot = true;
            }
        }
    }

    public void InsertRound(GameObject round)
    {
        //TO DO Animation
        _revolver.AddRound(round);
    }

    public void SwitchWalkMode()
    {
        if (_isJoggingOn)
        {
            _isJoggingOn = false;
            _walkSpeed = 2.0f;
        }
        else
        {
            _isJoggingOn = true;
            _walkSpeed = 3.5f;
        }
        _moveSpeed = _walkSpeed;
        _animator.SetBool("IsJoggingOn", _isJoggingOn);
    }

    private void StabilizeArm()
    {
        if (_isArmStabilized)
        {
            _rightArm.rotation = _armStabilizerPoint.rotation;
           _rightArm.position = _armStabilizerPoint.position;
        }
    }
}