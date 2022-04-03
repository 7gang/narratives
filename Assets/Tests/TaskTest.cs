using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Valve.VR.InteractionSystem;

public class TaskTest : PromptTest
{
    Task testTask;
    Task testTask2;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        GameObject testTaskObject = new GameObject("TestTask");
        GameObject testTaskObject2 = new GameObject("TestTask2");
        testTaskObject.transform.parent = this.testLevel.transform;  // set the parent before Start in Prompt is run
        testTaskObject2.transform.parent = this.testLevel.transform;
        this.testTask = testTaskObject.AddComponent<Task>();
        this.testTask2 = testTaskObject2.AddComponent<Task>();

        // required components must be added manually in testing, even though it is done through the editor
        testTask.gameObject.AddComponent<AudioSource>();
        testTask.gameObject.AddComponent<FriendlyInteractable>();
        testTask2.gameObject.AddComponent<AudioSource>();
        testTask2.gameObject.AddComponent<FriendlyInteractable>();
    }

    /*[Test]
    public void GlowsWhenGlowingIsEnabled()
    {
        Assert.True(testTask.isGlovingImmidiately);
        Material defaultHighlightMaterial = Resources.Load<Material>("YellowHue");  // whatever we go for as the default material must be updated here
        //Debug.Log(defaultHighlightMaterial);
        Assert.AreNotEqual(defaultHighlightMaterial, testTask.GetComponent<MeshRenderer>().materials[0]);

        testTask.OnActive.AddListener((Prompt p) =>
        {
            bool highlightMaterialInListOfSharedMaterials = false;
            foreach (Material m in testTask.GetComponent<MeshRenderer>().materials)
            {
                if (m != null && m.Equals(defaultHighlightMaterial))
                    highlightMaterialInListOfSharedMaterials = true;
            }
            Assert.True(highlightMaterialInListOfSharedMaterials);
        });
        testTask.Activate();
    }*/
}
