using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class FriendlyThrowable : Throwable
{
    Task parentTask;
    Hand attachedHand;

    protected void Start()
    {
        this.parentTask = this.GetComponent<Task>();
        this.parentTask.OnResolve.AddListener(
            (Prompt p) => this.attachedHand.DetachObject(this.gameObject, this.restoreOriginalParent)
        );

        // impose constraints
        Rigidbody localRigidbody = this.GetComponent<Rigidbody>();
        localRigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    protected override void OnHandHoverBegin(Hand hand)
    {
        //Debug.Log("attached hand: " + this.attachedHand != null ? this.attachedHand.gameObject.name : null);
        //if (!this.GetComponent<FriendlyInteractable>().isHovering)
        if (this.attachedHand == null)
            base.OnHandHoverBegin(hand);
    }

    protected override void HandHoverUpdate(Hand hand)
    {
        //Debug.Log(this.gameObject.name + "->HandHoverUpdate->" + hand.name);

        GrabTypes gt = hand.GetGrabStarting();

        if (gt != GrabTypes.None && this.parentTask.IsActive() && this.attachedHand == null)
        {
            this.attachedHand = hand;
            this.parentTask.Grab(hand);
            if (this.parentTask.target != null)
            {
                // lift constraints
                Rigidbody localRigidbody = this.GetComponent<Rigidbody>();
                localRigidbody.constraints = RigidbodyConstraints.None;

                base.HandHoverUpdate(hand);
            }
        }
    }

    protected void OnDetachedFromHand()
    {
        //Debug.Log(this.gameObject.name + "->OnDetachedFromHand");

        if (this.parentTask != null && this.parentTask.target != null)
        {
            //this.attachedHand.DetachObject(this.gameObject, this.restoreOriginalParent);
            base.OnDetachedFromHand(this.attachedHand);
        }
        this.attachedHand = null;
    }

    protected override void HandAttachedUpdate(Hand hand)
    {
        //Debug.Log(this.gameObject.name + "->HandAttachedUpdate" + hand.name);
        //base.HandAttachedUpdate(hand);

        if (this.onHeldUpdate != null)
            this.onHeldUpdate.Invoke(hand);
    }
}