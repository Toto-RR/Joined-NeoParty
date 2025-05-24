using UnityEngine;
using System.Collections;
using static PlayerChoices;

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

    private Renderer playerRenderer;
    private Color originalColor;

    private void Start()
    {
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

        if (!isChangingLane)
        {
            HandleInput();
        }

        animator.SetFloat("Speed", 1f); // simula cinta de correr
    }

    private void HandleInput()
    {
        if (playerColor == PlayerColor.Blue)
        {
            if (Input.GetKeyDown(KeyCode.A)) MoverACarril(0);
            else if (Input.GetKeyDown(KeyCode.W)) MoverACarril(1);
            else if (Input.GetKeyDown(KeyCode.S)) MoverACarril(2);
            else if (Input.GetKeyDown(KeyCode.D)) MoverACarril(3);
        }
        else if (playerColor == PlayerColor.Orange)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) MoverACarril(0);
            else if (Input.GetKeyDown(KeyCode.UpArrow)) MoverACarril(1);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) MoverACarril(2);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) MoverACarril(3);
        }
        else if (playerColor == PlayerColor.Green)
        {
            if (Input.GetKeyDown(KeyCode.J)) MoverACarril(0);
            else if (Input.GetKeyDown(KeyCode.I)) MoverACarril(1);
            else if (Input.GetKeyDown(KeyCode.K)) MoverACarril(2);
            else if (Input.GetKeyDown(KeyCode.L)) MoverACarril(3);
        }
        else if (playerColor == PlayerColor.Yellow)
        {
            if (Input.GetKeyDown(KeyCode.F)) MoverACarril(0);
            else if (Input.GetKeyDown(KeyCode.T)) MoverACarril(1);
            else if (Input.GetKeyDown(KeyCode.G)) MoverACarril(2);
            else if (Input.GetKeyDown(KeyCode.H)) MoverACarril(3);
        }
    }

    public void AsignarCarriles(Transform[] nuevosCarriles, int carrilActual)
    {
        carriles = nuevosCarriles;
        currentCarrilIndex = carrilActual;
        transform.position = carriles[currentCarrilIndex].position + new Vector3(-0.5f, 0.5f, -20);
    }

    public void MoverACarril(int index)
    {
        if (index >= 0 && index < carriles.Length && index != currentCarrilIndex)
        {
            StopAllCoroutines();
            StartCoroutine(MoverSuavemente(index));
        }
    }

    private IEnumerator MoverSuavemente(int nuevoIndex)
    {
        isChangingLane = true;
        Vector3 inicio = transform.position;
        Vector3 destino = new Vector3((carriles[nuevoIndex].position.x - 0.5f), inicio.y, inicio.z);
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / laneChangeDuration;
            transform.position = Vector3.Lerp(inicio, destino, t);
            yield return null;
        }

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
    }

    public int GetPoints() => points;
}
