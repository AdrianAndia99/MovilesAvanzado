using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class SimplePlayerController : NetworkBehaviour
{
    public NetworkVariable<ulong> PlayerID;

    public ulong PlayerID2;

    public GameObject projectilPrefab;
    public float speed;
    private Animator animator;
    private Rigidbody rb;
    public LayerMask groundLayer;
    public float jumpForce = 5f;

    public Transform firepoint;
    public float aimLength = 10f;
    private LineRenderer lineRenderer;

    public float turnSpeed = 15f; // Velocidad de rotación hacia el mouse
    private Camera mainCamera;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main; // Guardar referencia a la cámara principal

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // Esta configuración es mejor hacerla en el editor, como se mencionó antes.
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    void Update()
    {
        if (!IsOwner)
        {
            if(lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
            }
            return;
        }

        // Lógica de apuntado con el mouse
        AimTowardsMouse();

        if (!lineRenderer.enabled)
        {
            lineRenderer.enabled = true;
        }

        float x = Input.GetAxisRaw("Horizontal") * speed * Time.deltaTime;
        float y = Input.GetAxisRaw("Vertical") * speed * Time.deltaTime;

        if (x != 0 || y != 0)
        {
            MovePlayerServerRpc(x, y);
        }
        CheckGroundRpc();

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            AnimatorSetTriggerRpc("Jump");
        }

        // Cambiado a botón izquierdo del mouse para disparar
        if (Input.GetMouseButtonDown(0)) // 0 es el botón izquierdo del mouse
        {
            shootRpc(firepoint.forward);
        }

        if (firepoint != null)
        {
            lineRenderer.SetPosition(0, firepoint.position);
            lineRenderer.SetPosition(1, firepoint.position + firepoint.forward * aimLength);
        }
    }

    private void AimTowardsMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance: 300f, layerMask: groundLayer))
        {
            var target = hitInfo.point;
            target.y = transform.position.y; // Mantiene al jugador derecho, sin inclinarse
            
            Vector3 direction = target - transform.position;
            Quaternion rotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, turnSpeed * Time.deltaTime);
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer);
    }

    [Rpc(SendTo.Server)]
    public void AnimatorSetTriggerRpc(string animationName)
    {
        if (rb != null)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        animator.SetTrigger(animationName);
    }

    [ServerRpc]
    private void MovePlayerServerRpc(float x, float y)
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + new Vector3(x, 0, y));
        }
    }

    [Rpc(SendTo.Server)]
    public void CheckGroundRpc()
    {
        if (IsGrounded())
        {
            animator.SetBool("Grounded", true);
            animator.SetBool("FreeFall", false);
        }
        else
        {
            animator.SetBool("Grounded", false);
            animator.SetBool("FreeFall", true);
        }
    }

    [Rpc(SendTo.Server)]
    public void shootRpc(Vector3 direction)
    {
        GameObject proj = Instantiate(projectilPrefab, firepoint.position, Quaternion.LookRotation(direction));
        proj.GetComponent<NetworkObject>().Spawn(true);

        proj.GetComponent<Rigidbody>().AddForce(direction * 5, ForceMode.Impulse);
    }
}