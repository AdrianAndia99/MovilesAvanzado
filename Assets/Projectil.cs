using Unity.Netcode;
using UnityEngine;

public class Projectil : NetworkBehaviour
{
    public int damage = 34;
    private ulong shooterClientId; // ID del jugador que disparó

    void Start()
    {
        if (IsServer)
        {
            Invoke("SimpleDespawn", 5f);
        }
    }

    // Método para establecer quién disparó el proyectil
    public void SetShooter(ulong clientId)
    {
        shooterClientId = clientId;
    }

    public void SimpleDespawn()
    {
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            // Obtener el NetworkObject del jugador para verificar si es el mismo que disparó
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            if (playerNetObj != null && playerNetObj.OwnerClientId != shooterClientId)
            {
                // Es un jugador diferente al que disparó, aplicar daño
                SimplePlayerController player = other.GetComponent<SimplePlayerController>();
                if (player != null)
                {
                    player.TakeDamageServerRpc(damage);
                }
                SimpleDespawn();
            }
            // Si es el mismo jugador que disparó, no hacer nada (no auto-daño)
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            SimpleDespawn();
        }

        // Se destruye al chocar con cualquier otra cosa
        SimpleDespawn();
    }
}