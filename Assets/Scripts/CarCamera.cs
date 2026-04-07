using UnityEngine;

public class CarCamera : MonoBehaviour
{
    public Transform target;          // The car body transform
    public Vector3 offset = new Vector3(0, 3f, -7f);
    public float followSpeed = 8f;
    public float rotateSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Smoothly follow position
        Vector3 desiredPos = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

        // Smoothly look at car
        Quaternion desiredRot = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotateSpeed * Time.deltaTime);
    }
}