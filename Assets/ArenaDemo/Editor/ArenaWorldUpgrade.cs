using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PhaseArena;

public static partial class ArenaDemoBuilder
{
    [MenuItem("Tools/Phase Arena/Upgrade World Laws Demo")]
    public static void UpgradeWorldDemo()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before upgrading.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/PhaseArenaDemo.unity") scene=EditorSceneManager.OpenScene("Assets/Scenes/PhaseArenaDemo.unity");
        var director=UnityEngine.Object.FindObjectOfType<ArenaDirector>();
        if(director==null) { Build(); return; }
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Art/ArenaFont.asset");
        square=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Square.png"); ring=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Ring.png");
        spriteMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Sprites.mat");
        var normal=PhysicsMaterial("Normal",.1f,.1f); var bouncy=PhysicsMaterial("Frozen",.01f,.95f);
        string tuningPath=Root+"/WorldTuning.asset";
        var tuning=AssetDatabase.LoadAssetAtPath<ArenaTuning>(tuningPath);
        if(tuning==null) { tuning=ScriptableObject.CreateInstance<ArenaTuning>(); AssetDatabase.CreateAsset(tuning,tuningPath); }
        director.tuning=tuning; director.randomSeed=0; director.normalMaterial=normal; director.bouncyMaterial=bouncy; director.feedbackFont=font;
        var medium=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/MediumSlime.prefab");
        var guard=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ClockGuard.prefab");
        var frames=AssetDatabase.LoadAllAssetsAtPath(Root+"/Art/HeavySlime.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        if(medium==null) medium=BodyPrefab("MediumSlime",BodyKind.Enemy,frames,1.8f,80,.48f,2,normal).gameObject;
        if(guard==null) guard=BodyPrefab("ClockGuard",BodyKind.Enemy,frames,4,tuning.guardHealth,.64f,2.5f,normal).gameObject;
        TunePrefab("LightSlime",b=> { b.baseMass=.8f; b.maxHealth=tuning.lightHealth; b.driveForce=7; b.topSpeed=2.4f; });
        TunePrefab("MediumSlime",b=> { b.baseMass=1.8f; b.maxHealth=tuning.mediumHealth; b.driveForce=16; b.topSpeed=2.2f; });
        TunePrefab("HeavySlime",b=> { b.baseMass=4; b.maxHealth=tuning.heavyHealth; b.driveForce=34; b.topSpeed=2; });
        TunePrefab("ClockGuard",b=> { b.baseMass=4; b.maxHealth=tuning.guardHealth; b.driveForce=30; b.topSpeed=2; b.isElite=true; });
        TunePrefab("DemonKing",b=> { b.baseMass=6; b.maxHealth=tuning.bossHealth; b.driveForce=40; b.topSpeed=2; });
        TunePrefab("SmallRock",b=> { b.baseMass=1.05f; b.maxHealth=tuning.rockHealth; b.driveForce=0; b.topSpeed=tuning.rockSpeedLimit; b.indestructible=false; });
        TunePrefab("LargeRock",b=> { b.baseMass=12; b.indestructible=true; b.gameObject.layer=10; b.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static; });
        TunePrefab("BurnableTree",b=> { b.baseMass=3; b.maxHealth=55; b.indestructible=false; b.gameObject.layer=10; b.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static; });
        TunePrefab("ElementProjectile",b=> { b.baseMass=tuning.spellMass; b.maxHealth=tuning.projectileHealth; b.lifetime=0; b.gameObject.layer=8; });
        TunePrefab("MagicShield",b=> { b.baseMass=2; b.maxHealth=55; b.lifetime=tuning.shieldLifetime; if(b.GetComponent<ShieldFollower>()==null) b.gameObject.AddComponent<ShieldFollower>(); });
        TunePrefab("PlayerMage",b=> { b.baseMass=2; b.maxHealth=tuning.playerHealth; b.driveForce=38; b.topSpeed=4.5f; });
        director.mediumEnemyPrefab=LoadBody("MediumSlime"); director.guardPrefab=LoadBody("ClockGuard");
        director.lightEnemyPrefab=LoadBody("LightSlime"); director.heavyEnemyPrefab=LoadBody("HeavySlime"); director.bossPrefab=LoadBody("DemonKing");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/RangedSlime.prefab")==null)
            AssetDatabase.CopyAsset(Root+"/Prefabs/MediumSlime.prefab",Root+"/Prefabs/RangedSlime.prefab");
        TunePrefab("RangedSlime",b=> { b.GetComponent<EnemyBrain>().ranged=true; b.baseColor=new Color(.65f,.4f,.85f); b.visual.color=b.baseColor; });
        director.rangedEnemyPrefab=LoadBody("RangedSlime");
        director.projectilePrefab=LoadBody("ElementProjectile"); director.shieldPrefab=LoadBody("MagicShield");
        string shardPath=Root+"/Prefabs/ClockFragment.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(shardPath)==null)
        {
            var go=new GameObject("ClockFragment",typeof(ShardPickup));
            var pickup=go.GetComponent<ShardPickup>(); pickup.visual=Render("Fragment",go.transform,AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Ice.png"),Vector2.zero,Vector2.one*.55f,gold,650);
            Render("Glow",go.transform,ring,Vector2.zero,Vector2.one,new Color(1,.85f,.2f,.65f),640);
            PrefabUtility.SaveAsPrefabAsset(go,shardPath); UnityEngine.Object.DestroyImmediate(go);
        }
        director.shardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(shardPath).GetComponent<ShardPickup>();
        var arena=GameObject.Find("Arena");
        foreach(string name in new[]{"Grass Ground","Floor Details","North","South","West","East","Phase Terrain","Physical Props","Water Label","Heat Label","Rough Label"})
        {
            var old=arena.transform.Find(name); if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        var generator=arena.GetComponent<WorldGenerator>(); if(generator==null) generator=arena.AddComponent<WorldGenerator>();
        generator.tuning=tuning; generator.square=square; generator.ring=ring; generator.spriteMaterial=spriteMaterial; generator.normalMaterial=normal;
        generator.rockPrefab=LoadBody("SmallRock"); generator.largeRockPrefab=LoadBody("LargeRock"); generator.treePrefab=LoadBody("BurnableTree");
        director.worldGenerator=generator;
        director.clock.position=WorldLayout.ClockAt(tuning);
        generator.Generate(104729,WorldLayout.SpawnAt(tuning)); director.terrain=generator.cells.Values.ToArray();
        director.player.baseMass=2; director.player.maxHealth=tuning.playerHealth; director.player.health=tuning.playerHealth; director.player.driveForce=38; director.player.topSpeed=4.5f;
        director.player.transform.position=WorldLayout.SpawnAt(tuning);
        Physics2D.maxTranslationSpeed=Mathf.Max(tuning.rockSpeedLimit,tuning.spellSpeedLimit,tuning.actorSpeedLimit);
        var camera=Camera.main; var follow=camera.GetComponent<ArenaCameraFollow>(); if(follow==null) follow=camera.gameObject.AddComponent<ArenaCameraFollow>();
        follow.target=director.player.transform; follow.tuning=tuning; camera.orthographicSize=8.2f; follow.Snap();
        UpdateWorldHud(director.hud);
        BakeWorldFont();
        EditorUtility.SetDirty(director); EditorUtility.SetDirty(generator); EditorUtility.SetDirty(tuning); EditorUtility.SetDirty(director.hud);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Selection.activeGameObject=director.gameObject;
        Debug.Log("[PhaseArena] World laws demo saved with eight worlds, seeded map, fragments, cardinal shields and minimap.");
    }
    static ThermoBody LoadBody(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab").GetComponent<ThermoBody>();
    static void TunePrefab(string name,Action<ThermoBody> tune)
    {
        string path=Root+"/Prefabs/"+name+".prefab"; var go=PrefabUtility.LoadPrefabContents(path); var b=go.GetComponent<ThermoBody>();
        tune(b); b.health=b.maxHealth; go.GetComponent<Rigidbody2D>().mass=Mathf.Max(.0001f,b.baseMass);
        PrefabUtility.SaveAsPrefabAsset(go,path); PrefabUtility.UnloadPrefabContents(go);
    }
    public static void EnsureManaHud(ArenaHud hud)
    {
        var status=hud.healthLabel.transform.parent;
        if(hud.manaLabel==null)
        {
            var go=UnityEngine.Object.Instantiate(hud.healthLabel.gameObject,status);
            go.name="Mana"; hud.manaLabel=go.GetComponent<TMP_Text>();
        }
        if(hud.manaBar==null)
        {
            var back=UnityEngine.Object.Instantiate(status.Find("HP Back").gameObject,status);
            back.name="MP Back";
            back.GetComponent<UnityEngine.UI.Image>().color=new Color(.08f,.15f,.24f);
            hud.manaBar=back.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>();
            hud.manaBar.name="MP Fill";
        }
        hud.healthLabel.rectTransform.anchoredPosition=new Vector2(0,96);
        status.Find("HP Back").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,72);
        hud.manaLabel.rectTransform.anchoredPosition=new Vector2(0,49);
        hud.manaLabel.fontSize=18; hud.manaLabel.text="法力 200 / 200";
        hud.manaLabel.color=new Color(.65f,.85f,1);
        status.Find("MP Back").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,27);
        hud.manaBar.color=new Color(.18f,.48f,1);
        hud.manaBar.type=UnityEngine.UI.Image.Type.Filled;
        hud.manaBar.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
        hud.manaBar.fillOrigin=0; hud.manaBar.fillAmount=1;
        hud.physicalLabel.rectTransform.anchoredPosition=new Vector2(0,-28);
        status.Find("Reminder").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-97);
        EditorUtility.SetDirty(hud);
    }
    static void UpdateWorldHud(ArenaHud hud)
    {
        EnsureManaHud(hud);
        hud.physicalLabel.rectTransform.sizeDelta=new Vector2(230,90);
        var root=hud.transform;
        var target=root.Find("Target Inspector").GetComponent<RectTransform>(); target.anchoredPosition=new Vector2(-30,-375); target.sizeDelta=new Vector2(260,180);
        var laws=root.Find("World Laws").GetComponent<RectTransform>(); laws.sizeDelta=new Vector2(260,330);
        hud.lawsLabel.rectTransform.sizeDelta=new Vector2(232,300); hud.lawsLabel.fontSize=18;
        var desc=root.Find("Start Screen/Title Card/Description").GetComponent<TMP_Text>();
        desc.text="从左向右探索，击败守卫并拾取两枚碎片。\n到右侧世界钟选法则，进入下一周目。\n\n开局法杖钝击能打倒怪物；之后双方血量增长。\n冰火准备条件，环境与新法则放大伤害。\n七次改写后，在第八周目击败魔王。";
        desc.fontSize=22;
        root.Find("World Clock Choices/Universal Rule Note").GetComponent<TMP_Text>().text="法则跨周目累计。每条法则共同影响玩家、怪物、弹丸与环境。";
        for(int i=0;i<hud.cardBodies.Length;i++) { hud.cardBodies[i].fontSize=20; hud.cardBodies[i].rectTransform.sizeDelta=new Vector2(335,180); hud.cardBodies[i].rectTransform.anchoredPosition=new Vector2(0,-20); }
        hud.resultBody.fontSize=20; hud.resultBody.rectTransform.sizeDelta=new Vector2(760,250);
        if(hud.minimap==null)
        {
            var panel=Panel("World Map",root,new Vector2(1,1),new Vector2(1,1),new Vector2(-30,-92),new Vector2(260,260),dark);
            panel.SetSiblingIndex(hud.titlePanel.transform.GetSiblingIndex());
            Label("Map Title",panel,"世界地图 · 金色菱形为碎片",new Vector2(0,110),new Vector2(245,26),16,gold);
            var raw=new GameObject("Map Surface",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));
            raw.transform.SetParent(panel,false); var rt=raw.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(240,186); rt.anchoredPosition=new Vector2(0,-1);
            raw.GetComponent<UnityEngine.UI.RawImage>().raycastTarget=false;
            var map=panel.gameObject.AddComponent<ArenaMinimap>(); map.image=raw.GetComponent<UnityEngine.UI.RawImage>();
            map.playerMarker=Panel("Player Marker",rt,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,Vector2.one*14,new Color(.45f,1,.75f));
            map.playerMarker.GetComponent<UnityEngine.UI.Image>().sprite=ring;
            Panel("Player Dot",map.playerMarker,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,Vector2.one*5,new Color(.75f,1,.85f));
            map.clockMarker=Panel("Clock Marker",rt,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,Vector2.one*8,gold);
            map.spawnMarker=Panel("Spawn Marker",rt,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,Vector2.one*8,new Color(.3f,.7f,.8f));
            map.playerMarker.SetAsLastSibling(); hud.minimap=map;
            Label("Legend",panel,"绿色：你   蓝色：出生点   红色：威胁",new Vector2(0,-115),new Vector2(250,27),13,pale);
        }
        if(hud.reactionLabel==null) hud.reactionLabel=Label("Reaction Chain",root,"",new Vector2(0,190),new Vector2(950,60),29,gold);
        hud.minimap.transform.SetSiblingIndex(hud.titlePanel.transform.GetSiblingIndex());
        hud.minimap.image.color=new Color(.18f,.27f,.2f);
    }
    [MenuItem("Tools/Phase Arena/Bake World UI Font")]
    public static void BakeWorldFont()
    {
        var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Art/ArenaFont.asset");
        var chars=new HashSet<char>();
        for(int c=32;c<=126;c++) chars.Add((char)c);
        foreach(var file in Directory.GetFiles(Root,"*.cs",SearchOption.AllDirectories))
            foreach(char c in File.ReadAllText(file))
                if((c>='\u4e00' && c<='\u9fff') || (c>='\u3000' && c<='\u303f') || (c>='\uff00' && c<='\uffef') || "°◆·→×".Contains(c.ToString())) chars.Add(c);
        string text=new string(chars.OrderBy(c=>c).ToArray());
        asset.atlasPopulationMode=AtlasPopulationMode.Dynamic; asset.ClearFontAssetData(true);
        var serialized=new SerializedObject(asset); serialized.FindProperty("m_AtlasWidth").intValue=2048; serialized.FindProperty("m_AtlasHeight").intValue=2048; serialized.ApplyModifiedProperties();
        asset.isMultiAtlasTexturesEnabled=false;
        string missing; bool added=asset.TryAddCharacters(text,out missing);
        asset.atlasPopulationMode=AtlasPopulationMode.Static;
        EditorUtility.SetDirty(asset); foreach(var texture in asset.atlasTextures) if(texture!=null) EditorUtility.SetDirty(texture);
        if(asset.material!=null) EditorUtility.SetDirty(asset.material);
        AssetDatabase.SaveAssets();
        if(!added) throw new InvalidOperationException("UI font missing glyphs: "+missing);
        Debug.Log("[PhaseArena] Static UI font baked: "+chars.Count+" glyphs.");
    }
}
