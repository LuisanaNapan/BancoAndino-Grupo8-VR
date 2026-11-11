using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Runs inside Unity: creates assets using URP's own editor API.
public static class Semana10URPConfig
{
    const string Carpeta = "Assets/Settings/Semana10URP";
    const string PipelinePath = Carpeta + "/Semana10Pipeline.asset";
    const string Escena = "Assets/HelloCardboard/Scenes/HelloCardboard.unity";

    [MenuItem("Semana 10/Configurar VR con URP")]
    public static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Semana 10", "Deten Play antes de configurar.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            Directory.CreateDirectory(Carpeta);
            AssetDatabase.Refresh();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create();
                pipeline.name = "Semana10Pipeline";
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                // This public editor API also assigns the renderer's default resources.
                var renderer = pipeline.LoadBuiltinRendererData(RendererType.UniversalRenderer);
                string origen = AssetDatabase.GetAssetPath(renderer);
                string destino = Carpeta + "/Semana10Renderer.asset";
                if (!string.IsNullOrEmpty(origen) && origen != destino)
                {
                    string error = AssetDatabase.MoveAsset(origen, destino);
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                }
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 2;
                pipeline.renderScale = 1f;
                EditorUtility.SetDirty(pipeline);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int calidadAnterior = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = pipeline;
                }
            }
            finally { QualitySettings.SetQualityLevel(calidadAnterior, false); }

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) throw new InvalidOperationException("URP no termino de importar. Espera y repite el boton.");
            int convertidos = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/HelloCardboard" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null || material.shader == unlit) continue;
                // Only the sample's legacy materials; do not alter unrelated custom shaders.
                string nombre = material.shader != null ? material.shader.name : "";
                if (!nombre.StartsWith("Unlit/") && nombre != "Standard" && nombre != "Standard (Specular setup)") continue;
                Texture textura = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                Vector2 escala = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
                Vector2 offset = material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;
                material.shader = unlit;
                material.shaderKeywords = new string[0];
                material.SetTexture("_BaseMap", textura);
                material.SetTextureScale("_BaseMap", escala);
                material.SetTextureOffset("_BaseMap", offset);
                material.SetColor("_BaseColor", color);
                material.renderQueue = -1;
                EditorUtility.SetDirty(material);
                convertidos++;
            }

            EditorSceneManager.OpenScene(Escena);
            var camera = Camera.main;
            if (camera != null)
            {
                var datos = camera.GetUniversalAdditionalCameraData();
                datos.allowXRRendering = true;
                datos.renderPostProcessing = false;
                EditorUtility.SetDirty(datos);
            }
            Shader reticulaShader = Shader.Find("Semana10/ReticulaURP");
            if (reticulaShader == null) throw new InvalidOperationException("No se importo el shader de la reticula. Espera y repite el boton.");
            string materialPath = Carpeta + "/ReticulaURP.mat";
            var reticulaMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (reticulaMaterial == null)
            {
                reticulaMaterial = new Material(reticulaShader);
                reticulaMaterial.name = "ReticulaURP";
                AssetDatabase.CreateAsset(reticulaMaterial, materialPath);
            }
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.gameObject.name != "CardboardReticlePointer") continue;
                Undo.RecordObject(renderer, "Reticula URP");
                renderer.sharedMaterial = reticulaMaterial;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
            }
            CardboardPreparacion.Preparar();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Escena, true) };
            EditorSceneManager.MarkSceneDirty(camera != null ? camera.gameObject.scene : UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Selection.activeObject = pipeline;
            EditorUtility.DisplayDialog("URP configurado", "URP asignado en Graphics y Quality. Materiales de la demo adaptados: " + convertidos + ". Ahora genera el APK y comprueba la vista doble y el giro en el celular. El funcionamiento en Android aun requiere esa prueba.", "OK");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("No se completo la configuracion", error.Message, "OK");
        }
    }
}
