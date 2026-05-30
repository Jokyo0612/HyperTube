using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class StellarHyperTubeController : MonoBehaviour
{
    public enum State { Normal, Dodging, Looping, Jumping }
    [Header("Current Status")]
    public State CurrentState = State.Normal;

    [Header("Forward & Boost")]
    public float BaseMaxSpeed = 5.0f;
    public float BoostMaxSpeed = 13.0f;
    public float Acceleration = 0.5f;
    private float currentTargetSpeed;
    private float currentSpeed;

    [Header("Slide & Struggle")]
    public float SlideSpeed = 75f;
    public float CenterReturnSpeed = 25f;
    public float HeavyGravityStartAngle = 60f;
    public float HeavyGravityForce = 55f;

    [Header("Jump Physics")]
    public float JumpForce = 13f;
    public float Gravity = 45f;
    private float currentHeight = 0f;
    private float verticalVelocity = 0f;
    private bool isGrounded = true;
    private float initialModelY; //Save the half-radius offset

    [Header("Action")]
    public float DodgeAngle = 35f;
    public float DodgeDuration = 0.15f;
    public float MaxDodgeAngle = 80f;
    public float LoopTriggerAngle = 70f;
    public float LoopDuration = 0.8f;

    [Header("References")]
    public Transform Pivot;
    public Transform PlayerModel;
    public Transform MainCamera;
    public Transform CamTarget;

    //[Header("Visual Effects")]
    //public ParticleSystem LeftSpark;
    //public ParticleSystem RightSpark;

    private float currentRotation = 0f;
    private Vector2 moveInput;

    void Start()
    {
        currentTargetSpeed = BaseMaxSpeed;
        currentSpeed = BaseMaxSpeed * 0.2f;
        initialModelY = PlayerModel.localPosition.y; //Save half-radius offset for jump calculations
    }

    void Update()
    {
        // Forward Movement
        currentSpeed = Mathf.MoveTowards(currentSpeed, currentTargetSpeed, Acceleration * Time.deltaTime);
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.World);

        if (currentTargetSpeed > BaseMaxSpeed)
        {
            currentTargetSpeed = Mathf.MoveTowards(currentTargetSpeed, BaseMaxSpeed, (Acceleration * 0.5f) * Time.deltaTime);
        }

        if (CurrentState == State.Jumping)
        {
            StopAllCoroutines();
            HandleJumpPhysics();
        }

        if (CurrentState == State.Normal)
        {
            HandleNormalSlide();
            // HandleSparks(); 스파크 이펙트 제어
        }

        // Looping Trigger
        if (CurrentState != State.Looping && isGrounded && Mathf.Abs(currentRotation) >= LoopTriggerAngle)
        {
            StopAllCoroutines();
            StartCoroutine(LoopRoutine());
        }

        Pivot.localRotation = Quaternion.Euler(0, 0, currentRotation);

        // Pivot의 자식인 PlayerModel을 조작하여 점프 시 원통 중앙을 향해 떠오르게 만듭니다.
        PlayerModel.localPosition = new Vector3(
            PlayerModel.localPosition.x,
            initialModelY + currentHeight,
            PlayerModel.localPosition.z
        );
    }

    private void HandleJumpPhysics()
    {
        if (!isGrounded)
        {
            verticalVelocity -= Gravity * Time.deltaTime;
            currentHeight += verticalVelocity * Time.deltaTime;
            currentRotation = Mathf.MoveTowards(currentRotation, 0f, CenterReturnSpeed * Time.deltaTime);

            if (currentHeight <= 0f)
            {
                currentHeight = 0f;
                verticalVelocity = 0f;
                isGrounded = true;
                CurrentState = State.Normal;

                // TODO: Camera shake for landing
            }
        }
    }

    private void HandleNormalSlide()
    {
        float absRot = Mathf.Abs(currentRotation);

        if (moveInput.x != 0)
        {
            currentRotation += moveInput.x * SlideSpeed * Time.deltaTime;
        }
        else
        {
            currentRotation = Mathf.MoveTowards(currentRotation, 0f, CenterReturnSpeed * Time.deltaTime);
        }

        if (absRot >= HeavyGravityStartAngle)
        {
            float pullDirection = -Mathf.Sign(currentRotation);
            currentRotation += pullDirection * HeavyGravityForce * Time.deltaTime;
        }

        currentRotation = Mathf.Clamp(currentRotation, -MaxDodgeAngle, MaxDodgeAngle);
    }

    //private void HandleSparks()
    //{
    //    // 급격한 방향 전환이나, 튜브의 50도 이상 가파른 벽을 탈 때 칼날이 긁히는 조건
    //    bool isSteeringHardLeft = moveInput.x < 0 && currentRotation > 20f;
    //    bool isSteeringHardRight = moveInput.x > 0 && currentRotation < -20f;

    //    if (LeftSpark != null)
    //    {
    //        var em = LeftSpark.emission;
    //        em.enabled = isSteeringHardLeft || (currentRotation < -50f && isGrounded);
    //    }
    //    if (RightSpark != null)
    //    {
    //        var em = RightSpark.emission;
    //        em.enabled = isSteeringHardRight || (currentRotation > 50f && isGrounded);
    //    }
    //}

    /* Input System Callbacks */

    void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); }

    void OnJump()
    {
        if (isGrounded && CurrentState != State.Looping)
        {
            CurrentState = State.Jumping;
            isGrounded = false;
            verticalVelocity = JumpForce;
        }
    }

    void OnDodge()
    {
        if (CurrentState == State.Normal && moveInput.x != 0 && isGrounded)
        {
            StartCoroutine(DodgeRoutine(Mathf.Sign(moveInput.x)));
        }
    }

    // Boost Speed Trigger

    public void ApplySpeedBoost()
    {
        currentTargetSpeed = BoostMaxSpeed;
    }


    /* Rouitine for Dodge & Looping */

    private IEnumerator DodgeRoutine(float direction)
    {
        CurrentState = State.Dodging;
        float startAngle = currentRotation;
        float targetAngle = Mathf.Clamp(startAngle + (DodgeAngle * direction), -MaxDodgeAngle, MaxDodgeAngle);

        float time = 0f;
        while (time < DodgeDuration)
        {
            time += Time.deltaTime;
            float t = time / DodgeDuration;
            t = t * t * (3f - 2f * t);
            currentRotation = Mathf.Lerp(startAngle, targetAngle, t);
            yield return null;
        }
        currentRotation = targetAngle;
        if (CurrentState == State.Dodging) CurrentState = State.Normal;
    }

    private IEnumerator LoopRoutine()
    {
        CurrentState = State.Looping;
        float startAngle = currentRotation;
        float direction = Mathf.Sign(startAngle);

        float targetAngle = startAngle + (270f * direction);

        float time = 0f;
        while (time < LoopDuration)
        {
            time += Time.deltaTime;
            float t = time / LoopDuration;
            
            currentRotation = Mathf.Lerp(startAngle, targetAngle, t);
            yield return null;
        }

        currentRotation = targetAngle;
        if (currentRotation > 180f) currentRotation -= 360f;
        if (currentRotation < -180f) currentRotation += 360f;

        CurrentState = State.Normal;
    }

    void LateUpdate()
    {
        if (MainCamera != null && CamTarget != null)
        {
            MainCamera.position = CamTarget.position;
            MainCamera.rotation = transform.rotation;
        }
    }
}