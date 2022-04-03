using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ActiveEvent : UnityEvent<Prompt> { }
[Serializable]
public class ResolveEvent : UnityEvent<Prompt> { }

/**
 * The base class for audio/visual Player signalling and communications
 */
//[RequireComponent(typeof(AudioSource))]
public class Prompt : MonoBehaviour
{
    [HideInInspector]
    [Tooltip("A (preferably) unqiue and descriptive name to help identify this Prompt.")]
    public string promptName = "An Awesome Prompt";
    [Tooltip("The UI text to be shown when this Prompt becomes active")]
    public string promptText = "";

    [Tooltip("The audio clip to be played when the Prompt is activated.")]
    public AudioClip audioClip;
    [Tooltip("The playback volume range (0 to 1) of the audio clip to be played.")]
    public float playbackVolume = 0.2f;
    [Tooltip("Whether the audio clip should be played in the player's ear (omnidirectional) or coming from the associated GameObject (spatial).")]
    public bool isOmnidirectional = false;
    [Tooltip("Whether the audio clip should start again after it has finished playing (provided the Prompt is still active at that point).")]
    public bool isLooping = true;
    [Tooltip("The amount of seconds (as a float) to wait after playback has ended before continuing.")]
    public float secondsWaitAfterPlay = 1.5f;

    [Tooltip("Invoked whenever this Prompt is activated. This could be if you want something to happen simultanuously with this Prompt!")]
    public ActiveEvent OnActive = new ActiveEvent();
    [Tooltip("Invoked whenever this Prompt is resolved. This could be if you want another Prompt to fire after this one is done!")]
    public ResolveEvent OnResolve = new ResolveEvent();

    [HideInInspector]
    public static List<Prompt> activePrompts = new List<Prompt>();  // enumerating the raw List is discouraged, as asynchronous modification can occur

    protected Level parentLevel;
    private bool isActive = false;

    protected virtual void Awake()
    {
        // make some settings in AudioSource right off the bat
        AudioSource localSource = this.gameObject.AddComponent<AudioSource>();
        localSource.playOnAwake = false;
        localSource.clip = this.audioClip;

        // handle spatial audio
        if (!this.isOmnidirectional)
        {
            localSource.maxDistance = 8f;
            localSource.spatialBlend = 1;
        }
    }

    protected virtual void Start()
    {
        // try to retrieve the parent level
        this.parentLevel = this.GetParentLevel();

        /*if (this.promptText == "")
            this.promptText = this.promptName;*/
        //PlayerVisor.AddMyText(this);
    }

    public override string ToString()
    {
        return $"<\"{this.name}\"> " + base.ToString();
    }

    /*
     * Resolve all currently active Prompts
     */
    public static void ResolveAll()
    {
        // copy activePrompts to protect from asynchronous modification during enumeration
        List<Prompt> activePromptsCopy = new List<Prompt>(activePrompts);
        foreach (Prompt p in activePromptsCopy) p.Resolve();
    }

    /*
     * Toggle active/non-active state
     * It's probably not the best idea to use this, better to do it explicitly using Activate and Resolve
     */
    public void ToggleActive()
    {
        this.SetActive(!this.IsActive());
    }

    /*
     * Activate the Prompt
     * This can be called as often as you like; it'll only fire if the Prompt is currently inactive
     */
    public void Activate()
    {
        this.SetActive(true);
    }

    /*
     * Activate the Prompt, considering the given Prompt as the parent
     * This is not actually used for anything internally, and is only here to make chaining Prompts easier
     */
    public void Activate(Prompt parentPrompt)
    {
        this.Activate();
    }

    /*
     * Resolve this Prompt
     * This can be called as often as you like; it'll only fire if the Prompt is currently active
     */
    public void Resolve(bool successful = true)
    {
        this.SetActive(false, successful);
    }

    public void Resolve(Prompt parentPrompt, bool successful = true)
    {
        this.Resolve(successful);
    }

    /*
     * Whether this Prompt is currently active or not
     */
    public bool IsActive()
    {
        return this.isActive;
    }

    /*
     * Retrieves the level containing this Prompt in the game world, otherwise null
     * It is important that every Prompt is placed inside a GameObject with an active Level component
     */
    public Level GetParentLevel()
    {
        if (this.parentLevel == null)
        {
            // find parent Level
            this.parentLevel = GetComponentInParent<Level>();
            if (this.parentLevel == null) Debug.LogError(this + " was unable to get parent level", this);
        }
        return this.parentLevel;
    }

    private GameObject GetPlayerObject()
    {
        // find the player object
        try
        {
            return GameObject.FindGameObjectsWithTag("Player")[0];  // there should only ever be one GameObject tagged with "Player"
        }
        catch (IndexOutOfRangeException e)
        {
            Debug.LogError(this + " was unable to locate \"Player\"-tagged object. Is the Player object tagged with \"Player\"? The game will continue running but omniderectional sound will error: " + e, this);
        }
        return null;
    }

    private void SetActive(bool newState, bool successful = true)
    {
        // make sure to not invoke events unless the state has actually changed
        if (newState != this.IsActive())
        {
            if (newState) this.TurnOn();
            else this.TurnOff(successful);
        }
    }

    protected virtual void TurnOn(int delayTime = 0)
    {
        this.isActive = true;
        activePrompts.Add(this);
        if (this.OnActive != null) this.OnActive.Invoke(this);
        Logger.Log(Classifier.Prompt.Activated, this);

        // wait to activate sound if specified
        IEnumerator DelayedCallback()
        {
            yield return new WaitForSeconds(delayTime);
            if (this.IsActive())
            {
                PlaySound();
            }
        }
        StartCoroutine(DelayedCallback());
    }

    protected virtual void OnPlaybackEnd()
    {
        if (this.GetAudioClip() != null && this.isLooping)
            this.PlaySound();  // keep playing sound effect recursively if there is one to be played
        else this.TurnOff();

        if (this.playbackVolume != 0f)
            Logger.Log(Classifier.Prompt.SoundStopped, this);
    }

    protected void PlaySound()
    {
        if (this.GetAudioClip() != null)
        {
            if (this.playbackVolume != 0f)
            {
                this.GetAudioSource().PlayOneShot(this.GetAudioClip(), this.playbackVolume);
                Logger.Log(Classifier.Prompt.SoundPlaying, this);
            }

            IEnumerator DelayedCallback(float time)
            {
                yield return new WaitForSeconds(time);
                if (this.IsActive()) this.OnPlaybackEnd();
            }
            StartCoroutine(DelayedCallback(this.GetAudioClip().length + this.secondsWaitAfterPlay));
        }
        /*else
        {
            Debug.LogWarning(this + " attempted to play audio but no clip was given", this);
            this.OnPlaybackEnd();
        }*/
    }

    protected virtual void TurnOff(bool successful = true)
    {
        this.isActive = false;
        activePrompts.Remove(this);
        Logger.Log(Classifier.Prompt.Resolved, this);

        this.GetAudioSource().Stop();

        if (this.OnActive != null && successful) this.OnResolve.Invoke(this);
        else Debug.LogWarning(this + " OnResolve was invoked with no subscribed listeners. Is the event chain supposed to end here?", this);
    }

    private AudioSource GetAudioSource()
    {
        if (this.isOmnidirectional && this.GetPlayerObject() != null)
        {
            AudioSource source = GetPlayerObject().GetComponentInChildren<AudioSource>();
            if (source != null) return source;
            else Debug.LogError(this + " was unable to locate audio source on \"Player\"-tagged object in Prompt. Does the Player object have an AudioSource component?", this);
        }
        return this.gameObject.GetComponent<AudioSource>();
    }

    protected AudioClip GetAudioClip()
    {
        return this.gameObject.GetComponent<AudioSource>().clip;
    }
}