using UnityEngine;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio/Library")]
public class AudioLibrary : ScriptableObject
{
    [Header("M�sica de fondo (BGM)")]
    public AudioClip[] backgroundMusicClips;

    [Header("Efectos de sonido (SFX)")]
    public AudioClip[] fxSounds;
}
