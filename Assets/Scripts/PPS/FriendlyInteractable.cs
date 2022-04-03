using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(MeshRenderer))]
public class FriendlyInteractable : Interactable
{
    public Material highlightMaterial;
    public Material hoverMaterial;
    public Material grabMaterial;

    [HideInInspector]
    public bool isActuallyHovering;

    private Task parentTask;

    protected override void Start()
    {
        if (this.highlightMaterial == null)
            this.highlightMaterial = Resources.Load<Material>("YellowHue");
        if (this.hoverMaterial == null)
            this.hoverMaterial = Resources.Load<Material>("GreenHue");

        base.Start();

        this.parentTask = this.gameObject.GetComponent<Task>();
        this.ChangeMaterial(highlightMaterial);
    }

    protected override void Update()
    {
        if (this.parentTask.IsActive())
        {
            base.Update();
        }
    }

    public void Activate(int delayTime = 0)
    {
        // wait to activate glow if specified
        IEnumerator DelayedCallback()
        {
            yield return new WaitForSeconds(delayTime);
            if (this.parentTask.IsActive())
            {
                this.ChangeMaterial(this.highlightMaterial);
                //if (!this.gameObject.GetComponent<Task>().isGlovingImmidiately) return;
                base.OnHandHoverBegin(new Hand());  // fake hand don't sue me Valve
            }
        }
        StartCoroutine(DelayedCallback());
    }

    public void DebugEnterHover()
    {
        //Debug.Log("DebugEnterHover");
        this.OnHandHoverBegin(new Hand());
    }

    public void DebugExitHover()
    {
        //Debug.Log("DebugExitHover");
        this.OnHandHoverEnd(new Hand());
    }

    public void DebugGrab()
    {
        //Debug.Log("DebugGrab");
        if (this.parentTask.IsActive())
            this.parentTask.Grab(new Hand(), true);
    }

    public void DebugDrop()
    {
        //Debug.Log("DebugDrop");
        this.parentTask.Drop(new Hand(), true);
    }

    private void ChangeMaterial(Material m)
    {
        if (m == null) return;
        Interactable.highlightMat = m;
    }

    protected override void OnHandHoverBegin(Hand hand)
    {
        if (!this.parentTask.IsActive()) return;
        if (hand != null) hand.ShowGrabHint();

        //Debug.Log(this.gameObject.name + "->OnHandHoverBegin");

        this.isActuallyHovering = true;
        this.parentTask.EnterHover(hand);

        //if (!this.parentTask.isGlovingImmidiately) return;
        base.OnHandHoverEnd(hand);
        this.ChangeMaterial(this.hoverMaterial);
        base.OnHandHoverBegin(hand);
    }

    protected override void OnHandHoverEnd(Hand hand)
    {
        if (!this.parentTask.IsActive()) return;

        //Debug.Log(this.gameObject.name + "->OnHandHoverEnd");

        this.isActuallyHovering = false;
        this.parentTask.ExitHover(hand);

        //if (!this.parentTask.isGlovingImmidiately) return;
        base.OnHandHoverEnd(hand);
        this.ChangeMaterial(this.highlightMaterial);
        base.OnHandHoverBegin(hand);
    }

    protected override void OnAttachedToHand(Hand hand)
    {
        if (!this.parentTask.IsActive()) return;

        //Debug.Log(this.gameObject.name + "->OnAttachedToHand");

        this.parentTask.Grab(hand);
        this.ChangeMaterial(this.grabMaterial);

        if (this.parentTask.IsMovable()) base.OnAttachedToHand(hand);
        //this.parentTask.Resolve();
    }

    protected override void OnDetachedFromHand(Hand hand)
    {
        if (!this.parentTask.IsActive()) return;

        //Debug.Log(this.gameObject.name + "->OnDetachedFromHand");

        this.parentTask.Drop(hand);
        this.ChangeMaterial(this.highlightMaterial);

        if (this.parentTask.IsMovable()) base.OnDetachedFromHand(hand);
    }
}