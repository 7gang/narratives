using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public AudioClip soundtrack;
    public float soundtrackPlaybackVolume = 0.2f;

    public AudioClip soundtrackDark;
    public float soundtrackDarkPlaybackVolume = 0.2f;

    protected AudioSource _source;

    protected void Start()
    {
        _source = this.gameObject.GetComponent<AudioSource>();

        this.Play(this.soundtrack, this.soundtrackPlaybackVolume);
    }

    protected void Play(AudioClip clip, float volume)
    {
        _source.PlayOneShot(clip, volume);

        IEnumerator DelayedCallback(float duration)
        {
            yield return new WaitForSeconds(duration);
            this.Play(this.soundtrackDark, this.soundtrackDarkPlaybackVolume);
        }
        StartCoroutine(DelayedCallback(clip.length));
    }
}
