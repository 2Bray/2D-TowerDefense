using System.Collections.Generic;
using UnityEngine;


public class AudioScript : MonoBehaviour
{

    private static AudioScript _instance = null;

    public static AudioScript Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioScript>();
            }
            return _instance;
        }
    }

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private List<AudioClip> _audioClips;

    public void PlaySFX(string name)
    {
        AudioClip sfx = _audioClips.Find(s => s.name == name);
        if (sfx == null)
        {
            return;
        }

        _audioSource.PlayOneShot(sfx);
    }
}