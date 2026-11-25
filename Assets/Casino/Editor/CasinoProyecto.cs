using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CasinoProyecto : EditorWindow
{
    public const string ScenePath = "Assets/Casino/Scenes/Casino_VR.unity";
    const string Root = "Assets/Casino";
    static Material navy, dark, teal, gold, white, red, green, panel, focus, neonGold, neonTeal;
    static Font font,displayFont,controlsFont;
    static int interactiveLayer;
    static Transform environment;
    static bool lastBuildFailed;
    static Scene buildingScene;
    static string buildPhase="Inicio";
    string message = "VERSION 5 CORREGIDA / Modelos 3D originales. Crea el escenario una vez. Despues abre la escena para continuar editandola.";

    [MenuItem("Casino 8/Abrir proyecto")]
    public static void Open() { var w = GetWindow<CasinoProyecto>("Casino 8"); w.minSize = new Vector2(470, 420); }
    void OnGUI()
    {
        GUILayout.Label("BANCO ANDINO / ESCENARIO 08", EditorStyles.boldLabel);
        GUILayout.Label("CASA DE CAMBIO Y CASINO", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox("Dos recorridos: Ataque y Defensa. Conserva la demo HelloCardboard original. El escenario se genera dentro de esta copia del proyecto.", MessageType.Info);
        if (GUILayout.Button("1. CREAR ESCENARIO (una sola vez)", GUILayout.Height(42)))
            EditorApplication.delayCall += () => Run(CreateScenario);
        if (GUILayout.Button("2. ABRIR ESCENA CASINO", GUILayout.Height(34)))
            EditorApplication.delayCall += () => Run(() => { if (!File.Exists(ScenePath)) throw new InvalidOperationException("Primero crea el escenario."); if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); });
        EditorGUILayout.HelpBox("PRUEBA PC: Play > Game. Clic izquierdo en objetos. Manten clic derecho y mueve el mouse para mirar. WASD: desplazarte. 1/2: modos. R: reiniciar. G: mirada. M: musica. Sin celular puedes probar los dos recorridos completos.", MessageType.None);
        if (GUILayout.Button("3. REVISAR PROYECTO", GUILayout.Height(32))) message = Validar();
        if(GUILayout.Button("PRUEBA DE RECORRIDOS (en Play)",GUILayout.Height(30))) CasinoRuntimeCheck.Begin();
        if (GUILayout.Button("PRUEBA DE RECORRIDOS Y CAPTURAS (en Play)", GUILayout.Height(30)))
            CasinoRuntimeCheck.Begin(capture: true);
        if (GUILayout.Button("4. GENERAR APK DEL CASINO", GUILayout.Height(42))) EditorApplication.delayCall += () => Run(Build);
        EditorGUILayout.HelpBox(string.IsNullOrEmpty(message) ? "Comprobaciones de configuracion correctas. Falta probar el APK en Android." : message, MessageType.Info);
        EditorGUILayout.HelpBox("El APK de VR necesita una prueba real de vista doble y giro. Los controles de PC no validan sensores ni el inicio de XR en el telefono.", MessageType.None);
    }
    void Run(Action action)
    {
        try { lastBuildFailed=false;action();message=lastBuildFailed?"Creacion cancelada. Revisa el aviso y Casino_Creacion_Error.txt.":"Accion completada."; }
        catch (Exception e) { message = e.Message; Debug.LogException(e); }
        Repaint();
    }

    public static string Validar()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return "Espera a que terminen las tareas y la compilacion de scripts.";
        if (SceneManager.GetActiveScene().path != ScenePath) return "Abre Casino_VR antes de compilar el casino.";
        if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)) return "Falta URP. Crea el escenario para configurarlo.";
        var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        string setupProblem;
        if(game!=null&&!game.ValidateConfiguration(out setupProblem))return setupProblem;
        if (game == null || game.data == null || game.ui == null || game.playerRig == null) return "Faltan referencias del GameManager.";
        if (game.chipAnimator == null || game.chipAnimator.chips.Length < 2 || game.aml == null || game.counter == null) return "Faltan componentes de fichas, alerta o contador.";
        if (UnityEngine.Object.FindObjectsByType<CasinoInteractable>(FindObjectsSortMode.None).Length < 5) return "Faltan interactuables.";
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP || PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64) return "Se requiere IL2CPP y ARM64.";
        if (PlayerSettings.Android.applicationEntry != AndroidApplicationEntry.Activity) return "Usa Activity como Application Entry Point.";
        string xr = CardboardPreparacion.Comprobar();
        return xr;
    }

    static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Deten Play antes de generar.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUtility.DisplayDialog("Android", "Espera a que termine el cambio de plataforma y vuelve a pulsar Generar APK.", "OK");
            return;
        }
        string error = Validar();
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string path = EditorUtility.SaveFilePanel("Guardar APK Casino", "", "BancoAndino_Casino", "apk");
        if (string.IsNullOrEmpty(path)) return;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, path, BuildTarget.Android, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("No se genero el APK. Revisa el primer error de Console.");
        EditorUtility.RevealInFinder(path);
        EditorUtility.DisplayDialog("APK generado", "Instala esta app por separado de PruebaVR. Comprueba ambos modos, vista doble y seguimiento de cabeza en el telefono.", "OK");
    }

    static Material Mat(string name, Color color)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("Espera a que URP termine de importar.");
            m = new Material(shader); AssetDatabase.CreateAsset(m, path);
        }
        m.enableInstancing=true;
        m.SetColor("_BaseColor", color); EditorUtility.SetDirty(m); return m;
    }
    static GameObject Shape(string name, PrimitiveType type, Vector3 at, Vector3 size, Material material, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(environment, false);
        go.transform.position = at; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static GameObject Box(string name, Vector3 at, Vector3 size, Material material) { return Shape(name, PrimitiveType.Cube, at, size, material); }
    static Transform Spawn(string name, Vector3 at)
    { var go = new GameObject(name); go.transform.position = at; return go.transform; }

    static void LoadEnvironment(float x, string modelName)
    {
        string path = Root + "/Models/" + modelName + ".obj";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new InvalidOperationException("Espera a que terminen de importar los modelos 3D.");
        var art = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        art.transform.SetParent(environment, false); art.transform.localPosition = new Vector3(x, 0, 0);
        foreach (var r in art.GetComponentsInChildren<Renderer>())
        {
            string objectKey=r.gameObject.name;
            int split=objectKey.LastIndexOf('_');if(split>0)objectKey=objectKey.Substring(0,split);
            var imported=r.sharedMaterials;
            var mapped=new Material[imported.Length];
            for(int i=0;i<mapped.Length;i++)
            {
                string key=imported[i]!=null?imported[i].name.Replace(" (Instance)",""):objectKey;
                mapped[i]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Art_"+key+".mat");
                if(mapped[i]==null)mapped[i]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Art_"+objectKey+".mat");
                if(mapped[i]==null)
                {
                    Color c=imported[i]!=null&&imported[i].HasProperty("_Color")?imported[i].color:new Color(.2f,.15f,.1f);
                    string safe=System.Text.RegularExpressions.Regex.Replace(key,"[^a-zA-Z0-9_]","_");
                    mapped[i]=Mat("Recuperado_"+safe,c);mapped[i].shader=Shader.Find("Universal Render Pipeline/Lit");EditorUtility.SetDirty(mapped[i]);
                }
            }
            r.sharedMaterials=mapped;
            var mf=r.GetComponent<MeshFilter>();
            if(mf!=null && mf.sharedMesh!=null)
            {
                var solid=r.gameObject.AddComponent<MeshCollider>();solid.sharedMesh=mf.sharedMesh;
            }
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }
    static CasinoInteractable ObjectTarget(string name, CasinoAction action, string hint, Vector3 at, Vector3 size)
    {
        var g = new GameObject(name); g.transform.SetParent(environment, false); g.transform.position = at;
        g.layer = interactiveLayer; g.AddComponent<BoxCollider>().size = size;
        var t = g.AddComponent<CasinoInteractable>(); t.action = action; t.prompt = hint;
        var outline=new GameObject("Contorno_foco");outline.transform.SetParent(g.transform,false);
        var line=outline.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=4;
        float w=size.x*.5f,h=size.y*.5f,z=-size.z*.5f-.015f;
        line.SetPositions(new[]{new Vector3(-w,-h,z),new Vector3(-w,h,z),new Vector3(w,h,z),new Vector3(w,-h,z)});
        line.startWidth=.015f;line.endWidth=.015f;line.sharedMaterial=focus;
        outline.SetActive(false);t.focusDecoration=outline;
        return t;
    }
    static void CreateHost(float x,float z,bool casino)
    {
        var root=new GameObject(casino?"Anfitrion":"Analista");root.transform.SetParent(environment,false);
        var head=new GameObject("Cabeza");head.transform.position=new Vector3(x,1.78f,z);head.transform.SetParent(root.transform,true);
        Material skin=Mat("Piel",new Color(.62f,.39f,.23f));Material suit=Mat("Traje",new Color(.025f,.04f,.055f));
        var body=Shape("Chaqueta",PrimitiveType.Capsule,new Vector3(x,1.13f,z),new Vector3(.53f,.40f,.33f),suit);body.transform.SetParent(root.transform,true);
        var shirt=Box("Camisa",new Vector3(x,1.25f,z-.17f),new Vector3(.19f,.43f,.025f),white);shirt.transform.SetParent(root.transform,true);
        foreach(float dx in new[]{-.15f,.15f})
        {
            var leg=Shape("Pantalon",PrimitiveType.Cylinder,new Vector3(x+dx,.42f,z),new Vector3(.20f,.42f,.23f),suit);leg.transform.SetParent(root.transform,true);
            var shoe=Shape("Zapato",PrimitiveType.Sphere,new Vector3(x+dx,.06f,z-.06f),new Vector3(.22f,.12f,.4f),dark);shoe.transform.SetParent(root.transform,true);
        }
        foreach(float dx in new[]{-.34f,.34f})
        {
            var arm=Shape("Manga",PrimitiveType.Capsule,new Vector3(x+dx,1.13f,z),new Vector3(.17f,.27f,.18f),suit);arm.transform.SetParent(root.transform,true);
            var hand=Shape("Mano",PrimitiveType.Sphere,new Vector3(x+dx,.83f,z-.015f),new Vector3(.14f,.19f,.13f),skin);hand.transform.SetParent(root.transform,true);
        }
        var face=Shape("Rostro",PrimitiveType.Sphere,new Vector3(x,1.78f,z),new Vector3(.38f,.47f,.35f),skin);face.transform.SetParent(head.transform,true);
        var hair=Shape("Cabello",PrimitiveType.Sphere,new Vector3(x,1.96f,z+.02f),new Vector3(.39f,.20f,.35f),dark);hair.transform.SetParent(head.transform,true);
        foreach(float dx in new[]{-.08f,.08f})
        {
            var eye=Shape("Ojo",PrimitiveType.Sphere,new Vector3(x+dx,1.81f,z-.16f),new Vector3(.035f,.035f,.025f),dark);eye.transform.SetParent(head.transform,true);
        }
        var tie=Box("Corbatin",new Vector3(x,1.46f,z-.18f),new Vector3(.21f,.085f,.045f),red);tie.transform.SetParent(root.transform,true);
        ObjectTarget("Hablar_"+root.name,CasinoAction.Host,casino?"Hablar con el anfitrion":"Hablar con el analista",new Vector3(x,1.15f,z),new Vector3(.8f,1.95f,.65f));
        var role=Panel("Rol_"+root.name,new Vector3(x,2.17f,z-.05f),new Vector2(.9f,.16f));
        Label(role,casino?"ANFITRION":"ANALISTA",Vector2.zero,new Vector2(950,150),60,controlsFont,Color.white);
        var idle=root.AddComponent<CasinoHostIdle>();idle.head=head.transform;
    }
    static RectTransform Panel(string title,Vector3 position,Vector2 metres)
    {
        var go=new GameObject(title,typeof(RectTransform));var canvas=go.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;go.transform.SetParent(environment,false);
        var rt=go.GetComponent<RectTransform>();rt.sizeDelta=new Vector2(1000,1000*metres.y/metres.x);
        rt.position=position;rt.localScale=Vector3.one*(metres.x/1000f);
        canvas.sortingOrder=1;
        return rt;
    }
    static RectTransform Rect(Transform parent,string title,Vector2 at,Vector2 size)
    {
        var go=new GameObject(title,typeof(RectTransform));go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=at;rt.sizeDelta=size;return rt;
    }
    static void Background(RectTransform root,Color color)
    {
        var edge=Rect(root,"Borde",Vector2.zero,root.sizeDelta+Vector2.one*8).gameObject.AddComponent<Image>();edge.color=new Color(.14f,.63f,.72f);
        var bg=Rect(root,"Fondo",Vector2.zero,root.sizeDelta).gameObject.AddComponent<Image>();bg.color=color;
    }
    static UnityEngine.UI.Text Label(Transform parent,string value,Vector2 at,Vector2 size,int pixels,Font type,Color color)
    {
        var text=Rect(parent,"Texto",at,size).gameObject.AddComponent<UnityEngine.UI.Text>();
        text.font=type;text.fontSize=pixels;text.fontStyle=FontStyle.Normal;text.alignment=TextAnchor.MiddleCenter;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.supportRichText=false;
        text.lineSpacing=1.12f;text.color=color;text.text=value;text.raycastTarget=false;return text;
    }
    static Texture2D NativeTexture(string source)
    {
        const string dir=Root+"/GeneratedTextures";
        if(!AssetDatabase.IsValidFolder(dir))AssetDatabase.CreateFolder(Root,"GeneratedTextures");
        string destination=dir+"/"+Path.GetFileNameWithoutExtension(source)+".asset";
        var cached=AssetDatabase.LoadAssetAtPath<Texture2D>(destination);if(cached!=null)return cached;
        if(!File.Exists(source))throw new FileNotFoundException("Falta imagen",source);
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,true);texture.name=Path.GetFileNameWithoutExtension(source);
        if(!texture.LoadImage(File.ReadAllBytes(source),false))throw new InvalidOperationException("No se pudo decodificar "+source);
        texture.filterMode=FilterMode.Trilinear;texture.wrapMode=TextureWrapMode.Clamp;
        AssetDatabase.CreateAsset(texture,destination);EditorUtility.SetDirty(texture);return texture;
    }
    static void Picture(Transform parent,string path,Vector2 at,Vector2 size)
    {
        var texture=NativeTexture(path);
        var image=Rect(parent,"Imagen",at,size).gameObject.AddComponent<RawImage>();
        if(image==null)throw new InvalidOperationException("Unity no pudo crear RawImage: "+path);
        image.texture=texture;image.color=Color.white;image.raycastTarget=false;
    }
    static CasinoActionGate Control(RectTransform root,string title,string label,CasinoAction action,Vector2 at,bool preview=true,bool gated=true)
    {
        var rt=Rect(root,title,at,new Vector2(860,125));rt.localPosition+=Vector3.back*22;
        var face=rt.gameObject.AddComponent<Image>();face.color=new Color(.055f,.22f,.25f);
        var accent=Rect(rt,"Acento",new Vector2(-422,0),new Vector2(8,125)).gameObject.AddComponent<Image>();accent.color=new Color(.94f,.74f,.37f);
        var caption=Label(rt,label,Vector2.zero,new Vector2(810,110),64,controlsFont,Color.white);
        var hit=rt.gameObject.AddComponent<BoxCollider>();hit.size=new Vector3(860,125,24);rt.gameObject.layer=interactiveLayer;
        var target=rt.gameObject.AddComponent<CasinoInteractable>();target.action=action;target.prompt=label;target.uiFace=face;target.idleColor=face.color;target.focusColor=new Color(.09f,.39f,.42f);
        var gate=root.gameObject.AddComponent<CasinoActionGate>();gate.target=target;gate.visuals=rt.gameObject;gate.hit=hit;gate.background=face;gate.label=caption;gate.caption=label;gate.normal=face.color;gate.gated=gated;
        rt.gameObject.SetActive(preview);return gate;
    }
    static void BuildMenu(float x)
    {
        var menu=Panel("Menu_recorrido",new Vector3(x-4.85f,1.7f,-3.2f),new Vector2(1.3f,1.65f));menu.rotation=Quaternion.Euler(0,-75,0);Background(menu,new Color(.018f,.05f,.065f));
        Label(menu,"OPERACIONES",new Vector2(0,475),new Vector2(900,105),64,displayFont,new Color(1,.83f,.43f));
        Control(menu,"Modo_Ataque","ATAQUE",CasinoAction.Attack,new Vector2(0,240),true,false);
        Control(menu,"Modo_Defensa","DEFENSA",CasinoAction.Defense,new Vector2(0,65),true,false);
        Control(menu,"Reiniciar","REINICIAR",CasinoAction.Restart,new Vector2(0,-160),true,false);
        Control(menu,"Musica","MUSICA / SILENCIAR",CasinoAction.ToggleMusic,new Vector2(0,-335),true,false);
        Control(menu,"Repetir_guia","REPETIR GUÍA",CasinoAction.ReplayGuide,new Vector2(0,-510),true,false);
    }
    static void BuildPortal(float x,bool casino)
    {
        var sign=Panel("Portal_rotulo",new Vector3(x+4.4f,2.9f,2.48f),new Vector2(1.5f,.32f));
        Label(sign,casino?"CASA DE CAMBIO":"ENTRAR AL CASINO",Vector2.zero,new Vector2(960,200),64,controlsFont,Color.white);
        ObjectTarget("Portal",casino?CasinoAction.ToExchange:CasinoAction.ToCasino,"Cruzar portal",new Vector3(x+4.4f,1.5f,2.45f),new Vector3(1.4f,2.7f,.18f)).GetComponent<BoxCollider>().isTrigger=true;
    }
    static void BuildTeleport(float x,float z,CasinoAction action,string label)
    {
        Shape("Anillo_teleport",PrimitiveType.Cylinder,new Vector3(x,.06f,z),new Vector3(.88f,.012f,.88f),neonTeal);
        Shape("Centro_teleport",PrimitiveType.Cylinder,new Vector3(x,.085f,z),new Vector3(.73f,.008f,.73f),dark);
        var target=ObjectTarget("TeleportPoint",action,label,new Vector3(x,.10f,z),new Vector3(.92f,.12f,.92f));target.GetComponent<BoxCollider>().isTrigger=true;
        var tag=Panel("Etiqueta_teleport",new Vector3(x,.115f,z),new Vector2(.66f,.20f));tag.rotation=Quaternion.Euler(90,0,0);
        Label(tag,label,Vector2.zero,new Vector2(960,290),96,controlsFont,Color.white);
    }
    static void BuildInfo(UIManager ui)
    {
        var card=Panel("Tarjeta_contexto",new Vector3(0,1.8f,-2),new Vector2(1.8f,1.35f));Background(card,new Color(.018f,.05f,.065f));
        ui.infoRoot=card;ui.infoText=Label(card,"",new Vector2(0,135),new Vector2(900,340),37,font,Color.white);
        Control(card,"Info_next","CONTINUAR",CasinoAction.InfoNext,new Vector2(0,-155),true,false);
        Control(card,"Info_close","CERRAR LECTURA",CasinoAction.InfoClose,new Vector2(0,-300),true,false);
        card.gameObject.SetActive(false);
    }

    public static void CreateScenario()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Deten Play antes de crear.");
        buildingScene=default(Scene);buildPhase="Preparacion";
        try{CreateScenarioInternal();}
        catch(Exception error)
        {
            lastBuildFailed=true;
            string report="CASINO V5 / CREACION INTERRUMPIDA\nPaso: "+buildPhase+"\n"+error;
            File.WriteAllText("Casino_Creacion_Error.txt",report);
            if(buildingScene.IsValid()&&buildingScene.isLoaded)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                const string sample="Assets/HelloCardboard/Scenes/HelloCardboard.unity";
                if(File.Exists(sample))EditorSceneManager.OpenScene(sample);
            }
            EditorUtility.DisplayDialog("No se creo el casino", "La creacion no termino y no se habilito el casino.\nPaso: "+buildPhase+"\n"+error.Message+"\nEl detalle se guardo en Casino_Creacion_Error.txt, junto a Assets.","OK");
            Debug.LogError(report);
        }
    }
    static void CreateScenarioInternal()

    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Deten Play.");
        buildPhase="Comprobacion de archivos";
        foreach(string dependency in new[]{"Fonts/CasinoBody.ttf","Fonts/CasinoDisplay.ttf","Fonts/CasinoControls.ttf","Branding/Casino.png","Branding/Cambio.png","Branding/Card7.png","Branding/Card6.png","Branding/CardBack.png","Models/CasaCambio.obj","Models/CasinoHorizonte.obj"})
            if(AssetDatabase.LoadMainAssetAtPath(Root+"/"+dependency)==null)throw new InvalidOperationException("Espera a que termine de importar: "+dependency);
        if (File.Exists(ScenePath))
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            var existing=UnityEngine.Object.FindFirstObjectByType<GameManager>();string reason="";
            if(existing==null||!existing.ValidateConfiguration(out reason))
            {
                string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/Casino/Scenes/Casino_Incompleto_Respaldo.unity");
                string moveError=AssetDatabase.MoveAsset(ScenePath,backup);
                if(!string.IsNullOrEmpty(moveError))throw new InvalidOperationException(moveError);
                EditorUtility.DisplayDialog("Escenario incompleto", "La copia incompleta se conservo como respaldo. Vuelve a pulsar CREAR ESCENARIO para reconstruirlo.","OK");return;
            }
            EditorUtility.DisplayDialog("Escenario existente", "Se abrio tu escena. No se reemplazo tu trabajo. Para reconstruir desde cero, renombra primero Casino_VR.unity.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)) Semana10URPConfig.Configurar();
        if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)) throw new InvalidOperationException("URP no se configuro. Espera y vuelve a crear.");
        foreach (string sub in new[] { "Materials", "Scenes", "Data" }) Directory.CreateDirectory(Root + "/" + sub);
        AssetDatabase.Refresh();
        interactiveLayer = LayerMask.NameToLayer("Interactive");
        if (interactiveLayer < 0) throw new InvalidOperationException("Falta la capa Interactive en el proyecto base.");
        navy = Mat("Navy", new Color(0.025f, 0.065f, 0.105f)); dark = Mat("Carbon", new Color(0.012f, 0.025f, 0.04f));
        panel = Mat("Panel", new Color(0.06f, 0.13f, 0.18f)); teal = Mat("Teal", new Color(0.015f, 0.38f, 0.38f));
        gold = Mat("Gold", new Color(0.72f, 0.5f, 0.19f)); white = Mat("Ivory", new Color(0.88f, 0.9f, 0.85f));
        red = Mat("Coral", new Color(0.66f, 0.16f, 0.17f)); green = Mat("Felt", new Color(0.025f, 0.17f, 0.13f));
        focus = Mat("Focus",new Color(1f,.73f,.22f));
        neonGold=Mat("NeonGold",new Color(1f,.84f,.37f));
        neonTeal=Mat("NeonTeal",new Color(.1f,.9f,.85f));
        
        { var m = Mat("Art_cream", new Color(0.22f,0.3f,0.39f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.22f,0.3f,0.39f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_plaster", new Color(0.23f,0.32f,0.42f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.23f,0.32f,0.42f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_wood", new Color(0.035f,0.085f,0.135f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.035f,0.085f,0.135f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_woodlight", new Color(0.17f,0.30f,0.37f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.17f,0.30f,0.37f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_gold", new Color(0.1f,0.52f,0.63f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.1f,0.52f,0.63f)); m.SetFloat("_Smoothness", .5f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_brasslight", new Color(0.45f,0.78f,0.88f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.45f,0.78f,0.88f)); m.SetFloat("_Smoothness", .5f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_teal", new Color(0.04f,0.29f,0.39f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.04f,0.29f,0.39f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_felt", new Color(0.035f,0.24f,0.16f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.035f,0.24f,0.16f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_navy", new Color(0.035f,0.07f,0.09f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.035f,0.07f,0.09f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_burgundy", new Color(0.23f,0.07f,0.12f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.23f,0.07f,0.12f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_velvet", new Color(0.35f,0.12f,0.18f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.35f,0.12f,0.18f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_paper", new Color(0.93f,0.9f,0.79f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.93f,0.9f,0.79f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_black", new Color(0.018f,0.025f,0.027f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.018f,0.025f,0.027f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_screen", new Color(0.03f,0.48f,0.49f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.03f,0.48f,0.49f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_light", new Color(.55f,.88f,1f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(.55f,.88f,1f)); m.SetFloat("_Smoothness", .18f); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.22f,.55f,.75f)); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_carpet", new Color(0.075f,0.11f,0.19f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.075f,0.11f,0.19f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_carpetline", new Color(0.15f,0.31f,0.44f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.15f,0.31f,0.44f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        { var m = Mat("Art_blue", new Color(0.06f,0.29f,0.55f)); m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", new Color(0.06f,0.29f,0.55f)); m.SetFloat("_Smoothness", .18f); EditorUtility.SetDirty(m); }
        buildPhase="Fuentes";
        controlsFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/CasinoControls.ttf");
        font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/CasinoBody.ttf");
        displayFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/CasinoDisplay.ttf");
        if(font==null||displayFont==null||controlsFont==null)throw new InvalidOperationException("Espera a que terminen de importar las fuentes.");
        // Copy the working sample rig, preserving its exact XR pose bindings.
        buildPhase="Camara Cardboard";
        var sourceScene = EditorSceneManager.OpenScene("Assets/HelloCardboard/Scenes/HelloCardboard.unity");
        var sourcePlayer = GameObject.Find("Player");
        if (sourcePlayer == null) throw new InvalidOperationException("No se encontro Player en HelloCardboard.");
        var rig = UnityEngine.Object.Instantiate(sourcePlayer); rig.name = "Player_Casino";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);buildingScene=scene;
        SceneManager.MoveGameObjectToScene(rig, scene); SceneManager.SetActiveScene(scene);
        EditorSceneManager.CloseScene(sourceScene, true);
        foreach (var diagnostic in rig.GetComponentsInChildren<DiagnosticoVR>(true)) UnityEngine.Object.DestroyImmediate(diagnostic);
        foreach (var t in rig.GetComponentsInChildren<Transform>(true)) if (t != null && t.name == "CardboardReticlePointer") UnityEngine.Object.DestroyImmediate(t.gameObject);
        foreach(var extra in rig.GetComponentsInChildren<GraphicsAPITextController>(true)) UnityEngine.Object.DestroyImmediate(extra.gameObject);
        var camera = rig.GetComponentInChildren<Camera>();
        camera.nearClipPlane = 0.03f; camera.farClipPlane = 80; camera.fieldOfView = 70;
        camera.backgroundColor = new Color(0.01f, 0.02f, 0.035f);
        camera.GetUniversalAdditionalCameraData().allowXRRendering = true;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        if (rig.GetComponent<CardboardStartup>() == null) rig.AddComponent<CardboardStartup>();
        var pointer = rig.AddComponent<CasinoPointer>(); pointer.view = camera; pointer.interactionMask = 1 << interactiveLayer;
        environment = new GameObject("Casino_Arte").transform;
        var dot = Shape("Reticula_Casino", PrimitiveType.Sphere, camera.transform.position + camera.transform.forward, Vector3.one * 0.009f, white);
        dot.transform.SetParent(camera.transform, true); pointer.reticle = dot.transform; pointer.reticleRenderer = dot.GetComponent<Renderer>();
        var systems = new GameObject("GameManager"); var game = systems.AddComponent<GameManager>();game.enabled=false;game.generationComplete=false;
        var ui = systems.AddComponent<UIManager>(); var counter = systems.AddComponent<MoneyCounter>();
        var aml = systems.AddComponent<AmlAlert>(); var sound = systems.AddComponent<AudioManager>();
        game.ui = ui; game.counter = counter; game.aml = aml; game.audioManager = sound; game.playerRig = rig.transform;
        game.exchangeSpawn = Spawn("Spawn_Cambio", new Vector3(0, 1.75f, -4.5f));
        game.casinoSpawn = Spawn("Spawn_Casino", new Vector3(20, 1.75f, -4.5f));
        rig.transform.position = game.exchangeSpawn.position;
        var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(Root + "/Data/Casino08.asset");
        if (data == null) { data = ScriptableObject.CreateInstance<ScenarioData>(); AssetDatabase.CreateAsset(data, Root + "/Data/Casino08.asset"); }
        game.data=data;ui.font=font;ui.displayFont=displayFont;ui.view=camera;
        var counts=new List<UnityEngine.UI.Text>();var modes=new List<UnityEngine.UI.Text>();var objectives=new List<UnityEngine.UI.Text>();
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.47f,.54f,.64f);
        var sun=new GameObject("Luz_suave").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.65f;sun.transform.rotation=Quaternion.Euler(45,-25,0);sun.shadows=LightShadows.None;
        for(int room=0;room<2;room++)
        {
            float x=room*20;bool casino=room==1;buildPhase=casino?"Modelos del casino":"Modelos de casa de cambio";LoadEnvironment(x,casino?"CasinoHorizonte":"CasaCambio");
            buildPhase=casino?"Interfaz del casino":"Interfaz de casa de cambio";
            var monitor = Panel("Monitor_del_incidente", new Vector3(x,2.96f,5.27f), new Vector2(6.23f,1.93f));
            Background(monitor, new Color(.022f,.062f,.10f));
            var blue = new Color(.37f,.91f,1f);
            var pale = new Color(.85f,.93f,1f);
            var strip = Rect(monitor, "Linea de señal", new Vector2(0,145), new Vector2(960,5)).gameObject.AddComponent<Image>();
            strip.color = blue;
            Label(monitor, casino ? "CASINO HORIZONTE / CASO 08" : "BANCO ANDINO / CASO 08",
                new Vector2(0,108), new Vector2(950,49), 33, displayFont, blue);
            if (casino)
            {
                Label(monitor,"TRAZABILIDAD DE FONDOS",new Vector2(0,22),new Vector2(950,66),45,displayFont,pale);
                Label(monitor,"01 FICHAS  /  02 CARTAS  /  03 CANJE",new Vector2(0,-79),new Vector2(950,58),30,controlsFont,blue);
            }
            else
            {
                ui.incidentTitle = Label(monitor,"INCIDENTE EN CURSO",new Vector2(0,24),new Vector2(950,66),42,displayFont,pale);
                ui.incidentInstruction = Label(monitor,"Selecciona un recorrido",new Vector2(0,-68),new Vector2(950,90),31,font,pale);
                ui.incidentStrip = strip;
            }
            for(int i=0;i<2;i++){var l=new GameObject("Luz_interior").AddComponent<Light>();l.type=LightType.Point;l.range=9;l.intensity=2.2f;l.color=casino?new Color(.68f,.69f,1f):new Color(.58f,.86f,1f);l.shadows=LightShadows.None;l.transform.position=new Vector3(x+(i==0?-2:2),3.7f,0);}
            var auditSign = Panel("Estado_del_caso", new Vector3(x, 3.77f, 5.23f), new Vector2(4.9f, .38f));
            Background(auditSign, new Color(.016f,.09f,.12f));
            Label(auditSign, casino ? "TRAMO 02 / TRAZABILIDAD DE FONDOS" : "TRAMO 01 / MONITOREO DE OPERACIONES",
                Vector2.zero, new Vector2(960, 60), 35, controlsFont, new Color(.38f,.95f,.90f));
            BuildMenu(x);
            var console=Panel(casino?"Consola_Mesa":"Consola_Analisis",new Vector3(x-3.65f,1.95f,1.6f),new Vector2(1.8f,2.3f));
            Box("Base_consola",new Vector3(x-3.65f,.08f,1.6f),new Vector3(.75f,.16f,.55f),dark);
            Shape("Soporte_consola",PrimitiveType.Cylinder,new Vector3(x-3.65f,.45f,1.6f),new Vector3(.18f,.37f,.18f),gold);
            console.rotation=Quaternion.Euler(0,-40,0);Background(console,new Color(.018f,.05f,.065f));
            var col=console.gameObject.AddComponent<BoxCollider>();col.size=new Vector3(1030,1310,14); // physical kiosk, behind its controls
            Label(console,casino?"MESA DE BACARÁ":"CONTROL DE FONDOS",new Vector2(0,550),new Vector2(880,90),56,displayFont,new Color(.94f,.76f,.40f));
            modes.Add(Label(console,"PULSA PLAY",new Vector2(0,452),new Vector2(900,62),34,controlsFont,new Color(.35f,.86f,.81f)));
            objectives.Add(Label(console,"Sigue el recorrido",new Vector2(0,300),new Vector2(860,175),46,font,Color.white));
            counts.Add(Label(console,"",new Vector2(0,70),new Vector2(860,200),35,font,new Color(.80f,.85f,.84f)));
            Label(console,"Mira un control y manten la reticula",new Vector2(0,-550),new Vector2(860,100),29,font,new Color(.63f,.72f,.73f));
            if(casino)
            {
                Control(console,"Colocar_Banca","FICHAS A BANCA",CasinoAction.PlaceBank,new Vector2(0,-190));
                Control(console,"Colocar_Jugador","FICHAS A JUGADOR",CasinoAction.PlacePlayer,new Vector2(0,-340));
                Control(console,"Revelar_Cartas","REVELAR CARTAS",CasinoAction.Deal,new Vector2(0,-190),false);
                Control(console,"Canjear","CANJEAR FICHAS",CasinoAction.CashOut,new Vector2(0,-190),false);
                Control(console,"Final","EXPLICAR EL CASO",CasinoAction.Finish,new Vector2(0,-190),false);
                Control(console,"Explicar_Correcto","OCULTA EL ORIGEN",CasinoAction.ExplainCorrect,new Vector2(0,-190),false);
                Control(console,"Explicar_Error","GANANCIA SEGURA",CasinoAction.ExplainWrong,new Vector2(0,-340),false);
                var compare=Control(console,"Comparar","COMPARAR DEFENSA",CasinoAction.Defense,new Vector2(0,-190),false);compare.onlyWhenComplete=true;
            }
            else
            {
                Control(console,"Convertir","REGISTRAR CAMBIO",CasinoAction.Convert,new Vector2(0,-190));
                Control(console,"Panel_ROS","EMITIR REPORTE",CasinoAction.Report,new Vector2(0,-190),false);
                var retry=Control(console,"Comparar_ataque","COMPARAR ATAQUE",CasinoAction.Attack,new Vector2(0,-190),false);retry.onlyWhenComplete=true;
            }
            BuildPortal(x,casino);BuildTeleport(x,-2.5f,CasinoAction.Approach,"ACERCARSE");BuildTeleport(x,-4.5f,CasinoAction.Overview,"VISTA GENERAL");BuildTeleport(x-2.5f,-.75f,CasinoAction.ToConsole,"CONSOLA");
        }
        ui.counterTexts=counts.ToArray();ui.modeTexts=modes.ToArray();ui.objectiveTexts=objectives.ToArray();
        buildPhase="Tarjeta de lectura";
        BuildInfo(ui);
        string[] captions={"LIBRO","COMPROBANTE","ALERTA"};
        CasinoAction[] ea={CasinoAction.Ledger,CasinoAction.Receipt,CasinoAction.SharedAlert};
        string[] ids={"libro","cambio","alerta"};
        for(int i=0;i<3;i++)
        {
            float x=-2+2*i;var target=ObjectTarget("Pista_"+ids[i],ea[i],captions[i],new Vector3(x,1.5f,.35f),new Vector3(1.1f,.65f,.70f));
            target.gameObject.AddComponent<EvidenceItem>().evidenceId=ids[i];
            var tag=Panel("Etiqueta",new Vector3(x,1.0f,-.1f),new Vector2(1.05f,.19f));
            Label(tag,captions[i],Vector2.zero,new Vector2(950,175),76,controlsFont,Color.white);
        }
        buildPhase="Fichas y cartas";
        var anim=systems.AddComponent<ChipAnimator>();game.chipAnimator=anim;
        anim.bankSpot=Spawn("Destino_Banca",new Vector3(17.9f,1.3f,.20f));anim.playerSpot=Spawn("Destino_Jugador",new Vector3(22.1f,1.3f,.20f));
        var chips=new List<Transform>();
        for(int i=0;i<12;i++)
        {
            var chip=Shape("Ficha_"+i,PrimitiveType.Cylinder,new Vector3(20,1.3f+i*.052f,-.10f),new Vector3(.34f,.022f,.34f),i%2==0?red:white);
            var face=Shape("Centro_ficha",PrimitiveType.Cylinder,chip.transform.position+Vector3.up*.046f,new Vector3(.24f,.002f,.24f),gold);face.transform.SetParent(chip.transform,true);chips.Add(chip.transform);
        }
        anim.chips=chips.ToArray();
        var reveal=systems.AddComponent<CasinoCardReveal>();var roots=new List<Transform>();var fronts=new List<GameObject>();var backs=new List<GameObject>();
        for(int i=0;i<2;i++)
        {
            float xx=i==0?18.7f:21.3f;var holder=new GameObject("Carta_"+i).transform;holder.SetParent(environment,false);holder.position=new Vector3(xx,1.47f,1.02f);
            var front=Panel("Anverso",Vector3.zero,new Vector2(.58f,.83f));front.SetParent(holder,false);front.localPosition=Vector3.zero;
            Picture(front,Root+"/Branding/"+(i==0?"Card7":"Card6")+".png",Vector2.zero,new Vector2(1000,1431));
            var back=Panel("Reverso",Vector3.zero,new Vector2(.58f,.83f));back.SetParent(holder,false);back.localPosition=Vector3.zero;back.localRotation=Quaternion.Euler(0,180,0);
            Picture(back,Root+"/Branding/CardBack.png",Vector2.zero,new Vector2(1000,1431));
            holder.localRotation=Quaternion.Euler(55,180,0);front.gameObject.SetActive(false);roots.Add(holder);fronts.Add(front.gameObject);backs.Add(back.gameObject);
            var name=Panel("Zona",new Vector3(xx,1.255f,-.28f),new Vector2(.9f,.25f));name.rotation=Quaternion.Euler(90,0,0);
            Label(name,i==0?"BANCA":"JUGADOR",Vector2.zero,new Vector2(960,240),100,controlsFont,Color.white);
        }
        reveal.cards=roots.ToArray();reveal.fronts=fronts.ToArray();reveal.backs=backs.ToArray();
        CreateHost(3.9f,.2f,false);CreateHost(23.9f,.2f,true);
        var barrier=new GameObject("Bloqueo_visible");barrier.transform.SetParent(environment,false);
        var stop=Box("Cinta_bloqueo",new Vector3(4.4f,1.45f,2.25f),new Vector3(1.42f,.45f,.09f),red);stop.transform.SetParent(barrier.transform,true);
        var blocked=Panel("Fondos_bloqueados",new Vector3(4.4f,1.45f,2.17f),new Vector2(1.3f,.35f));blocked.SetParent(barrier.transform,true);
        Label(blocked,"FONDOS BLOQUEADOS",Vector2.zero,new Vector2(940,230),78,controlsFont,Color.white);
        systems.AddComponent<CasinoFreezeVisual>().barrier=barrier;
        buildPhase="Sonidos";
        sound.effects = systems.AddComponent<AudioSource>(); sound.effects.playOnAwake = false;
        sound.ambience = systems.AddComponent<AudioSource>(); sound.ambience.loop = true; sound.ambience.volume = 0.22f; sound.ambience.playOnAwake = true;
        sound.selectClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/select.wav");
        sound.alertClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/alert.wav");
        sound.chipsClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/chips.wav");
        sound.endingClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/ending.wav");
        sound.ambience.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/lounge_original.wav");
        sound.paperClip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/paper.wav");
        sound.registerClip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/register.wav");
        var targets=UnityEngine.Object.FindObjectsByType<CasinoInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var guide=systems.AddComponent<CasinoGuide>();guide.targets=targets;
        foreach(var t in targets)
        {
            if(t.uiFace!=null)continue;
            var c=t.GetComponent<Collider>();if(c==null)continue;
            Bounds b=c.bounds;
            var marker=Shape("Siguiente_objeto",PrimitiveType.Sphere,new Vector3(b.center.x,b.max.y+.12f,b.min.z-.07f),Vector3.one*.085f,neonGold);
            marker.transform.SetParent(t.transform,true);marker.SetActive(false);t.guideMarker=marker;
        }
        CardboardPreparacion.Preparar();
        PlayerSettings.productName = "Banco Andino - Casino";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.equipo08.bancoandino.casino");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        PlayerSettings.Android.optimizedFramePacing = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.buildAppBundle = false;
        buildPhase="Comprobacion final";
        game.generationComplete=true;string invalid;
        if(!game.ValidateConfiguration(out invalid)){game.generationComplete=false;throw new InvalidOperationException(invalid);}
        CasinoSceneAudit.Check();game.enabled=true;
        buildPhase="Guardar escena";
        if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new InvalidOperationException("Unity no pudo guardar Casino_VR.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = systems;buildingScene=default(Scene);buildPhase="Completado";
        EditorUtility.DisplayDialog("Casino creado", "Escena Casino_VR lista para editar. Pulsa Play y abre Game. Clic izquierdo: interactuar. Clic derecho + mouse: mirar. WASD: moverse. G: mirada automatica. M: musica. Teclas 1/2: cambiar modo. El APK aun requiere compilacion y prueba en Android.", "OK");
    }
}

[InitializeOnLoad]
public static class CasinoWelcome
{
    static CasinoWelcome() { EditorApplication.delayCall += Show; }
    static void Show()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Show; return; }
        if (SessionState.GetBool("Casino08Welcome", false)) return;
        SessionState.SetBool("Casino08Welcome", true); CasinoProyecto.Open();
    }
}
