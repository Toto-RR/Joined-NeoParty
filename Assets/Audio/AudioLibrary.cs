using UnityEngine;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio/Library")]
public class AudioLibrary : ScriptableObject
{
    [Header("Música de fondo (BGM)")]
    public AudioClip[] backgroundMusicClips;

    [Header("Efectos de sonido (SFX)")]
    public AudioClip[] fxSounds;
}
