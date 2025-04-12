using UnityEngine;

public class HitboxVisualRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90.0f; // Degrees per second

    void Update()
    {
        // Rotate the GameObject this script is attached to around the Z axis
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }
} 