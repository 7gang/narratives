using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Linq;

public class Level : MonoBehaviour
{
    [Tooltip("The first Prompt to be activated when the level is loaded. This should never be null.")]
    public Prompt entryPrompt;
    [Tooltip("If true, this Level will activate as the first Level in the game on startup.")]
    public bool isFirstLevel = false;

    [HideInInspector]
    public bool isActive;
    [HideInInspector]
    public static Level activeLevel;

    private void OnEnable()
    {
        this.SetVisibilityOfAllChildren(false);
    }

    protected void Start()
    {
        if (this.isFirstLevel) this.Activate();
    }

    /*
     * Set visibility of all child GameObjects
     */
    private void SetVisibilityRecursively(GameObject node, bool isVisible, bool keepDisabled = false)
    {
        if (node.GetComponent<InteractionTarget>() != null)
            keepDisabled = true;
        else if (node.GetComponent<Task>() != null && node.GetComponent<Task>().hideUntilActive)
            keepDisabled = true;

        if (!keepDisabled || node.GetComponent<InteractionTarget>() == null || !isVisible)
        {
            if (node.GetComponent<Renderer>() != null)
                node.GetComponent<Renderer>().enabled = isVisible;
            if (node.GetComponent<Collider>() != null)
                node.GetComponent<Collider>().enabled = isVisible;
            if (node.GetComponent<Light>() != null)
                node.GetComponent<Light>().enabled = isVisible;
        }

        for (int i = 0; i < node.transform.childCount; i++)
            this.SetVisibilityRecursively(node.transform.GetChild(i).gameObject, isVisible, keepDisabled);

        if (node.GetComponent<Renderer>() != null && keepDisabled)
            node.GetComponent<Renderer>().enabled = false;
        if (node.GetComponent<Collider>() != null && keepDisabled)
            node.GetComponent<Collider>().enabled = false;

        if (node.GetComponent<Canvas>() != null)
            node.GetComponent<Canvas>().enabled = isVisible;
        if (node.GetComponent<Light>() != null)
            node.GetComponent<Light>().enabled = isVisible;
    }

    /*
     * Toggle Renderer- and Collider components in all children of this Level
     */
    public void SetVisibilityOfAllChildren(bool isVisible)
    {
        this.isActive = isVisible;

        this.SetVisibilityRecursively(this.gameObject, isVisible);
    }

    /*
     * Activate this Level
     */
    public void Activate()
    {
        // resolve the previous activeLevel before continuing
        if (Level.activeLevel != null)
        {
            Level.activeLevel.Complete();
            Logger.Log(Classifier.Level.Unloaded, Level.activeLevel);
        }

        Level.activeLevel = this;
        this.SetVisibilityOfAllChildren(true);

        Scene scene = SceneManager.GetActiveScene();
        Debug.Log("Active Level is now " + Level.activeLevel.name + " in Scene " + scene.name);

        // activate the entryPrompt if given, otherwise quit the game
        if (entryPrompt != null)
        {
            entryPrompt.Activate();
        }
        else
        {
            Debug.LogError(this + " was activated but no entry Prompt was provided. Assuming this is the end of the game...");
            Application.Quit();
        }

        // reset player position and orientation
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Transform spawnPointTransform = GameObject.FindGameObjectWithTag("SpawnPoint").transform;
        playerObject.transform.position = new Vector3(
            spawnPointTransform.position.x,
            playerObject.transform.position.y,
            spawnPointTransform.position.z
        );
        playerObject.transform.rotation = spawnPointTransform.rotation;

        Logger.Log(Classifier.Level.Loaded, Level.activeLevel);
        Logger.Log(Classifier.Level.Started, this);
    }

    /*
     * Activate this Level through Level.Activate()
     */
    public void Activate(Prompt p)
    {
        this.Activate();
    }

    /*
     * Call when the Level is considered complete and the player is ready to move on to the next
     */
    public void Complete()
    {
        // resolve all unresolved prompts as "unsuccessful" before continuing
        Prompt.ResolveAll();

        this.SetVisibilityOfAllChildren(false);
        Logger.Log(Classifier.Level.Completed, this);
    }

    /*
     * Overload function for event chain calls, pointing to Complete()
     */
    public void Complete(Prompt p)
    {
        this.Complete();
    }
}