using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;
using Valve.VR.InteractionSystem;

[Serializable]
public class GrabEvent : UnityEvent<Prompt> { }

[RequireComponent(typeof(FriendlyInteractable))]
public class Task : Prompt
{
    [Tooltip("Make GameObject glow when activated.")]
    public bool isGlovingImmidiately = true;
    [Tooltip("Hide GameObject until activated.")]
    public bool hideUntilActive = false;
    [Tooltip("If set, the Task will only resolve when the associated GameObject is moved to the same location as the target transform. Setting this also makes the Task movable by the Player.")]
    public InteractionTarget target;
    [Tooltip("The amount of diviation on all axis (in Unity units) allowed from the target transform before it is considered hit.")]
    public float targetPrecision = 0.2f;
    [Tooltip("When enabled, the Task object must match both the position AND the rotation of the target.")]
    public bool matchRotation = false;
    [Tooltip("Disable the mesh and interactability of the Task object after the Task is completed.")]
    public bool hideAfterCompletion = true;

    [Tooltip("Invoked whenever this Task is grabbed. Use this to activate narrative Prompts!")]
    public ActiveEvent OnGrab = new ActiveEvent();

    private bool isVisible = false;

    protected override void Start()
    {
        if (this.target != null) this.gameObject.AddComponent<Rigidbody>();
        if (this.hideUntilActive) this.Hide();
        base.Start();
    }

    protected virtual void Update()
    {
        // TODO: check for position in relation to target if defined and grabbed...
        if (this.target != null && this.IsActive() /*&& this.gameObject.GetComponent<FriendlyInteractable>().attachedToHand != null*/)
        {
            Transform selfTransform = this.transform;
            Transform targetTransform = this.target.transform;

            // check position
            Vector3 positionDifference = selfTransform.position - targetTransform.position;
            bool validPosition = true;
            for (int i = 0; i < 3; i++)
                if (Mathf.Abs(positionDifference[i]) > this.targetPrecision) validPosition = false;

            // check rotation if set
            Quaternion simplifiedRotation = Quaternion.Euler(selfTransform.rotation.x, targetTransform.rotation.y, selfTransform.rotation.z);

            bool validRotation = true;
            /*if (this.matchRotation && !CompareRotation(simplifiedRotation, targetTransform.rotation, 0.15f))
                validRotation = false;*/

            // report the product of both checks
            if (validPosition && validRotation) this.Resolve();
        }
    }
    
    protected override void OnPlaybackEnd()
    {
        if (this.GetAudioClip() != null && this.isLooping)
            this.PlaySound();  // keep playing sound effect recursively if there is one to be played
        //else this.TurnOff();
    }

    private bool CompareRotation(Quaternion r1, Quaternion r2, float precision)
    {
        return Mathf.Abs(Quaternion.Dot(r1, r2)) >= 1 - precision;
    }

    public bool IsMovable()
    {
        return this.target != null;
    }

    public void EnterHover(Hand hand)
    {
        Logger.Log(Classifier.Task.Touching, this);
    }

    public void ExitHover(Hand hand)
    {
        Logger.Log(Classifier.Task.NoLongerTouching, this);
    }

    /*
     * Called when the player grabs the associated GameObject
     */
    public void Grab(Hand hand, bool debugAutoComplete = false)
    {
        // the player has grabbed this GameObject...
        if (!this.target || debugAutoComplete)
            this.Resolve();
        else
        {
            this.target.Activate();
            this.OnGrab.Invoke(this);
        }

        if (hand.handType == SteamVR_Input_Sources.RightHand)
            Logger.Log(Classifier.Player.PlayerRightHandInputEngaged);
        else Logger.Log(Classifier.Player.PlayerLeftHandInputEngaged);
        Logger.Log(Classifier.Task.Grabbed, this);
    }

    /*
     * Called when the player lets go of the associated GameObject
     */
    public void Drop(Hand hand, bool debugAutoComplete = false)
    {
        // the player has released this GameObject...
        if (hand.handType == SteamVR_Input_Sources.RightHand)
            Logger.Log(Classifier.Player.PlayerRightHandInputReleased);
        else Logger.Log(Classifier.Player.PlayerLeftHandInputReleased);
        Logger.Log(Classifier.Task.Released, this);
    }

    public new void Resolve()
    {
        if (this.target)
            this.target.Resolve();

        this.GetComponent<Rigidbody>().isKinematic = false;
        this.GetComponent<FriendlyInteractable>().enabled = false;
        if (this.hideAfterCompletion) this.Hide();

        // impose constraints
        Rigidbody localRigidbody = this.GetComponent<Rigidbody>();
        localRigidbody.constraints = RigidbodyConstraints.FreezeAll;

        base.Resolve();
    }

    public void Show()
    {
        this.SetChildRenderersRecursively(this.gameObject);
    }

    public void Show(Prompt p)
    {
        this.Show();
    }

    public void Hide()
    {
        this.SetChildRenderersRecursively(this.gameObject, false);
    }

    public void Hide(Prompt p)
    {
        this.Hide();
    }

    private void SetChildRenderersRecursively(GameObject node, bool state = true)
    {
        //Debug.Log(this.name + "->SetChildRenderersRecursively->" + node.name + "->" + state);
        for (int i = 0; i < node.transform.childCount; i++)
            this.SetChildRenderersRecursively(node.transform.GetChild(i).gameObject, state);

        if (node.GetComponent<Renderer>() != null)
            node.GetComponent<Renderer>().enabled = state;
        if (node.GetComponent<Collider>() != null)
            node.GetComponent<Collider>().enabled = state;

        if (this.gameObject == node) isVisible = state;
    }

    protected override void TurnOn(int delayTime = 0)  // delayTime not actually used
    {
        // make visible and interactable
        if (this.hideUntilActive && !isVisible) this.Show();

        // wait to activate glow and sound if specified
        int delay = this.isGlovingImmidiately ? 0 : 10;  // TODO: adjust wait timer here;
        this.gameObject.GetComponent<FriendlyInteractable>().Activate(delay);
        base.TurnOn(delay);
    }

    protected override void TurnOff(bool successful = true)
    {
        base.TurnOff();
        this.gameObject.GetComponent<FriendlyInteractable>().DebugExitHover();
        if (this.hideAfterCompletion) this.gameObject.GetComponent<Collider>().enabled = false;
    }
}