using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public static class Lab2XRConfigurator
{
    private const string SettingsDirectory = "Assets/XR/Settings";
    private const string GeneralSettingsPath = SettingsDirectory + "/XRGeneralSettingsPerBuildTarget.asset";
    private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";

    [MenuItem("Labs/Lab 2/Configure OpenXR")]
    public static void ConfigureOpenXR()
    {
        XRGeneralSettingsPerBuildTarget perBuildTargetSettings = GetOrCreateGeneralSettings();

        ConfigureBuildTarget(perBuildTargetSettings, BuildTargetGroup.Standalone);
        ConfigureBuildTarget(perBuildTargetSettings, BuildTargetGroup.Android);

        EnableControllerProfiles(BuildTargetGroup.Standalone);
        EnableControllerProfiles(BuildTargetGroup.Android);
        EnableFeature<MetaQuestFeature>(BuildTargetGroup.Android, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("LAB2_OPENXR_CONFIG_COMPLETE: OpenXR is enabled for Standalone and Android; Meta controller profiles are active.");
    }

    private static XRGeneralSettingsPerBuildTarget GetOrCreateGeneralSettings()
    {
        if (EditorBuildSettings.TryGetConfigObject(
                XRGeneralSettings.k_SettingsKey,
                out XRGeneralSettingsPerBuildTarget existingSettings) && existingSettings != null)
        {
            return existingSettings;
        }

        XRGeneralSettingsPerBuildTarget settings =
            AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(GeneralSettingsPath);

        if (settings == null)
        {
            Directory.CreateDirectory(SettingsDirectory);
            AssetDatabase.Refresh();
            settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(settings, GeneralSettingsPath);
        }

        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
        return settings;
    }

    private static void ConfigureBuildTarget(
        XRGeneralSettingsPerBuildTarget perBuildTargetSettings,
        BuildTargetGroup buildTargetGroup)
    {
        if (!perBuildTargetSettings.HasManagerSettingsForBuildTarget(buildTargetGroup))
        {
            perBuildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(buildTargetGroup);
        }

        XRGeneralSettings generalSettings = perBuildTargetSettings.SettingsForBuildTarget(buildTargetGroup);
        generalSettings.InitManagerOnStart = true;

        if (!XRPackageMetadataStore.AssignLoader(
                generalSettings.Manager,
                OpenXRLoaderTypeName,
                buildTargetGroup))
        {
            bool alreadyAssigned = generalSettings.Manager.activeLoaders.Any(
                loader => loader != null && loader.GetType().FullName == OpenXRLoaderTypeName);

            if (!alreadyAssigned)
            {
                throw new InvalidOperationException($"Could not assign OpenXR loader for {buildTargetGroup}.");
            }
        }

        EditorUtility.SetDirty(generalSettings);
        EditorUtility.SetDirty(generalSettings.Manager);
        EditorUtility.SetDirty(perBuildTargetSettings);
        FeatureHelpers.RefreshFeatures(buildTargetGroup);
    }

    private static void EnableControllerProfiles(BuildTargetGroup buildTargetGroup)
    {
        EnableFeature<OculusTouchControllerProfile>(buildTargetGroup, true);
        EnableFeature<MetaQuestTouchPlusControllerProfile>(buildTargetGroup, true);
        EnableFeature<MetaQuestTouchProControllerProfile>(buildTargetGroup, true);
    }

    private static void EnableFeature<T>(BuildTargetGroup buildTargetGroup, bool enabled)
        where T : OpenXRFeature
    {
        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(buildTargetGroup);
        T feature = settings != null ? settings.GetFeature<T>() : null;
        if (feature == null)
        {
            throw new InvalidOperationException($"OpenXR feature {typeof(T).Name} was not created for {buildTargetGroup}.");
        }

        feature.enabled = enabled;
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(settings);
    }
}
