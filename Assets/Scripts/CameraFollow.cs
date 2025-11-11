using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;            // The player's transform to follow.
    public float smoothSpeed = 0.125f;  // Smoothing factor for camera movement.
    public Vector3 offset;              // Offset between the camera and the player.

    public Vector2 minBounds;           // Minimum camera position bounds.
    public Vector2 maxBounds;           // Maximum camera position bounds.

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Calculate the desired camera position based on the player's position.
        Vector3 desiredPosition = target.position + offset;

        // Apply boundary restrictions.
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);

        // Smoothly move the camera towards the desired position.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Set the camera's position to the smoothed position.
        transform.position = smoothedPosition;
    }
}