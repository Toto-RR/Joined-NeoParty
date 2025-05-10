using UnityEngine;
using System.Collections;
using static PlayerChoices;

public class PlayerController : MonoBehaviour
{
    public PlayerChoices.PlayerColor playerColor;

    [Header("Movement Parameters")]
    public float moveSpeed = 0f;        // Velocidad de avance
    public float jumpForce = 5f;        // Fuerza del salto

    public float lateralSpeed = 3f;     // Velocidad lateral separada

    public float crouchHeight = 1f;
    public float crouchCenterY = 0.5f;

    [Header("Hit Parameters")]
    public Color hitColor = Color.red;  // Color al chocar
    public int points = 10;

    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider capsule;

    [Header("State Parameters")]
    public bool isGrounded = true;
    public bool finished = false;
    public bool isCrouching = false;
    public bool startToRun = false;

    private float originalHeight;
    private Vector3 originalCenter;
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

        StartCoroutine(StartToRun());
    }

    private void Update()
    {
        if (finished)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, 0f, Time.deltaTime * 4f);
        }
        else
        {
            HandleInput();
            MoveForward();
            UpdateColliderBasedOnAnimation();
        }

        animator.SetFloat("Speed", moveSpeed);
    }

    public IEnumerator StartToRun()
    {
        yield return new WaitForSeconds(3f);
        moveSpeed = 10f;
    }

    private void MoveForward()
    {
        Vector3 move = Vector3.forward * moveSpeed;

        // Movimiento lateral personalizado por color
        if (IsKeyHeld(GetLeftKey()) && isGrounded && !isCrouching)
        {
            move += Vector3.left * lateralSpeed;
            animator.SetBool("Left", true);
        }
        else if (IsKeyReleased(GetLeftKey()))
        {
            animator.SetBool("Left", false);
        }

        if (IsKeyHeld(GetRightKey()) && isGrounded && !isCrouching)
        {
            move += Vector3.right * lateralSpeed;
            animator.SetBool("Right", true);
        }
        else if (IsKeyReleased(GetRightKey()))
        {
            animator.SetBool("Right", false);
        }

        rb.MovePosition(transform.position + move * Time.deltaTime);
    }

    private void HandleInput()
    {
        bool isRunning = IsRunning();

        // Salto
        if (IsKeyPressed(GetJumpKey()) && isGrounded && !isCrouching && isRunning)
        {
            Jump();
        }

        // Agacharse
        if (IsKeyPressed(GetCrouchKey()) && isGrounded && !isCrouching && isRunning)
        {
            StartCrouch();
        }
        else if (IsKeyReleased(GetCrouchKey()) && isGrounded && isCrouching)
        {
            StopCrouch();
        }
    }

    // Helpers para keys
    private KeyCode GetLeftKey()
    {
        return playerColor switch
        {
            PlayerChoices.PlayerColor.Blue => KeyCode.A,
            PlayerChoices.PlayerColor.Orange => KeyCode.LeftArrow,
            PlayerChoices.PlayerColor.Yellow => KeyCode.F,
            PlayerChoices.PlayerColor.Green => KeyCode.J,
            _ => KeyCode.None
        };
    }

    private KeyCode GetRightKey()
    {
        return playerColor switch
        {
            PlayerChoices.PlayerColor.Blue => KeyCode.D,
            PlayerChoices.PlayerColor.Orange => KeyCode.RightArrow,
            PlayerChoices.PlayerColor.Yellow => KeyCode.H,
            PlayerChoices.PlayerColor.Green => KeyCode.L,
            _ => KeyCode.None
        };
    }

    private KeyCode GetJumpKey()
    {
        return playerColor switch
        {
            PlayerChoices.PlayerColor.Blue => KeyCode.W,
            PlayerChoices.PlayerColor.Orange => KeyCode.UpArrow,
            PlayerChoices.PlayerColor.Yellow => KeyCode.T,
            PlayerChoices.PlayerColor.Green => KeyCode.I,
            _ => KeyCode.None
        };
    }

    private KeyCode GetCrouchKey()
    {
        return playerColor switch
        {
            PlayerChoices.PlayerColor.Blue => KeyCode.S,
            PlayerChoices.PlayerColor.Orange => KeyCode.DownArrow,
            PlayerChoices.PlayerColor.Yellow => KeyCode.G,
            PlayerChoices.PlayerColor.Green => KeyCode.K,
            _ => KeyCode.None
        };
    }

    private bool IsKeyPressed(KeyCode key) => Input.GetKeyDown(key);
    private bool IsKeyReleased(KeyCode key) => Input.GetKeyUp(key);
    private bool IsKeyHeld(KeyCode key) => Input.GetKey(key);

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
            points--;
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
        Debug.Log("Puntuación final: " + points);

        if (Minigame_1.Instance != null)
        {
            Minigame_1.Instance.PlayerFinished(playerColor, points);
        }
    }

    public int GetPoints()
    {
        return points;
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