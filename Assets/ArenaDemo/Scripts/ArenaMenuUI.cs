using TMPro;
using UnityEngine;
namespace PhaseArena
{
    public sealed class ArenaMenuUI : MonoBehaviour
    {
        public UnityEngine.UI.Button settingsButton,titleQuitButton,mainMenuButton,pauseQuitButton;
        public UnityEngine.UI.Slider musicSlider,effectsSlider;
        public TMP_Text musicValue,effectsValue,resumeLabel,settingsTitle;
        ArenaHud hud;
        void Awake()
        {
            hud=GetComponent<ArenaHud>();
            settingsButton.onClick.AddListener(OpenSettings); titleQuitButton.onClick.AddListener(QuitGame);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu); pauseQuitButton.onClick.AddListener(QuitGame);
            musicSlider.onValueChanged.AddListener(v=>{if(ArenaAudio.Instance!=null) ArenaAudio.Instance.SetMusicVolume(v); RefreshVolumeLabels();});
            effectsSlider.onValueChanged.AddListener(v=>{if(ArenaAudio.Instance!=null) ArenaAudio.Instance.SetEffectsVolume(v); RefreshVolumeLabels();});
        }
        void Start() { RefreshSliders(); }
        void Update()
        {
            var g=ArenaDirector.Instance; if(g==null) return;
            bool title=g.State==RunState.Title;
            resumeLabel.text=title ? "关闭设置" : "返回游戏";
            mainMenuButton.gameObject.SetActive(!title);
            settingsTitle.text=title ? "设置" : "游戏暂停 / 设置";
        }
        void RefreshSliders()
        {
            var audio=ArenaAudio.Instance; if(audio==null) return;
            musicSlider.SetValueWithoutNotify(audio.MusicVolume); effectsSlider.SetValueWithoutNotify(audio.EffectsVolume); RefreshVolumeLabels();
        }
        void RefreshVolumeLabels() {musicValue.text=Mathf.RoundToInt(musicSlider.value*100)+"%"; effectsValue.text=Mathf.RoundToInt(effectsSlider.value*100)+"%";}
        public void OpenSettings()
        {
            ArenaDirector.Instance.SetPaused(true); RefreshSliders();
            if(ArenaAudio.Instance!=null) ArenaAudio.Instance.StopEffects();
            hud.pausePanel.SetActive(true); hud.pausePanel.transform.SetAsLastSibling();
        }
        public void CloseSettings()
        {
            ArenaDirector.Instance.SetPaused(false); hud.pausePanel.SetActive(false);
            if(ArenaAudio.Instance!=null) ArenaAudio.Instance.SavePreferences();
        }
        public void HandleEscape() {if(ArenaDirector.Instance.Paused) CloseSettings(); else OpenSettings();}
        public void ReturnToMainMenu()
        {
            if(ArenaAudio.Instance!=null) {ArenaAudio.Instance.StopEffects(); ArenaAudio.Instance.SavePreferences();}
            ArenaDirector.Instance.ReturnToTitle(); hud.pausePanel.SetActive(false); hud.titlePanel.SetActive(true);
            if(ArenaAudio.Instance!=null) ArenaAudio.Instance.RefreshMusic();
        }
        public void QuitGame()
        {
            if(ArenaAudio.Instance!=null) ArenaAudio.Instance.SavePreferences();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
