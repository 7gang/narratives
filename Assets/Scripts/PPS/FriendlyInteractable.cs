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
                base.OnHandHoverBegin(new Hand());  // fake hand don't sue me Valve
            }
        }
        StartCoroutine(DelayedCallback());
    }

    public void UnsuccessfulResolve()
    {
        this.OnDetachedFromHand(new Hand());
    }

    public void DebugEnterHover()
    {
        this.OnHandHoverBegin(new Hand());
    }

    public void DebugExitHover()
    {
        this.OnHandHoverEnd(new Hand());
    }

    public void DebugGrab()
    {
        if (this.parentTask.IsActive())
            this.parentTask.Grab(new Hand(), true);
    }

    public void DebugDrop()
    {
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
        hand?.ShowGrabHint();

        this.isActuallyHovering = true;
        this.parentTask.EnterHover(hand);

        base.OnHandHoverEnd(hand);
        this.ChangeMaterial(this.hoverMaterial);
        base.OnHandHoverBegin(hand);
    }

    protected override void OnHandHoverEnd(Hand hand)
    {
        if (!this.parentTask.IsActive()) return;

        this.isActuallyHovering = false;
        this.parentTask.ExitHover(hand);

        base.OnHandHoverEnd(hand);
        this.ChangeMaterial(this.highlightMaterial);
        base.OnHandHoverBegin(hand);
    }

    protected override void OnAttachedToHand(Hand hand)
    {
        if (!this.parentTask.IsActive()) return;

        this.parentTask.Grab(hand);
        this.ChangeMaterial(this.grabMaterial);

        if (this.parentTask.IsMovable()) base.OnAttachedToHand(hand);
    }

    protected override void OnDetachedFromHand(Hand hand)
    {
        if (!this.parentTask.IsActive()) return;

        this.parentTask.Drop(hand);
        this.ChangeMaterial(this.highlightMaterial);

        if (this.parentTask.IsMovable()) base.OnDetachedFromHand(hand);
    }
}