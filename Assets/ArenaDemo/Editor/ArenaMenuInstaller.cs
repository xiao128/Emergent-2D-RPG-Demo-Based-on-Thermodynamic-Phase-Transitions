using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PhaseArena
{
    // Authors only the menu/audio additions; leaves user-authored gameplay tuning intact.
    public static class ArenaMenuInstaller
    {
        static TMP_FontAsset font;
        static readonly Color Gold=new Color(.9f,.75f,.42f);
        static readonly Color Ink=new Color(.025f,.075f,.095f,.94f);
        [MenuItem("Tools/Phase Arena/Install Main Menu and Audio")]
        public static void Install()
        {
            if(Application.isPlaying) throw new System.InvalidOperationException("请先停止运行模式。");
            var g=Object.FindObjectOfType<ArenaDirector>(); var h=g.hud;
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ArenaDemo/Art/ArenaFont.asset");
            var menu=h.GetComponent<ArenaMenuUI>(); if(menu==null) menu=h.gameObject.AddComponent<ArenaMenuUI>();
            // Keep the existing HUD button references while replacing their surrounding cards.
            h.startButton.transform.SetParent(h.titlePanel.transform,false);
            var old=h.titlePanel.transform.Find("Title Card"); if(old!=null) Object.DestroyImmediate(old.gameObject);
            Remove(h.titlePanel.transform,"Menu Art"); Remove(h.titlePanel.transform,"Menu Content");
            h.titlePanel.GetComponent<Image>().color=Color.black;
            var art=Rect("Menu Art",h.titlePanel.transform,Vector2.zero,Vector2.zero); Stretch(art);
            var raw=art.gameObject.AddComponent<RawImage>();
            raw.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ArenaDemo/Art/MainMenuBackground.png"); raw.raycastTarget=false;
            var aspect=art.gameObject.AddComponent<AspectRatioFitter>(); aspect.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio=raw.texture!=null ? (float)raw.texture.width/raw.texture.height : 16f/9;
            art.SetAsFirstSibling();
            var content=Rect("Menu Content",h.titlePanel.transform,Vector2.zero,new Vector2(900,900));
            content.gameObject.AddComponent<ArenaMenuFit>();
            Label("Title",content,"<color=#A5EAFF>PHASE</color> <color=#F2CE79>ARENA</color>",new Vector2(0,28),new Vector2(900,115),84);
            Label("Subtitle",content,"世 界 法 则 的 碎 片",new Vector2(0,-48),new Vector2(700,50),26,Gold);
            var line=Panel("Title Rule",content,new Vector2(0,-79),new Vector2(260,2),new Color(.9f,.75f,.42f,.65f));
            h.startButton.transform.SetParent(content,false); StyleButton(h.startButton,"开始游戏",new Vector2(0,-146),new Vector2(360,68),true);
            menu.settingsButton=Button("Settings",content,"设置",new Vector2(0,-235),new Vector2(360,68));
            menu.titleQuitButton=Button("Quit",content,"退出游戏",new Vector2(0,-324),new Vector2(360,68));
            Label("Footer",content,"冰与火改变温度  ·  推动物体改写法则",new Vector2(0,-424),new Vector2(800,30),18,new Color(.65f,.81f,.82f,.85f));

            var card=h.pausePanel.transform.Find("Pause Card") as RectTransform;
            h.resumeButton.transform.SetParent(h.pausePanel.transform,false);
            if(card!=null) Object.DestroyImmediate(card.gameObject);
            card=Panel("Pause Card",h.pausePanel.transform,Vector2.zero,new Vector2(640,650),Ink);
            card.gameObject.AddComponent<ArenaMenuFit>().authoredSize=new Vector2(640,650);
            Border(card,Gold);
            menu.settingsTitle=Label("Pause Title",card,"游戏暂停 / 设置",new Vector2(0,250),new Vector2(590,65),36,Gold);
            Label("Music Label",card,"音乐音量",new Vector2(-175,140),new Vector2(220,40),26);
            menu.musicValue=Label("Music Value",card,"65%",new Vector2(225,140),new Vector2(90,40),24,Gold);
            menu.musicSlider=Slider("Music Volume",card,new Vector2(0,92));
            Label("Effects Label",card,"音效音量",new Vector2(-175,22),new Vector2(220,40),26);
            menu.effectsValue=Label("Effects Value",card,"80%",new Vector2(225,22),new Vector2(90,40),24,Gold);
            menu.effectsSlider=Slider("Effects Volume",card,new Vector2(0,-26));
            Panel("Settings Rule",card,new Vector2(0,-77),new Vector2(510,1),new Color(.9f,.75f,.42f,.35f));
            h.resumeButton.transform.SetParent(card,false); StyleButton(h.resumeButton,"返回游戏",new Vector2(0,-132),new Vector2(420,60),true);
            menu.resumeLabel=h.resumeButton.GetComponentInChildren<TMP_Text>();
            menu.mainMenuButton=Button("Return Main Menu",card,"返回主菜单",new Vector2(0,-210),new Vector2(420,60));
            menu.pauseQuitButton=Button("Quit Game",card,"退出游戏",new Vector2(0,-288),new Vector2(420,60));
            h.pausePanel.GetComponent<Image>().color=new Color(.01f,.025f,.035f,.82f);
            h.pausePanel.transform.SetAsLastSibling(); h.pausePanel.SetActive(false); h.titlePanel.SetActive(true);
            h.upgradePanel.SetActive(false); h.resultPanel.SetActive(false);
            foreach(var button in h.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                if(button.GetComponent<ArenaUiSound>()==null) button.gameObject.AddComponent<ArenaUiSound>();

            var audio=Object.FindObjectOfType<ArenaAudio>();
            if(audio==null) audio=new GameObject("Arena Audio").AddComponent<ArenaAudio>();
            audio.menuMusic=Music("StartSceneMusic"); audio.normalMusic=Music("NormalMusic"); audio.bossMusic=Music("BossMusic");
            audio.uiClick=Effect("鼠标点击2-xys20070412.wav"); audio.uiHover=Effect("鼠标点击1-xys20070412.wav");
            audio.staffHit=Effect("F打击.wav"); audio.playerHit=Effect("拍打2.wav"); audio.enemyShot=Effect("敌人发射法球.wav"); audio.slimeDeath=Effect("史莱姆死亡.wav");
            audio.lightCharge=Effect("小史莱姆冲撞.wav"); audio.mediumCharge=Effect("中史莱姆冲撞.ogg"); audio.heavyCharge=Effect("大史莱姆冲撞.ogg");
            audio.stoneImpact=Effect("土石类击中－mcx20070509.wav"); audio.waterImpact=Effect("mud_06.ogg");
            audio.grassFootstep=Effect("脚步-草地.wav"); audio.iceFootstep=Effect("脚步-冰上.wav");
            if(g.player.GetComponent<ArenaFootsteps>()==null) g.player.gameObject.AddComponent<ArenaFootsteps>();
            var importer=AssetImporter.GetAtPath("Assets/ArenaDemo/Art/MainMenuBackground.png") as TextureImporter;
            if(importer!=null) { importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.maxTextureSize=2048; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport(); }
            EditorUtility.SetDirty(audio); EditorUtility.SetDirty(menu); EditorUtility.SetDirty(h);
            ArenaDemoBuilder.BakeWorldFont();
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(g.gameObject.scene); EditorSceneManager.SaveScene(g.gameObject.scene);
            Selection.activeGameObject=h.titlePanel;
            Debug.Log("[PhaseArena] Main menu and audio installed; scene saved.");
        }
        static AudioClip Music(string name)
        {
            string path="Assets/Music/"+name+".mp3";
            var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            if(importer!=null)
            {
                var settings=importer.defaultSampleSettings; settings.loadType=AudioClipLoadType.Streaming; settings.preloadAudioData=false;
                importer.defaultSampleSettings=settings; importer.loadInBackground=true; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static AudioClip Effect(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SoundsEffect/"+name);
        static void Remove(Transform parent,string name) { var t=parent.Find(name); if(t!=null) Object.DestroyImmediate(t.gameObject); }
        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=position; r.sizeDelta=size; return r;
        }
        static void Stretch(RectTransform r) {r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero;}
        static RectTransform Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        {
            var r=Rect(name,parent,position,size); var image=r.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false; return r;
        }
        static TMP_Text Label(string name,Transform parent,string value,Vector2 position,Vector2 size,float points,Color? color=null)
        {
            var r=Rect(name,parent,position,size); var label=r.gameObject.AddComponent<TextMeshProUGUI>();
            label.font=font; label.text=value; label.fontSize=points; label.color=color ?? new Color(.92f,.93f,.85f);
            label.alignment=TextAlignmentOptions.Center; label.raycastTarget=false; label.fontStyle=FontStyles.Bold;
            return label;
        }
        static void Border(RectTransform parent,Color color)
        {
            var s=parent.sizeDelta;
            Panel("Top Frame",parent,new Vector2(0,s.y/2),new Vector2(s.x,2),color);
            Panel("Bottom Frame",parent,new Vector2(0,-s.y/2),new Vector2(s.x,2),color);
            Panel("Left Frame",parent,new Vector2(-s.x/2,0),new Vector2(2,s.y),color);
            Panel("Right Frame",parent,new Vector2(s.x/2,0),new Vector2(2,s.y),color);
        }
        static UnityEngine.UI.Button Button(string name,Transform parent,string value,Vector2 position,Vector2 size)
        {
            var r=Rect(name,parent,position,size); r.gameObject.AddComponent<Image>(); var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();
            StyleButton(b,value,position,size,false); return b;
        }
        static void StyleButton(UnityEngine.UI.Button button,string value,Vector2 position,Vector2 size,bool primary)
        {
            var r=(RectTransform)button.transform; r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=position; r.sizeDelta=size;
            while(r.childCount>0) Object.DestroyImmediate(r.GetChild(0).gameObject);
            var image=button.GetComponent<Image>(); image.color=Color.white; image.raycastTarget=true; button.targetGraphic=image;
            var colors=button.colors; colors.normalColor=primary ? new Color(.08f,.14f,.12f,.95f) : new Color(.02f,.085f,.1f,.86f);
            colors.highlightedColor=new Color(.24f,.29f,.19f,.96f); colors.selectedColor=colors.highlightedColor; colors.pressedColor=new Color(.38f,.29f,.13f,.96f); colors.fadeDuration=.12f; button.colors=colors;
            Border(r,primary ? Gold : new Color(.6f,.64f,.47f,.65f));
            Label("Label",r,value,Vector2.zero,size,30,primary ? new Color(1,.9f,.65f) : new Color(.84f,.87f,.79f));
            foreach(float sign in new[]{-1f,1f})
            {
                var diamond=Panel("Diamond",r,new Vector2(sign*(size.x/2-20),0),new Vector2(7,7),Gold); diamond.localRotation=Quaternion.Euler(0,0,45);
            }
        }
        static UnityEngine.UI.Slider Slider(string name,Transform parent,Vector2 position)
        {
            var r=Rect(name,parent,position,new Vector2(500,38)); var slider=r.gameObject.AddComponent<UnityEngine.UI.Slider>();
            Panel("Background",r,Vector2.zero,new Vector2(500,8),new Color(.16f,.24f,.25f));
            var area=Rect("Fill Area",r,Vector2.zero,new Vector2(480,8));
            var fill=Panel("Fill",area,Vector2.zero,new Vector2(480,8),Gold); Stretch(fill); slider.fillRect=fill;
            var handles=Rect("Handle Area",r,Vector2.zero,new Vector2(480,38));
            var handle=Panel("Handle",handles,Vector2.zero,new Vector2(20,30),new Color(1,.91f,.7f));
            handle.GetComponent<Image>().raycastTarget=true; slider.handleRect=handle; slider.targetGraphic=handle.GetComponent<Image>();
            slider.minValue=0; slider.maxValue=1; slider.value=.65f;
            // The full row receives pointer events, including clicks away from the handle.
            var hit=r.gameObject.AddComponent<Image>(); hit.color=Color.clear; hit.raycastTarget=true;
            return slider;
        }
    }
}
