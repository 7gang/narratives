using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class InteractionTarget : Prompt
{
    protected override void Awake()
    {
        this.gameObject.GetComponent<MeshRenderer>().enabled = false;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        //this.OnActive.AddListener((Prompt p) => this.gameObject.GetComponent<MeshRenderer>().enabled = false);
        //this.OnResolve.AddListener((Prompt p) => this.gameObject.GetComponent<MeshRenderer>().enabled = true);
    }

    public new void Activate(Prompt p)
    {
        this.Activate();
    }

    public new void Activate()
    {
        Debug.Log(this.gameObject.name);
        this.gameObject.GetComponent<MeshRenderer>().enabled = true;
        base.Activate();
    }

    public new void Resolve()
    {
        this.gameObject.GetComponent<MeshRenderer>().enabled = false;
        base.Resolve();
    }
}