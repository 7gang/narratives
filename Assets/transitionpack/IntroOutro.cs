using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroOutro : MonoBehaviour
{
    public Animator animator;
    public string anim;
    GameObject transition;
    //public Text text;
    //public bool gamefinish = false;

    private void Awake()
    {
        transition = GameObject.Find("Transition");
        //text = transition.GetComponentInChildren<Text>();
        animator = transition.GetComponent<Animator>();
        fadeIn();
    }

    public void fadeIn(Prompt p)
    {
        Debug.Log("called");
        anim = "FIn";
        animator.SetTrigger(anim);
    }

    public void fadeIn()
    {
        Debug.Log("called");
        anim = "FIn";
        animator.SetTrigger(anim);
    }

    public void fadeOut(Prompt p)
    {
        //text.text = "Loading";
        anim = "FOut";
        animator.SetTrigger(anim);
        /*if (gamefinish)
        {
            text.text = "You take a sip of the beer and you are satisfied with the results";
            IEnumerator DelayedCallback()
            {
                yield return new WaitForSeconds(6);
                Application.Quit();
            }
            StartCoroutine(DelayedCallback());
        }*/
    }

    public void fadeOut(float delay)
    {
        //text.text = "Day " + (GameObject.Find("MinigameManager").GetComponent<MinigameManagerScript>().currentLevel + 1);
        anim = "FOut";
        Invoke("playFadoOut",delay);

    }

    void playFadoOut()
    {
        animator.SetTrigger(anim);
    }

    /*public void finishGame()
    {
        gamefinish = true;
    }

    public void finishGame(Prompt p)
    {
        this.finishGame();
    }*/
}