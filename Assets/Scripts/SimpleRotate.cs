using UnityEngine;

// Simple script to rotate the GameObject it's attached to around the Z axis.
public class SimpleRotate : MonoBehaviour
{
    [Tooltip("Degrees per second")]
    [SerializeField] private float rotationSpeed = 60f;
    
    // Update is called once per frame
    void Update()
    {
        // Rotate around the Z axis (suitable for 2D)
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
