using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;        // Velocidad de avance
    public float jumpForce = 5f;        // Fuerza del salto

    public float lateralSpeed = 3f;     // Velocidad lateral separada
    public Color hitColor = Color.red;  // Color al chocar

    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider capsule;

    public bool isGrounded = true;
    public bool finished = false;
    public bool isCrouching = false;

    private float originalHeight;
    private Vector3 originalCenter;
    public float crouchHeight = 1f;
    public float crouchCenterY = 0.5f;

    private Renderer playerRenderer;
    private Color originalColor;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider>();

        playerRenderer = GetComponentInChildren<Renderer>();
        originalColor = playerRenderer.material.color;

        originalHeight = capsule.height;
        originalCenter = capsule.center;
    }

    private void Update()
    {
        if (finished)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, 0f, Time.deltaTime * 2f);
        }
        else
        {
            HandleInput();
            MoveForward();
            UpdateColliderBasedOnAnimation();
        }

        animator.SetFloat("Speed", moveSpeed);
    }

    private void MoveForward()
    {
        Vector3 move = Vector3.forward * moveSpeed;

        if (Input.GetKey(KeyCode.A) && isGrounded && !isCrouching)
        {
            move += Vector3.left * lateralSpeed;
            animator.SetBool("Left", true);
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            animator.SetBool("Left", false);
        }

        if (Input.GetKey(KeyCode.D) && isGrounded && !isCrouching)
        {
            move += Vector3.right * lateralSpeed;
            animator.SetBool("Right", true);
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            animator.SetBool("Right", false);
        }

        rb.MovePosition(transform.position + move * Time.deltaTime);
    }

    private void HandleInput()
    {
        bool isRunning = IsRunning();

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouching && isRunning)
        {
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.S) && isGrounded && !isCrouching && isRunning)
        {
            StartCrouch();
        }
        else if (Input.GetKeyUp(KeyCode.S) && isGrounded && isCrouching)
        {
            StopCrouch();
        }
    }

    private void Jump()
    {
        if (isCrouching) return;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        animator.SetTrigger("Jump");
        isGrounded = false;
    }

    private void StartCrouch()
    {
        if (isCrouching) return;

        animator.SetTrigger("Crouch");
    }

    private void StopCrouch()
    {
        if (!isCrouching) return;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            Debug.Log("¡Colisión detectada con obstáculo!");
            playerRenderer.material.color = hitColor;
            Invoke(nameof(ResetColor), 0.5f);
        }
    }

    private bool IsRunning()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("HumanoidRun");
    }

    private void ResetColor()
    {
        playerRenderer.material.color = originalColor;
    }

    public void FinishReached()
    {
        finished = true;
    }

    private void UpdateColliderBasedOnAnimation()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Crouch") || stateInfo.IsName("HumanoidCrouchWalk"))
        {
            isCrouching = true;
            capsule.height = crouchHeight;
            capsule.center = new Vector3(capsule.center.x, crouchCenterY, capsule.center.z);
        }
        else
        {
            capsule.height = originalHeight;
            capsule.center = originalCenter;
            isCrouching = false;
        }
    }

}