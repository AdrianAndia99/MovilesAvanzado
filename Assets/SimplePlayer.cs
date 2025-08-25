using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class SimplePlayer : NetworkBehaviour
{
    public float speed;
    private void Start()
    {
        GetComponent<NetworkTransform>().IsServerAuthoritative();
    }
    void Update()
    {
        if (!IsOwner) return;
        float x = Input.GetAxisRaw("Horizontal") * speed * Time.deltaTime;
        float y = Input.GetAxisRaw("Vertical") * speed * Time.deltaTime;

        transform.position += new Vector3(x, 0, y);
    }
}