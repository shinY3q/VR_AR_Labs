using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class Lab2SceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Lab1.unity";
    private const string TargetScenePath = "Assets/Scenes/Lab2_VR.unity";
    private const string XriPackageName = "com.unity.xr.interaction.toolkit";

    [MenuItem("Labs/Lab 2/Build VR Scene")]
    public static void BuildScene()
    {
        ImportStarterAssets();

        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        RemoveDesktopPlayer();

        GameObject systems = new("LAB2_VR_SYSTEM");
        GameObject interactionManagerObject = new("XR Interaction Manager");
        interactionManagerObject.transform.SetParent(systems.transform);
        interactionManagerObject.AddComponent<XRInteractionManager>();

        GameObject xrOriginPrefab = FindStarterPrefab("XR Origin (XR Rig)");
        GameObject xrOrigin = PrefabUtility.InstantiatePrefab(xrOriginPrefab) as GameObject;
        if (xrOrigin == null)
        {
            throw new InvalidOperationException("Could not instantiate the XR Origin prefab.");
        }

        xrOrigin.name = "XR Origin (XR Rig)";
        xrOrigin.transform.SetParent(systems.transform);
        xrOrigin.transform.SetPositionAndRotation(new Vector3(0f, 0f, -4f), Quaternion.identity);
        xrOrigin.tag = "Player";

        DisableSmoothMove(xrOrigin.transform);
        ConfigureTeleportationArea();
        ConfigureGrabbableCalibrationSample();
        ConfigureSnapZone();

        EditorSceneManager.SaveScene(scene, TargetScenePath);
        AddSceneToBuildSettings(TargetScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("LAB2_BUILD_COMPLETE: Lab2_VR scene contains XR Origin, teleportation, grab/throw object and Snap Zone.");
    }

    private static void ImportStarterAssets()
    {
        UnityEditor.PackageManager.PackageInfo packageInfo =
            UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{XriPackageName}");
        if (packageInfo == null)
        {
            throw new InvalidOperationException("XR Interaction Toolkit is not installed.");
        }

        Sample starterAssets = Sample.FindByPackage(XriPackageName, packageInfo.version)
            .FirstOrDefault(sample => sample.displayName == "Starter Assets");

        if (string.IsNullOrWhiteSpace(starterAssets.displayName))
        {
            throw new InvalidOperationException("XR Interaction Toolkit Starter Assets sample was not found.");
        }

        if (!starterAssets.isImported && !starterAssets.Import(
                Sample.ImportOptions.OverridePreviousImports | Sample.ImportOptions.HideImportWindow))
        {
            throw new InvalidOperationException("XR Interaction Toolkit Starter Assets could not be imported.");
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static GameObject FindStarterPrefab(string prefabName)
    {
        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab", new[] { "Assets/Samples" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.name == prefabName)
            {
                return prefab;
            }
        }

        throw new InvalidOperationException($"Starter Assets prefab was not found: {prefabName}");
    }

    private static void RemoveDesktopPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            UnityEngine.Object.DestroyImmediate(player);
        }

        foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
        {
            if (camera != null && camera.CompareTag("MainCamera"))
            {
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
        }
    }

    private static void DisableSmoothMove(Transform xrOrigin)
    {
        Transform moveProvider = FindDeepChild(xrOrigin, "Move");
        if (moveProvider != null)
        {
            moveProvider.gameObject.SetActive(false);
        }
    }

    private static void ConfigureTeleportationArea()
    {
        GameObject floor = GameObject.Find("Floor");
        if (floor == null)
        {
            throw new InvalidOperationException("Floor object was not found in Lab1 scene.");
        }

        if (floor.GetComponent<Collider>() == null)
        {
            floor.AddComponent<BoxCollider>();
        }

        if (floor.GetComponent<TeleportationArea>() == null)
        {
            floor.AddComponent<TeleportationArea>();
        }

        floor.name = "Floor_TeleportationArea";
    }

    private static void ConfigureGrabbableCalibrationSample()
    {
        GameObject sample = GameObject.Find("CalibrationSample");
        if (sample == null)
        {
            sample = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sample.name = "CalibrationSample";
        }

        GameObject table = GameObject.Find("WorkTable");
        Vector3 tablePosition = table != null ? table.transform.position : new Vector3(-2f, 0f, 1.5f);
        sample.transform.SetPositionAndRotation(tablePosition + new Vector3(-0.55f, 1.35f, 0f), Quaternion.identity);
        sample.transform.localScale = new Vector3(0.35f, 0.18f, 0.5f);

        Rigidbody rigidbody = sample.GetComponent<Rigidbody>() ?? sample.AddComponent<Rigidbody>();
        rigidbody.mass = 0.6f;
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (sample.GetComponent<Collider>() == null)
        {
            sample.AddComponent<BoxCollider>();
        }

        XRGrabInteractable grabInteractable = sample.GetComponent<XRGrabInteractable>() ??
                                               sample.AddComponent<XRGrabInteractable>();
        grabInteractable.throwOnDetach = true;
        grabInteractable.forceGravityOnDetach = true;

        Lab2InteractionReporter reporter = sample.GetComponent<Lab2InteractionReporter>() ??
                                           sample.AddComponent<Lab2InteractionReporter>();
        reporter.ConfigureGrab(grabInteractable);
    }

    private static void ConfigureSnapZone()
    {
        GameObject table = GameObject.Find("WorkTable");
        Vector3 tablePosition = table != null ? table.transform.position : new Vector3(-2f, 0f, 1.5f);

        GameObject snapZone = new("SnapZone_SocketInteractor");
        snapZone.transform.SetPositionAndRotation(tablePosition + new Vector3(0.65f, 1.22f, 0f), Quaternion.identity);

        SphereCollider trigger = snapZone.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.3f;

        XRSocketInteractor socketInteractor = snapZone.AddComponent<XRSocketInteractor>();

        GameObject attachPoint = new("Attach Point");
        attachPoint.transform.SetParent(snapZone.transform, false);
        attachPoint.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        socketInteractor.attachTransform = attachPoint.transform;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Snap Zone Marker";
        marker.transform.SetParent(snapZone.transform, false);
        marker.transform.localScale = new Vector3(0.32f, 0.015f, 0.32f);
        UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());

        Material markerMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CalibrationZone.mat");
        Renderer markerRenderer = marker.GetComponent<Renderer>();
        if (markerMaterial != null)
        {
            markerRenderer.sharedMaterial = markerMaterial;
        }

        Lab2InteractionReporter reporter = snapZone.AddComponent<Lab2InteractionReporter>();
        reporter.ConfigureSocket(socketInteractor, markerRenderer);
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, name);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.All(scene => scene.path != scenePath))
        {
            EditorBuildSettings.scenes = scenes
                .Concat(new[] { new EditorBuildSettingsScene(scenePath, true) })
                .ToArray();
        }
    }
}
