using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Linq;

public class Level : MonoBehaviour
{
    [Tooltip("The first Prompt to be activated when the level is loaded.")]
    public Prompt entryPrompt;

    [HideInInspector]
    public bool isActive;
    [HideInInspector]
    public static Level activeLevel;
    [HideInInspector]
    public static int activeLevelIndex = -1;

    private static Level[] levelsInScene = new Level[3];
    private static bool firstLevelStarted = false;

    private void OnEnable()
    {
        this.SetVisibilityOfAllChildren(false);
    }

    protected void Start()
    {
        if (!Level.firstLevelStarted)
            Level.Continue();
    }

    private static Level[] getLevelsInScene(Scene scene)
    {
        if (Level.levelsInScene[0] == null)
        {
            // find all Level components in the Scene
            IEnumerator ie = scene.GetRootGameObjects().GetEnumerator();
            ie.Reset();
            while (ie.MoveNext())
            {
                GameObject currentObject = (GameObject)ie.Current;
                if (currentObject != null && currentObject.GetComponent<Level>())
                    //levelsInScene.Add(currentObject.GetComponent<Level>());
                    switch (currentObject.name)
                    {
                        case "Level1":
                            Level.levelsInScene[0] = currentObject.GetComponent<Level>();
                            break;
                        case "Level2":
                            Level.levelsInScene[1] = currentObject.GetComponent<Level>();
                            break;
                        case "Level3":
                            Level.levelsInScene[2] = currentObject.GetComponent<Level>();
                            break;
                    }
            }
        }
        return Level.levelsInScene;
    }

    /*
     * Continue to the next Level after completing the current activeLevel
     */
    private static void Continue()
    {
        if (!Level.firstLevelStarted)
        {
            Logger.Log(Classifier.Level.Unloaded, Level.activeLevel);
            Level.firstLevelStarted = true;
        }

        Scene scene = SceneManager.GetActiveScene();
        
        Level.activeLevelIndex++;
        
        Logger.Log(Classifier.Level.Loaded, Level.activeLevel);
        if (!Level.firstLevelStarted) Debug.Log("Active Level is now " + Level.activeLevel.name + " in Scene " + scene.name);

        // activate the new activeLevel
        Level.getLevelsInScene(scene)[Level.activeLevelIndex].Activate();
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
        }

        for (int i = 0; i < node.transform.childCount; i++)
            this.SetVisibilityRecursively(node.transform.GetChild(i).gameObject, isVisible, keepDisabled);

        if (node.GetComponent<Renderer>() != null && keepDisabled)
            node.GetComponent<Renderer>().enabled = false;
        if (node.GetComponent<Collider>() != null && keepDisabled)
            node.GetComponent<Collider>().enabled = false;

        if (node.GetComponent<Canvas>() != null)
            node.GetComponent<Canvas>().enabled = isVisible;
    }

    /*
     * Toggles Renderer- and Collider components in all children of this Level
     */
    public void SetVisibilityOfAllChildren(bool isVisible)
    {
        this.isActive = isVisible;

        this.SetVisibilityRecursively(this.gameObject, isVisible);
    }

    /*
     * Activates this Level
     */
    public void Activate()
    {
        Level.activeLevel = this;
        this.SetVisibilityOfAllChildren(true);
        if (entryPrompt != null) entryPrompt.Activate();
        else Debug.LogError(this + " was activated but no initial Prompt was given. Did you foget to reference the entry Prompt?");

        Logger.Log(Classifier.Level.Started, this);
    }

    /*
     * Call when the Level is considered complete and the player is ready to move on to the next
     */
    public void Complete()
    {
        // resolve all unresolved prompts as "unsuccessful" before continuing
        foreach (Prompt ap in new List<Prompt>(Prompt.activePrompts))
            ap.Resolve(false);

        this.SetVisibilityOfAllChildren(false);
        Logger.Log(Classifier.Level.Completed, this);

        // run preloaded scene
        Level.Continue();
    }

    /*
     * Overload function for event chain calls, pointing to Complete()
     */
    public void Complete(Prompt p)
    {
        this.Complete();
    }
}