using Unity.Netcode;
using UnityEngine;

public class Projectil : NetworkBehaviour
{
    public int damage = 34;

    void Start()
    {
        if (IsServer)
        {
            Invoke("SimpleDespawn", 5f);
        }
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

        // Evita que la bala se destruya al chocar con el jugador que la disparó
        if (other.CompareTag("Player"))
        {
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        // Se destruye al chocar con cualquier otra cosa (que no sea el jugador)
        SimpleDespawn();
    }
}