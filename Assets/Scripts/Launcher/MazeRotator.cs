using UnityEngine;

public class MazeRotator :  MonoBehaviour
{
    // Speed of rotation in degrees per second
    private float rotationSpeed;
    // Store the calculated center of gravity
    private Vector3 centerOfGravity;

    void Start()
    {
        // 6 rotations per minute = 6 * 360 degrees per minute
        // Convert to degrees per second
        rotationSpeed = (6 * 360f) / 60f;

        // Calculate the center of gravity considering all children
        centerOfGravity = CalculateCenterOfGravity();
    }

    void Update()
    {
        // Rotate around the calculated center of gravity along local Y-axis
        transform.RotateAround(centerOfGravity, transform.up, rotationSpeed * Time.deltaTime);
    }

    // Method to calculate center of gravity for the object and all its children
    Vector3 CalculateCenterOfGravity()
    {
        Vector3 totalCenter = Vector3.zero;
        int count = 0;

        // Get all renderers in the object and its children
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // Sum up all the centers and count them
        foreach (Renderer rend in renderers)
        {
            totalCenter += rend.bounds.center;
            count++;
        }

        // Calculate the average center
        if (count > 0)
            return totalCenter / count;
        else
            return transform.position;
    }
}
