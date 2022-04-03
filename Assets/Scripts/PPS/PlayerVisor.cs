using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
[RequireComponent(typeof(GraphicRaycaster))]
public class PlayerVisor : MonoBehaviour
{

    public static PlayerVisor activeVisor;
    public static Camera playerCamera;
    public static Dictionary<Prompt, Text> texts = new Dictionary<Prompt, Text>();
    public RawImage rightSideIndicator;
    public RawImage leftSideIndicator;
    public Canvas canvas;
    static PlayerVisor()
    {
        //SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        //Debug.Log("ActiveVisorSet");

        texts.Clear();

        GameObject[] visorObjects = GameObject.FindGameObjectsWithTag("HUD");
        foreach (GameObject vo in visorObjects)
            if (vo.scene == newScene)
                PlayerVisor.activeVisor = vo.GetComponent<PlayerVisor>();

        Debug.Log("Found PlayerVisor in Scene " + PlayerVisor.activeVisor.gameObject.scene.name);
        PlayerVisor.FindPlayerCamera();
    }

    private void Update()
    {
        if (playerCamera == null) return;  // TODO: fix PlayerVisor for VR...

        //Debug.Log(cam.WorldToScreenPoint(target.transform.position));
        foreach (Prompt targetObject in texts.Keys)
        {
            Text textReticle = texts[targetObject];

            // hide/show text depending on the state of the associated Prompt
            if (targetObject.IsActive())
            {
                if (textReticle.enabled == false)
                {
                    textReticle.enabled = true;
                    Logger.Log(Classifier.Prompt.TextShowing, targetObject);
                }
            }
            else
            {
                if (textReticle.enabled == true)
                {
                    textReticle.enabled = false;
                    Logger.Log(Classifier.Prompt.TextHidden, targetObject);
                }
                continue;
            }


            Vector3 pos = playerCamera.WorldToViewportPoint(targetObject.transform.position);
            bool onScreen = pos.z > 0 && pos.x > 0 && pos.x < 1 && pos.y > 0 && pos.y < 1;
            Vector3 dir = (targetObject.transform.position - playerCamera.transform.position).normalized;

            // TODO: refine corners of screen for VR (possible use a circle instead of square)
            if (!onScreen)
            {


                if (Vector3.SignedAngle(playerCamera.transform.forward, dir, Vector3.up) > 0)
                {
                    //right side
                    rightSideIndicator.color = new Color(1, 1, 1, 0.3f);
                }
                else
                {
                    // left side
                    leftSideIndicator.color = new Color(1, 1, 1, 0.3f); ;
                }
            }
            else
            {
                rightSideIndicator.color = Color.clear;
                leftSideIndicator.color = Color.clear;
            }
            textReticle.transform.position = new Vector3((targetObject.transform.position.x + playerCamera.transform.position.x) * 0.5f, (targetObject.transform.position.y + playerCamera.transform.position.y) * 0.8f, (targetObject.transform.position.z + playerCamera.transform.position.z) * 0.5f);
            //Debug.Log(new Vector3(targetObject.transform.position.x - playerCamera.transform.position.x * 2, targetObject.transform.position.y + 0.2f, targetObject.transform.position.z - playerCamera.transform.position.z * 2));
        }
    }

    public static void AddMyText(Prompt prompt)
    {
        if (prompt.promptText == "") return;

        // add Text GameObject with required components
        GameObject newTextObject = new GameObject(prompt.promptText);
        newTextObject.layer = 5;  // UI layer
        newTextObject.AddComponent<RectTransform>();
        newTextObject.AddComponent<CanvasRenderer>();
        newTextObject.AddComponent<Text>();

        // style text
        Text text = newTextObject.GetComponent<Text>();
        text.text = prompt.promptText;
        text.fontSize = 24;
        text.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        text.fontStyle = FontStyle.Bold;

        // make text available for display
        newTextObject.transform.parent = PlayerVisor.activeVisor.transform;
        newTextObject.GetComponent<RectTransform>().localScale = Vector3.one;
        PlayerVisor.texts.Add(prompt, newTextObject.GetComponent<Text>());
        text.enabled = false;
    }

    public static void FindPlayerCamera()
    {
        // CURRENTLY DOESN'T WORK FOR VR, ONLY DEBUGPLAYER
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject po in playerObjects)
        {
            Debug.Log(po.name);
            if (po.scene == SceneManager.GetActiveScene() && po.activeInHierarchy || po.activeSelf)
            {
                PlayerVisor.playerCamera = po.GetComponentInChildren<Camera>();
                GameObject.FindGameObjectWithTag("HUD").GetComponent<Canvas>().worldCamera = playerCamera;
            }
        }
    }
}