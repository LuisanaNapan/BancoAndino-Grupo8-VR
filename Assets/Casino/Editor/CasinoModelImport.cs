using UnityEditor;

// Original models use metres. Avoid differing import defaults across machines.
public class CasinoModelImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Casino/Branding/"))return;
        var image=(TextureImporter)assetImporter;
        image.textureType=TextureImporterType.Default;image.sRGBTexture=true;image.mipmapEnabled=true;image.maxTextureSize=2048;
    }
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Casino/Models/")) return;
        var model = (ModelImporter)assetImporter;
        model.globalScale = 1f;
        model.useFileScale = false;
        model.importCameras = false;
        model.importLights = false;
        model.importAnimation = false;
        model.isReadable = false;
        model.importNormals = ModelImporterNormals.Calculate;
        model.normalSmoothingAngle = 45f;
    }
}
