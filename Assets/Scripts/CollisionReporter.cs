using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    private Renderer objectRenderer;

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(
            "CalibrationSample collided with: " +
            collision.gameObject.name
        );

        if (objectRenderer != null)
        {
            objectRenderer.material.color = Color.red;
        }
    }
}
