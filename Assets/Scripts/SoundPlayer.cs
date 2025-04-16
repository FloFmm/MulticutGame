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
    public void PlaySoundWithPitch(float pitch)
    {
        audioSource.pitch = pitch;  // Set the pitch
        audioSource.Play();         // Play the sound
    }
}