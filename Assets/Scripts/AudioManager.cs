using System;
using UnityEngine;

namespace Crossword3D
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        private AudioSource sfxSource;
        private AudioSource musicSource;

        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1.0f;
        [Range(0f, 1f)] public float musicVolume = 0.35f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        public bool isMuted = false;

        // Procedurally generated clips
        private AudioClip clickClip;
        private AudioClip selectClip;
        private AudioClip correctWordClip;
        private AudioClip errorClip;
        private AudioClip winClip;
        private AudioClip hintClip;
        private AudioClip lampSwitchClip;
        private AudioClip ambientMusicClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupAudioSources();
            GenerateAudioClips();
            PlayAmbientMusic();
        }

        private void SetupAudioSources()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D sound for crisp feedback

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            UpdateMusicVolume();
        }

        private void GenerateAudioClips()
        {
            // Tactile soft wooden click
            clickClip = CreateToneClip("Click", 0.04f, (t) => Mathf.Sin(2 * Mathf.PI * 650f * t) * Mathf.Exp(-t * 65f));
            
            // Pleasant gentle chime for cell select
            selectClip = CreateToneClip("Select", 0.07f, (t) => Mathf.Sin(2 * Mathf.PI * 523.25f * t) * Mathf.Exp(-t * 30f)); // C5
            
            // Subtle, non-jarring low tone for error
            errorClip = CreateToneClip("Error", 0.16f, (t) => 
                (Mathf.Sin(2 * Mathf.PI * 180f * t) * 0.7f + Mathf.Sin(2 * Mathf.PI * 130f * t) * 0.3f) * Mathf.Exp(-t * 14f));
            
            // Magical harp glissando for hints
            hintClip = CreateToneClip("Hint", 0.38f, (t) =>
            {
                float freq = Mathf.Lerp(520f, 1100f, t / 0.38f);
                return Mathf.Sin(2 * Mathf.PI * freq * t) * Mathf.Sin(Mathf.PI * (t / 0.38f)) * 0.5f;
            });

            // Realistic mechanical switch click
            lampSwitchClip = CreateToneClip("LampSwitch", 0.05f, (t) => 
                (Mathf.Sin(2 * Mathf.PI * 320f * t) + (UnityEngine.Random.value * 2f - 1f) * 0.25f) * Mathf.Exp(-t * 45f));

            // Harmonious soft arpeggio for solved word
            correctWordClip = CreateChordClip("WordCorrect", 0.5f, new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }); // C-E-G-C
            
            // Warm celebratory fanfare
            winClip = CreateFanfareClip("WinFanfare", 1.8f);

            // Calming, peaceful, relaxing Rhodes/Acoustic ambient progression
            ambientMusicClip = CreatePeacefulAmbientClip("PeacefulAmbient", 16.0f);
        }

        private AudioClip CreateToneClip(string name, float duration, Func<float, float> waveFunc)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Clamp(waveFunc(t), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateChordClip(string name, float duration, float[] frequencies)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float sum = 0f;
                for (int f = 0; f < frequencies.Length; f++)
                {
                    float noteStart = f * 0.07f;
                    if (t >= noteStart)
                    {
                        float localT = t - noteStart;
                        float envelope = Mathf.Exp(-localT * 5.5f);
                        sum += Mathf.Sin(2 * Mathf.PI * frequencies[f] * localT) * envelope;
                    }
                }
                data[i] = Mathf.Clamp(sum * 0.32f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateFanfareClip(string name, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];

            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f }; // C5, E5, G5, C6, E6
            float[] times = { 0.0f, 0.15f, 0.30f, 0.50f, 0.80f };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float sum = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    if (t >= times[n])
                    {
                        float localT = t - times[n];
                        float env = Mathf.Exp(-localT * (n == notes.Length - 1 ? 2.5f : 6.0f));
                        sum += Mathf.Sin(2 * Mathf.PI * notes[n] * localT) * env;
                    }
                }
                data[i] = Mathf.Clamp(sum * 0.38f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generates a peaceful, tranquil 16-second acoustic/piano chord loop.
        /// Chords: Cmaj9 -> Am9 -> Fmaj7 -> Gsus4 (4s each) with gentle decay and warm harmonics.
        /// </summary>
        private AudioClip CreatePeacefulAmbientClip(string name, float duration)
        {
            int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];

            // 4 peaceful chord voicings (frequencies in Hz)
            float[][] chords = new float[][]
            {
                // Bar 0: Cmaj9 (C3, G3, B3, D4, E4)
                new float[] { 130.81f, 196.00f, 246.94f, 293.66f, 329.63f },
                // Bar 1: Am9 (A2, E3, G3, C4, B3)
                new float[] { 110.00f, 164.81f, 196.00f, 261.63f, 246.94f },
                // Bar 2: Fmaj7 (F2, C3, E3, A3, C4)
                new float[] { 87.31f, 130.81f, 164.81f, 220.00f, 261.63f },
                // Bar 3: Gsus4 / G6 (G2, D3, G3, C4, E4)
                new float[] { 98.00f, 146.83f, 196.00f, 261.63f, 329.63f }
            };

            float barDuration = duration / 4f; // 4 seconds per chord

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                int currentChordIdx = Mathf.FloorToInt(t / barDuration) % 4;
                float chordLocalT = t - (currentChordIdx * barDuration);
                float[] notes = chords[currentChordIdx];

                float chordSample = 0f;

                // Warm bass drone note (first note in chord)
                float bassFreq = notes[0];
                float bassEnv = Mathf.Clamp01(1f - (chordLocalT / barDuration) * 0.45f);
                chordSample += Mathf.Sin(2 * Mathf.PI * bassFreq * t) * 0.28f * bassEnv;

                // Arpeggiated gentle melody notes in the chord
                for (int n = 1; n < notes.Length; n++)
                {
                    float noteOffset = (n - 1) * 0.55f; // staggered note entry
                    if (chordLocalT >= noteOffset)
                    {
                        float localNoteT = chordLocalT - noteOffset;
                        // Soft acoustic envelope with warm second harmonic
                        float env = Mathf.Exp(-localNoteT * 1.4f);
                        float f = notes[n];
                        float tone = Mathf.Sin(2 * Mathf.PI * f * localNoteT) * 0.65f 
                                   + Mathf.Sin(4 * Mathf.PI * f * localNoteT) * 0.22f 
                                   + Mathf.Sin(6 * Mathf.PI * f * localNoteT) * 0.08f;
                        chordSample += tone * env * 0.16f;
                    }
                }

                // Seamless loop fade envelope at boundaries
                float loopProgress = t / duration;
                float edgeFade = Mathf.Sin(Mathf.PI * loopProgress);
                data[i] = Mathf.Clamp(chordSample * (0.85f + 0.15f * edgeFade), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void PlayClick() => PlaySFX(clickClip);
        public void PlaySelect() => PlaySFX(selectClip);
        public void PlayCorrectWord() => PlaySFX(correctWordClip);
        public void PlayError() => PlaySFX(errorClip);
        public void PlayWin() => PlaySFX(winClip);
        public void PlayHint() => PlaySFX(hintClip);
        public void PlayLampSwitch() => PlaySFX(lampSwitchClip);

        private void PlaySFX(AudioClip clip)
        {
            if (isMuted || clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }

        public void PlayAmbientMusic()
        {
            if (musicSource == null || ambientMusicClip == null) return;
            musicSource.clip = ambientMusicClip;
            UpdateMusicVolume();
            musicSource.Play();
        }

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            UpdateMusicVolume();
        }

        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            UpdateMusicVolume();
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
        }

        public void SetMuted(bool mute)
        {
            isMuted = mute;
            UpdateMusicVolume();
        }

        public void ToggleMute()
        {
            SetMuted(!isMuted);
        }

        private void UpdateMusicVolume()
        {
            if (musicSource != null)
            {
                musicSource.volume = isMuted ? 0f : (musicVolume * masterVolume);
            }
        }
    }
}
