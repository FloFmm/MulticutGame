using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    public AudioClip audioClip;  // Assign the .wav file in the inspector
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
    }

    // Method to play the sound with different pitch (height)
    public void PlaySoundWithPitch(float pitch, float volume)
    {
        if (GameData.SoundIsOn)
        { 
            audioSource.pitch = pitch;  // Set the pitch
            audioSource.volume = volume;   // Set the volume (0.0 to 1.0)
            audioSource.Play();         // Play the sound
        }
    }
}