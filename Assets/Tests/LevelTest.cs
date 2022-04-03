using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

public class LevelTest
{
    protected Level testLevel;
    protected Prompt testPrompt;
    
    protected Level testLevel2;
    protected Prompt testPrompt2;

    protected Level testLevel3;
    protected Prompt testPrompt3;

    [SetUp]
    public void Setup()
    {
        // test Level 1
        GameObject testLevelObject = new GameObject("Level1");
        this.testLevel = testLevelObject.AddComponent<Level>();
        GameObject testPromptObject = new GameObject("TestPrompt1");
        testPromptObject.transform.parent = testLevelObject.transform;  // set the parent before Start in Prompt is run
        this.testPrompt = testPromptObject.AddComponent<Prompt>();
        this.testLevel.entryPrompt = testPrompt;

        // test Level 2
        GameObject testLevelObject2 = new GameObject("Level2");
        this.testLevel2 = testLevelObject2.AddComponent<Level>();
        GameObject testPromptObject2 = new GameObject("TestPrompt2");
        testPromptObject2.transform.parent = testLevelObject2.transform;
        this.testPrompt2 = testPromptObject2.AddComponent<Prompt>();
        this.testLevel2.entryPrompt = testPrompt2;

        // test Level 3
        GameObject testLevelObject3 = new GameObject("Level3");
        this.testLevel3 = testLevelObject3.AddComponent<Level>();
        GameObject testPromptObject3 = new GameObject("TestPrompt3");
        testPromptObject3.transform.parent = testLevelObject3.transform;
        this.testPrompt3 = testPromptObject3.AddComponent<Prompt>();
        this.testLevel3.entryPrompt = testPrompt3;

        // audio source must be added manually in testing, even though it is done through the editor
        testPrompt.gameObject.AddComponent<AudioSource>();
        testPrompt2.gameObject.AddComponent<AudioSource>();
        testPrompt3.gameObject.AddComponent<AudioSource>();

        this.testLevel.Complete();
    }

    [TearDown]
    public void Teardown()
    {
        Prompt.ResolveAll();
        Level.activeLevelIndex = -1;

        Object.DestroyImmediate(testLevel.gameObject);  // running DestroyImmediate is ok since we're testing in edit mode and >should< also destroy child objects... hopefully...
        Object.DestroyImmediate(testLevel2.gameObject);
        Object.DestroyImmediate(testLevel3.gameObject);
    }

    [Test]
    public void ActivatesLevel1OnStartup()
    {
        NUnit.Framework.Assert.AreEqual(Level.activeLevel.name, this.testLevel.name);
        NUnit.Framework.Assert.True(this.testLevel.isActive);
    }

    [Test]
    public void ActivatesLevel1EntryPromptOnStartup()
    {
        Prompt prompt = this.testLevel.gameObject.GetComponentInChildren<Prompt>();
        NUnit.Framework.Assert.True(prompt.Equals(testPrompt));
        NUnit.Framework.Assert.True(prompt.IsActive());
    }

    [Test]
    public void DoesNotActivateOtherLevelsOnStartup()
    {
        NUnit.Framework.Assert.AreNotEqual(Level.activeLevel.name, this.testLevel2.name);
        NUnit.Framework.Assert.AreNotEqual(Level.activeLevel.name, this.testLevel3.name);
        NUnit.Framework.Assert.False(this.testLevel2.isActive);
        NUnit.Framework.Assert.False(this.testLevel3.isActive);
    }

    [Test]
    public void DoesNotActivateEntryPromptOfOtherLevelsOnStartup()
    {
        Prompt prompt2 = this.testLevel2.gameObject.GetComponentInChildren<Prompt>();
        NUnit.Framework.Assert.True(prompt2.Equals(testPrompt2));
        NUnit.Framework.Assert.False(prompt2.IsActive());

        Prompt prompt3 = this.testLevel3.gameObject.GetComponentInChildren<Prompt>();
        NUnit.Framework.Assert.True(prompt3.Equals(testPrompt3));
        NUnit.Framework.Assert.False(prompt3.IsActive());
    }

    [Test]
    public void ResolvesAllPromptsOnComplete()
    {
        Prompt prompt = this.testLevel.gameObject.GetComponentInChildren<Prompt>();
        NUnit.Framework.Assert.True(this.testLevel.isActive);
        NUnit.Framework.Assert.True(prompt.IsActive());
        this.testLevel.Complete();
        NUnit.Framework.Assert.False(prompt.IsActive());
    }

    [Test]
    public void MakesChildElementsInvisibleWhenCompleted()
    {
        // inject renderers and colliders into Prompt for testing
        this.testPrompt.gameObject.AddComponent<MeshRenderer>();
        this.testPrompt.gameObject.AddComponent<BoxCollider>();

        NUnit.Framework.Assert.True(this.testPrompt.IsActive());
        NUnit.Framework.Assert.True(this.testPrompt.GetComponent<Renderer>().enabled);
        NUnit.Framework.Assert.True(this.testPrompt.GetComponent<Collider>().enabled);

        this.testLevel.Complete();                                  // assumes the queue ordering of Levels are correct!
        NUnit.Framework.Assert.False(this.testPrompt.IsActive());   // assumes the activation of child prompts works!
        NUnit.Framework.Assert.False(this.testPrompt.GetComponent<Renderer>().enabled);
        NUnit.Framework.Assert.False(this.testPrompt.GetComponent<Collider>().enabled);
    }

    [Test]
    public void MakesChildElementsVisibleWhenActivated()
    {
        // inject renderers and colliders into Prompt for testing
        this.testPrompt2.gameObject.AddComponent<MeshRenderer>();
        this.testPrompt2.gameObject.AddComponent<BoxCollider>();
        this.testPrompt2.GetComponent<Renderer>().enabled = false;
        this.testPrompt2.GetComponent<Collider>().enabled = false;

        NUnit.Framework.Assert.False(this.testPrompt2.IsActive());
        NUnit.Framework.Assert.False(this.testPrompt2.GetComponent<Renderer>().enabled);
        NUnit.Framework.Assert.False(this.testPrompt2.GetComponent<Collider>().enabled);

        this.testLevel.Complete();                                  // assumes the queue ordering of Levels are correct!
        NUnit.Framework.Assert.True(this.testPrompt2.IsActive());   // assumes the activation of child prompts works!
        NUnit.Framework.Assert.True(this.testPrompt2.GetComponent<Renderer>().enabled);
        NUnit.Framework.Assert.True(this.testPrompt2.GetComponent<Collider>().enabled);
    }

    [Test]
    public void DoesNotReEnableCertainSubRenderersAndColliders()
    {
        // setup objects with InteractionTarget and Task, and a child with a renderer or collider
        /*this.testPrompt2.gameObject.AddComponent<InteractionTarget>();
        this.testPrompt3.gameObject.AddComponent<Task>();
        this.testPrompt3.GetComponent<Task>().hideUntilActive = true;

        GameObject childRenderer = new GameObject("childRenderer");
        childRenderer.AddComponent<MeshRenderer>();
        childRenderer.GetComponent<Renderer>().enabled = false;
        childRenderer.transform.SetParent(this.testLevel2.transform);

        GameObject childCollider = new GameObject("childCollider");
        childCollider.AddComponent<BoxCollider>();
        childCollider.GetComponent<Collider>().enabled = false;
        childCollider.transform.SetParent(this.testLevel3.transform);

        Debug.Log(this.testPrompt2.GetComponentsInChildren<Renderer>().Where(go => go.gameObject != this.testPrompt2.gameObject).First().name);
        NUnit.Framework.Assert.False(this.testPrompt2.IsActive());
        NUnit.Framework.Assert.False(this.testPrompt2.GetComponentInChildren<Renderer>().enabled);
        this.testLevel.Complete();                                  // assumes the queue ordering of Levels are correct!
        NUnit.Framework.Assert.True(this.testPrompt2.IsActive());   // assumes the activation of child prompts works!
        NUnit.Framework.Assert.False(this.testPrompt2.GetComponentInChildren<Renderer>().enabled);
        
        NUnit.Framework.Assert.False(this.testPrompt3.IsActive());
        NUnit.Framework.Assert.False(this.testPrompt3.GetComponentInChildren<Collider>().enabled);
        this.testLevel2.Complete();
        NUnit.Framework.Assert.True(this.testPrompt3.IsActive());
        NUnit.Framework.Assert.False(this.testPrompt3.GetComponentInChildren<Collider>().enabled);*/
    }

    [Test]
    public void MaintainsLevelOrder()
    {
        Level level1 = this.testLevel.GetComponent<Level>();
        Level level2 = this.testLevel2.GetComponent<Level>();
        Level level3 = this.testLevel3.GetComponent<Level>();

        NUnit.Framework.Assert.AreEqual(Level.activeLevel.name, this.testLevel.name);
        NUnit.Framework.Assert.True(level1.isActive);
        NUnit.Framework.Assert.False(level2.isActive);
        NUnit.Framework.Assert.False(level3.isActive);
        
        this.testLevel.Complete();
        NUnit.Framework.Assert.AreEqual(Level.activeLevel.name, this.testLevel2.name);
        NUnit.Framework.Assert.False(level1.isActive);
        NUnit.Framework.Assert.True(level2.isActive);
        NUnit.Framework.Assert.False(level3.isActive);

        this.testLevel2.Complete();
        NUnit.Framework.Assert.AreEqual(Level.activeLevel.name, this.testLevel3.name);
        NUnit.Framework.Assert.False(level1.isActive);
        NUnit.Framework.Assert.False(level2.isActive);
        NUnit.Framework.Assert.True(level3.isActive);
    }

    [Test]
    public void MaintainsPromptStates()
    {
        NUnit.Framework.Assert.True(this.testPrompt.IsActive());
        NUnit.Framework.Assert.False(this.testPrompt2.IsActive());
        NUnit.Framework.Assert.False(this.testPrompt3.IsActive());

        this.testLevel.Complete();
        NUnit.Framework.Assert.False(this.testPrompt.IsActive());
        NUnit.Framework.Assert.True(this.testPrompt2.IsActive());
        NUnit.Framework.Assert.False(this.testPrompt3.IsActive());

        this.testLevel.Complete();
        NUnit.Framework.Assert.False(this.testPrompt.IsActive());
        NUnit.Framework.Assert.False(this.testPrompt2.IsActive());
        NUnit.Framework.Assert.True(this.testPrompt3.IsActive());
    }
}
