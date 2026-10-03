using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using PhaseArena;

public static partial class ArenaDemoBuilder
{
    const string Root="Assets/ArenaDemo";
    static Sprite square, circle, ring, fire, ice, clockSprite, staffSprite;
    static Material spriteMaterial;
    static Color dark=new Color(.055f,.07f,.12f,.96f), gold=new Color(.97f,.8f,.36f), pale=new Color(.88f,.93f,1);
    static TMP_FontAsset font;

    [MenuItem("Tools/Phase Arena/Create Demo Scene")]
    public static void Build()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before building the scene.");
        Directory.CreateDirectory(Root+"/Art"); Directory.CreateDirectory(Root+"/Prefabs");
        Directory.CreateDirectory(Root+"/Materials"); Directory.CreateDirectory("Assets/Scenes");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Art/ArenaFont.asset") ?? TMP_Settings.defaultFontAsset;
        if(font==null) throw new InvalidOperationException("The project's TMP default font is missing.");
        square=Shape("Square",32,(x,y)=>true,new Color(.9f,.95f,1));
        circle=Shape("Circle",32,(x,y)=>x*x+y*y<.9f,Color.white);
        ring=Shape("Ring",64,(x,y)=>x*x+y*y<.95f && x*x+y*y>.73f,Color.white);
        fire=Shape("Fire",32,(x,y)=>x*x+y*y<.65f || (y>.05f && Mathf.Abs(x)<(.9f-y)*.65f),new Color(1,.83f,.48f));
        ice=Shape("Ice",32,(x,y)=>Mathf.Abs(x)+Mathf.Abs(y)<.83f,new Color(.6f,.94f,1));
        clockSprite=Shape("Clock",64,(x,y)=>x*x+y*y<.95f,new Color(.35f,.28f,.18f));
        staffSprite=Shape("Staff",32,(x,y)=>Mathf.Abs(x)<.09f || ((x*x+(y-.52f)*(y-.52f))<.08f),gold);
        spriteMaterial=LoadOrCreateMaterial();
        var normal=PhysicsMaterial("Normal",.1f,.1f); var bouncy=PhysicsMaterial("Frozen",.02f,.85f);
        Sprite[] mage=Strip("Mage","Assets/ArtSources/Female character/PNG/Unarmed_Idle/Unarmed_Idle_full.png",12);
        Sprite[] slime=Strip("Slime","Assets/ArtSources/Slime Mobs Pixel Art Top-Down Sprite Pack/PNG/Slime1/Idle/Slime1_Idle_full.png",6);
        Sprite[] heavy=Strip("HeavySlime","Assets/ArtSources/Slime Mobs Pixel Art Top-Down Sprite Pack/PNG/Slime2/Idle/Slime2_Idle_full.png",6);
        Sprite[] demon=Strip("Demon","Assets/ArtSources/Top-Down Pixel Art Slime Monsters Sprite Pack/PNG/Slime3/Idle/Slime3_Idle_full.png",6);
        Sprite rock=ImportSprite("Rock","Assets/ArtSources/Rocks and Stones Top-Down Pixel Art/PNG/Objects_separately/Rock1_1.png",32);
        Sprite tree=ImportSprite("Tree","Assets/ArtSources/Top-Down Trees Pixel Art/PNG/Assets_separately/Trees_texture_shadow/Tree1.png",64);

        var playerPrefab=BodyPrefab("PlayerMage",BodyKind.Player,mage,2,180,.42f,1.6f,normal);
        var p=playerPrefab.GetComponent<PlayerMage>();
        if(p==null)
        {
            var edit=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(playerPrefab));
            p=edit.AddComponent<PlayerMage>(); p.facing=edit.GetComponent<ThermoBody>().visual;
            var staff=Render("Staff",edit.transform,staffSprite,new Vector2(.3f,0),new Vector2(.6f,1.4f),pale,150);
            p.staff=staff.transform;
            PrefabUtility.SaveAsPrefabAsset(edit,AssetDatabase.GetAssetPath(playerPrefab)); PrefabUtility.UnloadPrefabContents(edit);
            playerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PlayerMage.prefab").GetComponent<ThermoBody>();
        }
        var light=BodyPrefab("LightSlime",BodyKind.Enemy,slime,.8f,44,.37f,1.6f,normal);
        var heavyPrefab=BodyPrefab("HeavySlime",BodyKind.Enemy,heavy,3.2f,95,.62f,2.5f,normal);
        var boss=BodyPrefab("DemonKing",BodyKind.Boss,demon,6,480,1.1f,4.6f,normal);
        var projectile=BodyPrefab("ElementProjectile",BodyKind.Projectile,new[]{fire},1,160,.15f,.42f,normal);
        var shield=ShieldPrefab(normal);
        var pulse=PulsePrefab();

        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        // NewScene may unload assets held only by local variables. Reacquire prefab handles.
        playerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PlayerMage.prefab").GetComponent<ThermoBody>();
        light=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/LightSlime.prefab").GetComponent<ThermoBody>();
        heavyPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/HeavySlime.prefab").GetComponent<ThermoBody>();
        boss=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/DemonKing.prefab").GetComponent<ThermoBody>();
        projectile=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ElementProjectile.prefab").GetComponent<ThermoBody>();
        shield=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/MagicShield.prefab").GetComponent<ThermoBody>();
        pulse=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ReactionPulse.prefab").GetComponent<ArenaPulse>();
        normal=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Root+"/Materials/Normal.physicsMaterial2D");
        bouncy=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Root+"/Materials/Frozen.physicsMaterial2D");
        rock=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Rock.png"); tree=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Tree.png");
        var cameraGO=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)); cameraGO.tag="MainCamera";
        cameraGO.transform.position=new Vector3(0,0,-10);
        var camera=cameraGO.GetComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=8.2f; camera.backgroundColor=new Color(.025f,.04f,.06f); camera.clearFlags=CameraClearFlags.SolidColor;
        var lightGO=new GameObject("Main Light",typeof(Light)); lightGO.GetComponent<Light>().type=LightType.Directional;
        var world=new GameObject("Arena").transform;
        Render("Grass Ground",world,square,Vector2.zero,new Vector2(24,15),new Color(.14f,.23f,.2f),-100);
        var deco=new GameObject("Floor Details").transform; deco.SetParent(world);
        var random=new System.Random(71);
        for(int i=0;i<130;i++)
        {
            float x=(float)random.NextDouble()*22-11,y=(float)random.NextDouble()*13-6.5f;
            Render("Grass "+i,deco,square,new Vector2(x,y),new Vector2(.035f,.12f),new Color(.22f,.34f,.24f,.55f),-95);
        }
        Wall("North",world,new Vector2(0,7.3f),new Vector2(24,.5f)); Wall("South",world,new Vector2(0,-7.3f),new Vector2(24,.5f));
        Wall("West",world,new Vector2(-11.8f,0),new Vector2(.5f,15)); Wall("East",world,new Vector2(11.8f,0),new Vector2(.5f,15));
        var directorGO=new GameObject("ArenaDirector"); var director=directorGO.AddComponent<ArenaDirector>();
        director.entities=new GameObject("Runtime Entities").transform;
        director.lightEnemyPrefab=light; director.heavyEnemyPrefab=heavyPrefab; director.bossPrefab=boss; director.projectilePrefab=projectile; director.shieldPrefab=shield;
        director.pulsePrefab=pulse; director.fireSprite=fire; director.iceSprite=ice; director.normalMaterial=normal; director.bouncyMaterial=bouncy;
        director.player=((GameObject)PrefabUtility.InstantiatePrefab(playerPrefab.gameObject)).GetComponent<ThermoBody>(); director.player.transform.position=new Vector2(-2,-1);
        director.player.driveForce=34; director.player.topSpeed=6;
        var tiles=new System.Collections.Generic.List<TerrainCell>();
        var terrain=new GameObject("Phase Terrain").transform; terrain.SetParent(world);
        foreach(int side in new[]{-1,1}) for(int x=0;x<3;x++) for(int y=0;y<3;y++)
        {
            var go=new GameObject("WaterCell "+side+" "+x+" "+y); go.transform.SetParent(terrain); go.transform.position=new Vector2(side*5+(x-1)*1.02f,(y-1)*1.02f);
            var tile=go.AddComponent<TerrainCell>(); tile.size=new Vector2(1,1); tile.phase=FloorPhase.Water;
            tile.surface=Render("Surface",go.transform,square,Vector2.zero,Vector2.one,new Color(.1f,.4f,.6f),-80);
            var blocker=new GameObject("WaterBlocker",typeof(BoxCollider2D)); blocker.layer=12; blocker.transform.SetParent(go.transform,false); blocker.GetComponent<BoxCollider2D>().size=Vector2.one; tile.waterBlocker=blocker;
            Render("Ice Crack",go.transform,square,new Vector2(0,.05f),new Vector2(.55f,.025f),new Color(.55f,.8f,.9f,.3f),-79);
            tiles.Add(tile);
        }
        var rough=new GameObject("Rough Ground"); rough.transform.SetParent(terrain); rough.transform.position=new Vector2(1.8f,-4);
        var roughTile=rough.AddComponent<TerrainCell>(); roughTile.rough=true; roughTile.size=new Vector2(4,1.6f); roughTile.surface=Render("Rough Surface",rough.transform,square,Vector2.zero,roughTile.size,new Color(.37f,.31f,.22f),-80); tiles.Add(roughTile);
        director.terrain=tiles.ToArray();

        var obstacles=new GameObject("Physical Props").transform; obstacles.SetParent(world);
        var rockPrefab=BodyPrefab("SmallRock",BodyKind.Rock,new[]{rock},.9f,110,.35f,1,normal);
        float[] rockSizes={.65f,.85f,1,1.2f,1.4f,.9f};
        for(int i=0;i<6;i++)
        {
            var r=((GameObject)PrefabUtility.InstantiatePrefab(rockPrefab.gameObject)).GetComponent<ThermoBody>();
            r.transform.SetParent(obstacles); r.transform.position=new Vector2(i%3*3-3,i<3 ? 2.7f : -2.9f);
            float f=rockSizes[i]; r.baseMass=.9f*f*f; r.radius=.35f*f; r.visual.transform.localScale*=f;
            r.GetComponent<CircleCollider2D>().radius=r.radius;
            r.healthBarSize=new Vector2(r.radius*2,.045f); r.healthFill.transform.parent.localPosition=new Vector2(-r.radius,r.radius+.25f);
            var back=r.transform.Find("Health Back"); back.localPosition=new Vector2(0,r.radius+.25f); back.localScale=new Vector3(r.radius*2,.065f,1);
        }
        var large=BodyPrefab("LargeRock",BodyKind.Rock,new[]{rock},12,999,.7f,2,normal);
        var big=((GameObject)PrefabUtility.InstantiatePrefab(large.gameObject)).GetComponent<ThermoBody>(); big.transform.SetParent(obstacles); big.transform.position=new Vector2(8,3); big.indestructible=true; big.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;
        var treePrefab=BodyPrefab("BurnableTree",BodyKind.Tree,new[]{tree},3,55,.48f,1.4f,normal);
        foreach(var pos in new[]{new Vector2(-8,3),new Vector2(-7,-4),new Vector2(7,-4),new Vector2(2,4.5f)}) { var t=(GameObject)PrefabUtility.InstantiatePrefab(treePrefab.gameObject); t.transform.SetParent(obstacles); t.transform.position=pos; }

        var clockGO=new GameObject("World Clock"); clockGO.transform.SetParent(world); director.clock=clockGO.transform;
        Render("Clock Halo",clockGO.transform,ring,Vector2.zero,new Vector2(2.8f,2.8f),new Color(1,.78f,.3f,.25f),-70);
        Render("Clock Face",clockGO.transform,clockSprite,Vector2.zero,new Vector2(1.5f,1.5f),Color.white,-60);
        Render("Clock Rim",clockGO.transform,ring,Vector2.zero,new Vector2(1.55f,1.55f),gold,-59);
        director.clockHand=Render("Clock Hand",clockGO.transform,square,new Vector2(0,.18f),new Vector2(.04f,.55f),gold,-58).transform;
        WorldText("Clock Label",clockGO.transform,"世界钟",new Vector2(0,-1.1f),gold);
        WorldText("Water Label",world,"冰冻通行 / 融冰陷落",new Vector2(-5,-2),new Color(.55f,.86f,1));
        WorldText("Heat Label",world,"火焰改变质量和地形",new Vector2(5,-2),new Color(1,.7f,.4f));
        WorldText("Rough Label",world,"粗糙地面",new Vector2(1.8f,-5.1f),new Color(.88f,.73f,.49f));
        var cross=Render("Aim Cursor",null,ring,Vector2.zero,new Vector2(.3f,.3f),new Color(1,1,1,.7f),400);
        director.hud=BuildHud(); director.hud.hoverCursor=cross.transform;
        if(UnityEngine.Object.FindObjectOfType<EventSystem>()==null) new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        SetLayer(8,"ArenaBody"); SetLayer(9,"ArenaProjectile"); SetLayer(10,"ArenaObstacle"); SetLayer(11,"ArenaShield"); SetLayer(12,"ArenaWater");
        Physics2D.IgnoreLayerCollision(9,9,false); Physics2D.IgnoreLayerCollision(9,12,true); Physics2D.IgnoreLayerCollision(11,11,true);
        Time.fixedDeltaTime=.02f;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/PhaseArenaDemo.unity");
        var scenes=EditorBuildSettings.scenes.Where(s=>s.path!="Assets/Scenes/PhaseArenaDemo.unity").ToList(); scenes.Insert(0,new EditorBuildSettingsScene("Assets/Scenes/PhaseArenaDemo.unity",true)); EditorBuildSettings.scenes=scenes.ToArray();
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=directorGO;
        Debug.Log("[PhaseArena] Demo scene created. Open PhaseArenaDemo and press Play.");
        UpgradeWorldDemo();
    }

    static Sprite Shape(string name,int size,Func<float,float,bool> predicate,Color color)
    {
        string path=Root+"/Art/"+name+".png";
        if(!File.Exists(path))
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            for(int y=0;y<size;y++) for(int x=0;x<size;x++) texture.SetPixel(x,y,predicate((x+.5f)/size*2-1,(y+.5f)/size*2-1) ? color : Color.clear);
            texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=size; importer.filterMode=FilterMode.Point; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite ImportSprite(string name,string source,float ppu)
    {
        string path=Root+"/Art/"+name+".png"; if(!File.Exists(source)) throw new FileNotFoundException(source);
        if(!File.Exists(path)) File.Copy(source,path);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.spritePixelsPerUnit=ppu; importer.filterMode=FilterMode.Point; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite[] Strip(string name,string source,int count)
    {
        ImportSprite(name,source,32);
        string path=Root+"/Art/"+name+".png"; var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.spriteImportMode=SpriteImportMode.Multiple;
        var slices=new SpriteMetaData[count];
        for(int i=0;i<count;i++) slices[i]=new SpriteMetaData { name=name+"_"+i.ToString("00"),rect=new Rect(i*64,texture.height-64,64,64),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f) };
        importer.spritesheet=slices; importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
    }
    static Material LoadOrCreateMaterial()
    {
        string path=Root+"/Materials/Sprites.mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null) { mat=new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(mat,path); } return mat;
    }
    static PhysicsMaterial2D PhysicsMaterial(string name,float friction,float bounce)
    {
        string path=Root+"/Materials/"+name+".physicsMaterial2D"; var mat=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if(mat==null) { mat=new PhysicsMaterial2D(name); AssetDatabase.CreateAsset(mat,path); } mat.friction=friction; mat.bounciness=bounce; return mat;
    }
    static SpriteRenderer Render(string name,Transform parent,Sprite sprite,Vector2 pos,Vector2 scale,Color color,int order)
    {
        var go=new GameObject(name); if(parent!=null) go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=new Vector3(scale.x,scale.y,1);
        var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=sprite; sr.color=color; sr.sortingOrder=order; sr.sharedMaterial=spriteMaterial; return sr;
    }
    static ThermoBody BodyPrefab(string name,BodyKind kind,Sprite[] sprites,float mass,float hp,float radius,float artScale,PhysicsMaterial2D material)
    {
        string path=Root+"/Prefabs/"+name+".prefab";
        var go=new GameObject(name); go.layer=kind==BodyKind.Tree ? 10 : 8;
        var rb=go.AddComponent<Rigidbody2D>(); rb.gravityScale=0; rb.freezeRotation=true; rb.mass=mass; rb.interpolation=RigidbodyInterpolation2D.Interpolate; rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        if(kind==BodyKind.Tree) rb.bodyType=RigidbodyType2D.Static;
        var collider=go.AddComponent<CircleCollider2D>(); collider.radius=radius; collider.sharedMaterial=material;
        var b=go.AddComponent<ThermoBody>(); b.kind=kind; b.baseMass=mass; b.maxHealth=hp; b.health=hp; b.radius=radius;
        b.driveForce=kind==BodyKind.Boss ? 30 : kind==BodyKind.Enemy && mass>2 ? 22 : 11; b.topSpeed=kind==BodyKind.Boss ? 3 : kind==BodyKind.Enemy ? 3.5f : 8;
        b.visual=Render("Visual",go.transform,sprites[0],Vector2.zero,Vector2.one*artScale,Color.white,100);
        if(sprites.Length==1) b.visual.transform.localScale=Vector3.one*(artScale/sprites[0].bounds.size.x);
        b.heatRing=Render("Temperature Halo",go.transform,ring,Vector2.zero,Vector2.one*radius*2.7f,Color.clear,80);
        if(kind!=BodyKind.Projectile)
        {
            Render("Health Back",go.transform,square,new Vector2(0,radius+.25f),new Vector2(radius*2,.065f),new Color(.08f,.05f,.08f),300);
            var pivot=new GameObject("Health Pivot").transform; pivot.SetParent(go.transform,false); pivot.localPosition=new Vector2(-radius,radius+.25f);
            b.healthFill=Render("Health Fill",pivot,square,new Vector2(radius,0),new Vector2(radius*2,.045f),kind==BodyKind.Player ? new Color(.45f,.9f,.6f) : new Color(.95f,.4f,.45f),301);
            b.healthBarSize=new Vector2(radius*2,.045f);
        }
        else b.lifetime=4;
        if(kind==BodyKind.Enemy || kind==BodyKind.Boss) go.AddComponent<EnemyBrain>();
        if(sprites.Length>1) { var a=go.AddComponent<BodySpriteAnimator>(); a.frames=sprites; a.visual=b.visual; a.body=b; }
        var asset=PrefabUtility.SaveAsPrefabAsset(go,path); UnityEngine.Object.DestroyImmediate(go); return asset.GetComponent<ThermoBody>();
    }
    static ThermoBody ShieldPrefab(PhysicsMaterial2D normal)
    {
        var go=new GameObject("MagicShield"); go.layer=11;
        var rb=go.AddComponent<Rigidbody2D>(); rb.bodyType=RigidbodyType2D.Kinematic; rb.gravityScale=0; rb.useFullKinematicContacts=true;
        var col=go.AddComponent<BoxCollider2D>(); col.size=new Vector2(1.7f,.15f); col.sharedMaterial=normal;
        var b=go.AddComponent<ThermoBody>(); b.kind=BodyKind.Shield; b.maxHealth=55; b.health=55; b.baseMass=2; b.lifetime=4;
        b.visual=Render("Shield",go.transform,square,Vector2.zero,new Vector2(1.7f,.15f),new Color(.6f,.4f,1),200); b.baseColor=new Color(.6f,.4f,1);
        var asset=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/MagicShield.prefab"); UnityEngine.Object.DestroyImmediate(go); return asset.GetComponent<ThermoBody>();
    }
    static ArenaPulse PulsePrefab()
    {
        var go=new GameObject("ReactionPulse"); var pulse=go.AddComponent<ArenaPulse>(); pulse.visual=Render("Ring",go.transform,ring,Vector2.zero,Vector2.one,Color.white,220);
        var asset=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/ReactionPulse.prefab"); UnityEngine.Object.DestroyImmediate(go); return asset.GetComponent<ArenaPulse>();
    }
    static void Wall(string name,Transform parent,Vector2 p,Vector2 size)
    {
        var wall=Render(name,parent,square,p,size,new Color(.31f,.34f,.32f),-50).gameObject; wall.layer=10;
        var col=wall.AddComponent<BoxCollider2D>(); col.size=Vector2.one;
    }
    static void SetLayer(int index,string name)
    {
        var tagManager=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]); tagManager.FindProperty("layers").GetArrayElementAtIndex(index).stringValue=name; tagManager.ApplyModifiedProperties();
    }
    static void WorldText(string name,Transform parent,string content,Vector2 p,Color c)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=p;
        var text=go.AddComponent<TextMeshPro>(); text.font=font; text.text=content; text.fontSize=2.5f; text.color=c; text.alignment=TextAlignmentOptions.Center; text.rectTransform.sizeDelta=new Vector2(5,1); text.GetComponent<MeshRenderer>().sortingOrder=350;
    }
    static ArenaHud BuildHud()
    {
        var go=new GameObject("Arena HUD",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1600,900); scaler.matchWidthOrHeight=.5f;
        var hud=go.AddComponent<ArenaHud>(); var root=go.transform;
        var top=Panel("Top Bar",root,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-18),new Vector2(1540,55),dark);
        hud.waveLabel=Label("Wave",top,"PHASE ARENA",new Vector2(0,0),new Vector2(1460,45),25,gold);
        var left=Panel("Player Status",root,new Vector2(0,1),new Vector2(0,1),new Vector2(30,-92),new Vector2(260,245),dark);
        hud.healthLabel=Label("Health",left,"生命",new Vector2(0,86),new Vector2(230,30),22,pale);
        var healthBG=Panel("HP Back",left,Vector2.one*.5f,Vector2.one*.5f,new Vector2(0,53),new Vector2(230,12),new Color(.18f,.22f,.24f));
        var hp=Panel("HP Fill",healthBG,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(230,12),new Color(.35f,.85f,.58f)); hud.healthBar=hp.GetComponent<UnityEngine.UI.Image>(); hud.healthBar.type=UnityEngine.UI.Image.Type.Filled; hud.healthBar.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
        hud.physicalLabel=Label("Physics",left,"温度 / 质量",new Vector2(0,5),new Vector2(230,70),20,new Color(.7f,.85f,.93f));
        Label("Reminder",left,"同一套法则影响所有物体",new Vector2(0,-83),new Vector2(230,42),16,gold);
        EnsureManaHud(hud);
        var laws=Panel("World Laws",root,new Vector2(0,1),new Vector2(0,1),new Vector2(30,-355),new Vector2(260,230),dark);
        hud.lawsLabel=Label("Law List",laws,"世界法则",Vector2.zero,new Vector2(230,200),20,pale); hud.lawsLabel.alignment=TextAlignmentOptions.TopLeft;
        var target=Panel("Target Inspector",root,new Vector2(1,1),new Vector2(1,1),new Vector2(-30,-92),new Vector2(260,180),dark);
        hud.targetLabel=Label("Target",target,"规则提示",Vector2.zero,new Vector2(232,155),18,new Color(.72f,.83f,.86f)); hud.targetLabel.alignment=TextAlignmentOptions.TopLeft;
        var message=Panel("Message",root,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,126),new Vector2(1120,50),dark);
        hud.messageLabel=Label("Prompt",message,"世界钟",Vector2.zero,new Vector2(1080,42),20,gold);
        var skills=Panel("Skill Bar",root,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,22),new Vector2(1330,90),dark);
        hud.skillsLabel=Label("Controls",skills,"控制",Vector2.zero,new Vector2(1280,80),20,pale);

        hud.titlePanel=Overlay("Start Screen",root);
        var title=Panel("Title Card",hud.titlePanel.transform,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(860,490),dark);
        Label("Eyebrow",title,"THERMODYNAMIC ROGUELITE  /  玩法原型",new Vector2(0,180),new Vector2(800,40),19,gold);
        Label("Title",title,"相 变 试 炼",new Vector2(0,105),new Vector2(780,90),55,pale);
        Label("Description",title,"冰与火改变温度，温度改变质量，碰撞引发连锁反应。\n\n三波怪物 → 世界钟三次改写法则 → 击败魔王\n左键火焰 · 右键冰霜 · 空格护盾 · F 法杖\nWASD 移动 · 波间靠近世界钟按 E",new Vector2(0,-35),new Vector2(760,200),23,new Color(.72f,.82f,.86f));
        hud.startButton=Button("Start Trial",title,"开始试炼",new Vector2(0,-185),new Vector2(260,65),gold);

        hud.upgradePanel=Overlay("World Clock Choices",root);
        hud.upgradeTitle=Label("Upgrade Title",hud.upgradePanel.transform,"世界钟",new Vector2(0,240),new Vector2(1400,100),32,gold);
        var cards=new GameObject("Law Cards",typeof(RectTransform),typeof(UnityEngine.UI.HorizontalLayoutGroup)); cards.transform.SetParent(hud.upgradePanel.transform,false);
        var cr=cards.GetComponent<RectTransform>(); cr.sizeDelta=new Vector2(1220,340);
        var layout=cards.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>(); layout.spacing=24; layout.childControlWidth=true; layout.childControlHeight=true; layout.childForceExpandWidth=true; layout.childForceExpandHeight=true;
        hud.choiceButtons=new UnityEngine.UI.Button[3]; hud.cardTitles=new TMP_Text[3]; hud.cardBodies=new TMP_Text[3];
        for(int i=0;i<3;i++)
        {
            var card=Panel("Law Choice "+i,cards.transform,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(390,340),new Color(.1f,.14f,.22f));
            var element=card.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); element.preferredWidth=390; element.preferredHeight=340;
            hud.choiceButtons[i]=card.gameObject.AddComponent<UnityEngine.UI.Button>(); hud.choiceButtons[i].targetGraphic=card.GetComponent<UnityEngine.UI.Image>(); card.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            hud.cardTitles[i]=Label("Law Name",card,"法则",new Vector2(0,112),new Vector2(340,65),30,gold);
            hud.cardBodies[i]=Label("Law Description",card,"说明",new Vector2(0,-15),new Vector2(335,170),21,pale); hud.cardBodies[i].alignment=TextAlignmentOptions.TopLeft;
            Label("Choose",card,"选择此法则 →",new Vector2(0,-135),new Vector2(340,40),21,gold);
        }
        Label("Universal Rule Note",hud.upgradePanel.transform,"升级改变世界规则，玩家的基础属性不随波次提升。",new Vector2(0,-240),new Vector2(1200,50),22,pale);
        hud.resultPanel=Overlay("Result Screen",root);
        var result=Panel("Result Card",hud.resultPanel.transform,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(850,500),dark);
        hud.resultTitle=Label("Result Title",result,"结果",new Vector2(0,160),new Vector2(780,80),44,gold);
        hud.resultBody=Label("Run Statistics",result,"统计",new Vector2(0,-5),new Vector2(760,230),24,pale);
        hud.restartButton=Button("Restart Trial",result,"重新开始",new Vector2(0,-180),new Vector2(260,65),gold);
        hud.pausePanel=Overlay("Pause Screen",root);
        var pause=Panel("Pause Card",hud.pausePanel.transform,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(550,300),dark);
        Label("Pause Title",pause,"世界暂时静止",new Vector2(0,70),new Vector2(500,70),36,gold);
        hud.resumeButton=Button("Resume",pause,"继续试炼",new Vector2(0,-60),new Vector2(260,65),gold);
        hud.upgradePanel.SetActive(false); hud.resultPanel.SetActive(false); hud.pausePanel.SetActive(false);
        return hud;
    }
    static GameObject Overlay(string name,Transform root)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); go.transform.SetParent(root,false);
        var rt=go.GetComponent<RectTransform>(); rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero;
        var img=go.GetComponent<UnityEngine.UI.Image>(); img.color=new Color(.015f,.025f,.045f,.88f); img.raycastTarget=true; return go;
    }
    static RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=anchor; rt.pivot=pivot; rt.anchoredPosition=pos; rt.sizeDelta=size;
        var image=go.GetComponent<UnityEngine.UI.Image>(); image.sprite=square; image.color=color; image.raycastTarget=false; return rt;
    }
    static TMP_Text Label(string name,Transform parent,string content,Vector2 pos,Vector2 size,int fontSize,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.anchoredPosition=pos; rt.sizeDelta=size;
        var text=go.AddComponent<TextMeshProUGUI>(); text.font=font; text.fontSize=fontSize; text.color=color; text.alignment=TextAlignmentOptions.Center; text.text=content; text.raycastTarget=false; text.enableWordWrapping=true; return text;
    }
    static UnityEngine.UI.Button Button(string name,Transform parent,string label,Vector2 pos,Vector2 size,Color color)
    {
        var rt=Panel(name,parent,Vector2.one*.5f,Vector2.one*.5f,pos,size,color); var image=rt.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget=true;
        var button=rt.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=image;
        Label("Label",rt,label,Vector2.zero,size-Vector2.one*10,25,new Color(.08f,.09f,.12f)); return button;
    }
}
