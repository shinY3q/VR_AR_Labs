using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SnapZoneIndicator : MonoBehaviour
{
    [SerializeField] private XRSocketInteractor socketInteractor;
    [SerializeField] private Renderer statusRenderer;
    [SerializeField] private Color idleColor = new(0.15f, 0.65f, 1f, 1f);
    [SerializeField] private Color activeColor = new(0.2f, 1f, 0.35f, 1f);

    private void Awake()
    {
        statusRenderer.material.color = idleColor;
    }

    private void OnEnable()
    {
        socketInteractor.selectEntered.AddListener(OnSocketed);
        socketInteractor.selectExited.AddListener(OnRemoved);
    }

    private void OnDisable()
    {
        socketInteractor.selectEntered.RemoveListener(OnSocketed);
        socketInteractor.selectExited.RemoveListener(OnRemoved);
    }

    private void OnSocketed(SelectEnterEventArgs args)
    {
        statusRenderer.material.color = activeColor;
        Debug.Log("Object fixed in Snap Zone: " + args.interactableObject.transform.name);
    }

    private void OnRemoved(SelectExitEventArgs args)
    {
        statusRenderer.material.color = idleColor;
    }
}
