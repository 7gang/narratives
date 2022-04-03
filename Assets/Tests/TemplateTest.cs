using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/**
 * NOT AN ACTUAL TEST
 */
public class TemplateTest
{
    Level target;

    [SetUp]
    public void Setup()
    {
        GameObject testObject = new GameObject("TestObject");
        target = testObject.AddComponent<Level>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(target.gameObject);
    }

    // A Test behaves as an ordinary method
    [Test]
    public void InheritanceTestSimplePasses()
    {
        // Use the Assert class to test conditions
        Assert.AreEqual(1, 1);  // this serves as a template, and we don't want to interfere with actual testing, hence the trivial conditions...
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator InheritanceTestWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
