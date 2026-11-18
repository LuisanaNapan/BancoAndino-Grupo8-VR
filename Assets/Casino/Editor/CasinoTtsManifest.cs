#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

public sealed class CasinoTtsManifest : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        var manifest = new XmlDocument();
        manifest.Load(manifestPath);
        var root = manifest.DocumentElement;
        var queries = root.SelectSingleNode("queries");
        if (queries == null) queries = root.AppendChild(manifest.CreateElement("queries"));
        const string android = "http://schemas.android.com/apk/res/android";
        foreach (XmlElement action in queries.SelectNodes("intent/action"))
            if (action.GetAttribute("name", android) == "android.intent.action.TTS_SERVICE") return;
        var intent = queries.AppendChild(manifest.CreateElement("intent"));
        var service = (XmlElement)intent.AppendChild(manifest.CreateElement("action"));
        service.SetAttribute("name", android, "android.intent.action.TTS_SERVICE");
        manifest.Save(manifestPath);
    }
}
#endif
