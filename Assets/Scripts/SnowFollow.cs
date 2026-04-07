using UnityEngine;

public class SnowFollow : MonoBehaviour
{
[SerializeField] private Transform player;
[SerializeField] private Vector3 offset = new Vector3(0f, 2f, 2.5f); // Forward offset

private void LateUpdate()
{
    if (player == null) return;
    transform.position = player.position + player.forward * offset.z + player.right * offset.x + player.up * offset.y;
}
}
