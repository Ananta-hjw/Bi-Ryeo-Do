using System.Collections.Generic;
using BiRyeoDo.Core;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>
    /// Optional audio, matched by file name like <see cref="ArtLibrary"/>: <c>Resources/Audio/Sfx/&lt;name&gt;.ogg</c> and
    /// <c>Resources/Audio/Bgm/&lt;name&gt;.ogg</c> (wav/mp3 work too). A missing file is silently skipped, so sounds can be added one by one.
    /// Names used by the game are listed in Docs/AUDIO_GUIDE.md. Volumes and vibration come from <see cref="GameSettings"/>.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager _instance;
        private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        private AudioSource _bgm, _sfx;
        private string _currentBgm = "";

        private static GameSettings Settings
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null || gm.Save == null) return new GameSettings();
                if (gm.Save.Settings == null) gm.Save.Settings = new GameSettings();
                return gm.Save.Settings;
            }
        }

        private void Awake()
        {
            if (_instance != null) { Destroy(this); return; }
            _instance = this;
            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.loop = true;
            _bgm.playOnAwake = false;
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
        }

        private AudioClip Load(string folder, string name)
        {
            string key = folder + "/" + name;
            AudioClip clip;
            if (_cache.TryGetValue(key, out clip)) return clip;
            clip = Resources.Load<AudioClip>("Audio/" + key);
            _cache[key] = clip;
            return clip;
        }

        /// <summary>Plays a short effect once (same name at most every 40 ms so multi-hit moves do not stack into noise).</summary>
        public static void Sfx(string name)
        {
            if (_instance == null) return;
            var clip = _instance.Load("Sfx", name);
            if (clip == null) return;
            float now = Time.unscaledTime, last;
            if (_instance._lastPlayed.TryGetValue(name, out last) && now - last < 0.04f) return;
            _instance._lastPlayed[name] = now;
            _instance._sfx.PlayOneShot(clip, Settings.SfxVolume);
        }

        /// <summary>Starts looping background music (no restart if it is already playing).</summary>
        public static void Bgm(string name)
        {
            if (_instance == null || _instance._currentBgm == name) return;
            _instance._currentBgm = name;
            var clip = _instance.Load("Bgm", name);
            _instance._bgm.Stop();
            if (clip == null) return;
            _instance._bgm.clip = clip;
            _instance._bgm.volume = Settings.BgmVolume;
            _instance._bgm.Play();
        }

        /// <summary>Re-applies the saved volumes (called after the options change).</summary>
        public static void RefreshVolumes()
        {
            if (_instance == null) return;
            _instance._bgm.volume = Settings.BgmVolume;
        }

        /// <summary>Short vibration; does nothing when switched off or outside a device.</summary>
        public static void Haptic(int milliseconds)
        {
            if (!Settings.Vibration) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (System.Exception)
            {
                Handheld.Vibrate();   // fallback (fixed length); also makes Unity add the VIBRATE permission
            }
#endif
        }
    }
}
