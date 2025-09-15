using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.InputSystem;

public class SimplePlayerController : NetworkBehaviour
{
    public NetworkVariable<ulong> PlayerID;
    public ulong PlayerID2;

    public NetworkVariable<FixedString32Bytes> accountID = new();
    public NetworkVariable<int> health = new();
    public NetworkVariable<int> attack = new();

    public GameObject projectilPrefab;
    public float speed;
    private Animator animator;
    private Rigidbody rb;
    public LayerMask groundLayer;
    public float jumpForce = 5f;

    public Transform firepoint;
    public float aimLength = 10f;
    private LineRenderer lineRenderer;

    public float turnSpeed = 15f;
    private Camera mainCamera;

    // Variables para el sistema de vida
    public int maxHealth = 100;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
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

        if (Input.GetMouseButtonDown(0))
        {
            shootRpc(firepoint.forward);
        }

        if (firepoint != null)
        {
            lineRenderer.SetPosition(0, firepoint.position);
            lineRenderer.SetPosition(1, firepoint.position + firepoint.forward * aimLength);
        }
    }

    public void SetData(PlayerData playerData)
    {
        accountID.Value = playerData.accountID;
        health.Value = playerData.health;
        attack.Value = playerData.attack;
        transform.position = playerData.position;
    }

    // Método para recibir daño
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (!IsServer) return;

        health.Value -= damage;
        Debug.Log($"Jugador {accountID.Value} recibió {damage} de daño. Vida restante: {health.Value}");

        if (health.Value <= 0)
        {
            // El jugador ha muerto, respawnearlo
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        if (!IsServer) return;

        // Restaurar vida
        health.Value = maxHealth;
        
        // Mover a una posición de respawn aleatoria
        Vector3 respawnPos = gameManager2.Instance.GetRandomSpawnPosition();
        transform.position = respawnPos;

        Debug.Log($"Jugador {accountID.Value} ha sido respawneado en {respawnPos}");
    }

    public override void OnNetworkDespawn()
    {
        if (gameManager2.Instance != null && !string.IsNullOrEmpty(accountID.Value.ToString()))
        {
            gameManager2.Instance.playerStatesByAccountID[accountID.Value.ToString()] = new PlayerData(
                accountID.Value.ToString(), 
                transform.position, 
                health.Value, 
                attack.Value
            );

            Debug.Log($"me desconecte {NetworkManager.Singleton.LocalClientId} y se guardo la data de {accountID.Value}");
        }
    }

    private void AimTowardsMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance: 300f, layerMask: groundLayer))
        {
            var target = hitInfo.point;
            target.y = transform.position.y;
            
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

        // Establecer quién disparó el proyectil para evitar auto-daño
        Projectil projectilScript = proj.GetComponent<Projectil>();
        if (projectilScript != null)
        {
            projectilScript.SetShooter(OwnerClientId);
        }

        proj.GetComponent<Rigidbody>().AddForce(direction * 5, ForceMode.Impulse);
    }
}