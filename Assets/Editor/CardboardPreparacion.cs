using System;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

public static class CardboardPreparacion
{
    public static void Preparar()
    {
        XRGeneralSettingsPerBuildTarget conjunto;
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out conjunto) || conjunto == null)
        {
            var existentes = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            conjunto = existentes.Length > 0 ? AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(existentes[0])) : null;
            if (conjunto == null)
            {
                System.IO.Directory.CreateDirectory("Assets/XR/Settings");
                AssetDatabase.Refresh();
                conjunto = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(conjunto, "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset");
            }
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, conjunto, true);
        }
        if (!conjunto.HasSettingsForBuildTarget(BuildTargetGroup.Android)) conjunto.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        if (!conjunto.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android)) conjunto.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        var general = conjunto.SettingsForBuildTarget(BuildTargetGroup.Android);
        general.InitManagerOnStart = true;
        bool ok = XRPackageMetadataStore.AssignLoader(general.Manager, typeof(Google.XR.Cardboard.XRLoader).FullName, BuildTargetGroup.Android);
        if (!ok || !general.Manager.activeLoaders.Any(l => l != null && l.GetType() == typeof(Google.XR.Cardboard.XRLoader)))
            throw new InvalidOperationException("No se pudo activar Cardboard. Ve a Project Settings > XR Plug-in Management > Android y marca Cardboard XR Plugin.");
        EditorUtility.SetDirty(conjunto);
        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(general.Manager);
        AssetDatabase.SaveAssets();
    }
    public static string Comprobar()
    {
        var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (general == null || general.Manager == null || !general.InitManagerOnStart || !general.Manager.activeLoaders.Any(l => l != null && l.GetType() == typeof(Google.XR.Cardboard.XRLoader)))
            return "Falta activar Cardboard para Android. Pulsa Preparar practica.";
        return "";
    }
}
