using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class Lab2InteractionReporter : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private XRSocketInteractor socketInteractor;
    [SerializeField] private Renderer statusRenderer;
    [SerializeField] private Color idleColor = new(0.15f, 0.65f, 1f, 1f);
    [SerializeField] private Color activeColor = new(0.2f, 1f, 0.35f, 1f);

    private Material runtimeMaterial;

    public void ConfigureGrab(XRGrabInteractable interactable)
    {
        grabInteractable = interactable;
    }

    public void ConfigureSocket(XRSocketInteractor interactor, Renderer renderer)
    {
        socketInteractor = interactor;
        statusRenderer = renderer;
    }

    private void Awake()
    {
        if (statusRenderer != null)
        {
            runtimeMaterial = statusRenderer.material;
            runtimeMaterial.color = idleColor;
        }
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(OnSocketed);
            socketInteractor.selectExited.AddListener(OnRemovedFromSocket);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.RemoveListener(OnSocketed);
            socketInteractor.selectExited.RemoveListener(OnRemovedFromSocket);
        }
    }

    private static void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log($"[Lab 2] Object grabbed: {args.interactableObject.transform.name}");
    }

    private static void OnReleased(SelectExitEventArgs args)
    {
        Debug.Log($"[Lab 2] Object released or thrown: {args.interactableObject.transform.name}");
    }

    private void OnSocketed(SelectEnterEventArgs args)
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.color = activeColor;
        }

        Debug.Log($"[Lab 2] Object fixed in Snap Zone: {args.interactableObject.transform.name}");
    }

    private void OnRemovedFromSocket(SelectExitEventArgs args)
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.color = idleColor;
        }

        Debug.Log($"[Lab 2] Object removed from Snap Zone: {args.interactableObject.transform.name}");
    }
}
