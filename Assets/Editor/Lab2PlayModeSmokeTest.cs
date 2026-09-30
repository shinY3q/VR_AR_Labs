using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

[InitializeOnLoad]
public static class Lab2PlayModeSmokeTest
{
    private const string ScenePath = "Assets/Scenes/Lab2_VR.unity";
    private const string RequestedKey = "Lab2.PlayTest.Requested";
    private const string ResultReadyKey = "Lab2.PlayTest.ResultReady";
    private const string ResultCodeKey = "Lab2.PlayTest.ResultCode";

    private static double playStartedAt;

    static Lab2PlayModeSmokeTest()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [MenuItem("Labs/Lab 2/Run Play Mode Smoke Test")]
    public static void Run()
    {
        SessionState.SetBool(RequestedKey, true);
        SessionState.SetBool(ResultReadyKey, false);
        SessionState.SetInt(ResultCodeKey, 1);
        playStartedAt = 0d;

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("LAB2_PLAY_TEST_START: Entering Play Mode with the configured OpenXR runtime.");
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RequestedKey, false))
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            if (playStartedAt <= 0d)
            {
                playStartedAt = EditorApplication.timeSinceStartup;
                return;
            }

            if (EditorApplication.timeSinceStartup - playStartedAt < 8d)
            {
                return;
            }

            int resultCode = ValidateRuntime();
            SessionState.SetInt(ResultCodeKey, resultCode);
            SessionState.SetBool(ResultReadyKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (!SessionState.GetBool(ResultReadyKey, false))
        {
            return;
        }

        int exitCode = SessionState.GetInt(ResultCodeKey, 1);
        SessionState.EraseBool(RequestedKey);
        SessionState.EraseBool(ResultReadyKey);
        SessionState.EraseInt(ResultCodeKey);

        Debug.Log(exitCode == 0
            ? "LAB2_PLAY_TEST_COMPLETE: Play Mode and Meta OpenXR runtime validation passed."
            : "LAB2_PLAY_TEST_FAILED: See the validation errors above.");

        EditorApplication.update -= Tick;
        EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
    }

    private static int ValidateRuntime()
    {
        try
        {
            List<string> failures = new();

            string runtimeName = OpenXRRuntime.name;
            string runtimeVersion = OpenXRRuntime.version;
            Debug.Log($"LAB2_OPENXR_RUNTIME: {runtimeName} {runtimeVersion}");

            if (string.IsNullOrWhiteSpace(runtimeName) ||
                runtimeName.IndexOf("Meta", StringComparison.OrdinalIgnoreCase) < 0)
            {
                failures.Add($"Expected Meta OpenXR runtime, received '{runtimeName}'.");
            }

            if (XRGeneralSettings.Instance?.Manager?.activeLoader == null)
            {
                failures.Add("The OpenXR loader is not active in Play Mode.");
            }

            if (SceneManager.GetActiveScene().name != "Lab2_VR")
            {
                failures.Add("Lab2_VR is not the active scene.");
            }

            RequireComponent<XROrigin>("XR Origin", failures);
            RequireComponent<XRInteractionManager>("XR Interaction Manager", failures);
            RequireComponent<TeleportationArea>("Teleportation Area", failures);
            RequireComponent<XRGrabInteractable>("XR Grab Interactable", failures);
            RequireComponent<XRSocketInteractor>("XR Socket Interactor", failures);

            if (Camera.main == null)
            {
                failures.Add("The XR Rig does not contain a Main Camera.");
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError("LAB2_VALIDATION_ERROR: " + failure);
                }

                return 1;
            }

            Debug.Log("LAB2_RUNTIME_COMPONENTS_OK: XR Origin, camera, teleportation, grab and socket are active.");
            return 0;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return 1;
        }
    }

    private static void RequireComponent<T>(string displayName, List<string> failures)
        where T : Component
    {
        if (UnityEngine.Object.FindAnyObjectByType<T>() == null)
        {
            failures.Add(displayName + " was not found in the running scene.");
        }
    }
}
