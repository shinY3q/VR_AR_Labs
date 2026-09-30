using UnityEngine;

public class CalibrationZone : MonoBehaviour
{
    private Renderer zoneRenderer;

    private void Awake()
    {
        zoneRenderer = GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Debug.Log("Player entered the calibration zone.");

        if (zoneRenderer != null)
        {
            zoneRenderer.material.color = Color.green;
        }
    }
}
