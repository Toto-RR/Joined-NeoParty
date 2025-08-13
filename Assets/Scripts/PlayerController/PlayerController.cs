using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public PlayerChoices.PlayerColor playerColor;

    [Header("Hit Parameters")]
    public Color hitColor = Color.red;
    public int points = 10;

    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider capsule;

    [Header("State Parameters")]
    public bool finished = false;

    [Header("Lane Parameters")]
    public Transform[] carriles;
    public int currentCarrilIndex = 0;
    private bool isChangingLane = false;
    public float laneChangeDuration = 0.5f;
    public float laneOffset = 1.25f;

    private float playerSpeed = 0f;
    private Renderer playerRenderer;
    private Color originalColor;
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            Debug.Log($"Jugador {playerColor} tiene {playerInput.actions.actionMaps.Count} action maps activos.");

            var actions = playerInput.actions;

            actions["MoveToLane1"].performed += ctx => MoverACarril(0);
            actions["MoveToLane2"].performed += ctx => MoverACarril(1);
            actions["MoveToLane3"].performed += ctx => MoverACarril(2);
            actions["MoveToLane4"].performed += ctx => MoverACarril(3);
        }
    }

    private void Start()
    {
        foreach (var p in PlayerChoices.GetActivePlayers())
        {
            Debug.Log($"{p.Color} tiene asignado el dispositivo: {p.Device.displayName}");
        }

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider>();

        playerRenderer = GetComponentInChildren<Renderer>();
        originalColor = playerRenderer.material.color;

        Debug.Log("Transform: " + transform.position + ", Carril Actual: " + currentCarrilIndex);
    }

    private void Update()
    {
        if (finished) return;

        //animator.SetFloat("Speed", 0f);
    }

    public void AsignarCarriles(Transform[] nuevosCarriles, int carrilActual)
    {
        carriles = nuevosCarriles;
        currentCarrilIndex = carrilActual;
        transform.position = carriles[currentCarrilIndex].position + new Vector3(-laneOffset, 0.5f, -20);
    }

    public void MoverACarril(int index)
    {
        animator.SetFloat("Speed", 1f);
        if (index >= 0 && index < carriles.Length && index != currentCarrilIndex && !isChangingLane)
        {
            StopAllCoroutines();
            StartCoroutine(MoverSuavemente(index));
        }
    }

    private IEnumerator MoverSuavemente(int nuevoIndex)
    {
        isChangingLane = true;
        Vector3 inicio = transform.position;
        Vector3 destino = new Vector3((carriles[nuevoIndex].position.x - laneOffset), inicio.y, inicio.z);
        float t = 0;

        if (nuevoIndex > currentCarrilIndex)
        {
            animator.SetBool("Right", true);
        }
        else
        {
            animator.SetBool("Left", true);
        }

        while (t < 1)
        {
            t += Time.deltaTime / laneChangeDuration;
            transform.position = Vector3.Lerp(inicio, destino, t);
            yield return null;
        }

        animator.SetBool("Right", false);
        animator.SetBool("Left", false);
        transform.position = destino;
        currentCarrilIndex = nuevoIndex;        
        isChangingLane = false;
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
        animator.SetFloat("Speed", Mathf.Lerp(playerSpeed, 0, 2f));
        //playerInput.actions.Disable();
    }

    public int GetPoints() => points;
}
