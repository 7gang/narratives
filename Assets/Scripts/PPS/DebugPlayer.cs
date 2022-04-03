using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Allows for rudimentary control of the Player object for debugging purposes
 */
public class DebugPlayer : MonoBehaviour
{
    public static bool isActive = false;
    public static float sensitivity = 3f;
    public static float speed = 3f;
    public static float height = 1.5f;
    public static float reach = 2.3f;

    public GameObject cameraChildObject = null;
    public Vector3 cameraChildOffset = Vector3.zero;

    private GameObject playerObject;
    private Transform targetTransform;
    private bool mousePressed = false;
    private bool localIsHovering = false;

#if UNITY_EDITOR
    protected void Start()
    {
        // find Player object
        this.playerObject = GameObject.FindGameObjectsWithTag("Player")[0];  // there should only ever be one GameObject tagged with "Player"
    }

    protected void Update()
    {
        while (isActive && this.playerObject == null)
        {
            Debug.LogWarning("Unable to find DebugPlayer. Retrying...");
            this.playerObject = GameObject.FindGameObjectsWithTag("Player")[0];
        }
        if (isActive && Input.GetKey(KeyCode.F))
        {
            // mouse hover
            Transform cameraTransform = this.playerObject.transform.Find("DebugCamera");
            RaycastHit hit;
            if (cameraTransform == null) this.InitializePlayer();
            Ray ray = cameraTransform.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                // if the cursor moves directly on from a different object
                Transform hitTransform = hit.transform;
                if (hitTransform != null && this.targetTransform != null && !this.targetTransform.Equals(hitTransform))
                {
                    FriendlyInteractable fi = this.targetTransform.GetComponent<FriendlyInteractable>();
                    if (fi != null) fi.DebugExitHover();
                    this.targetTransform = null;
                    this.localIsHovering = false;
                }

                this.targetTransform = hitTransform;
                FriendlyInteractable targetInteractable = hitTransform.GetComponent<FriendlyInteractable>();

                // if the cursor is within reach of an object and is not currently hovering over any
                if (targetInteractable != null && hit.distance < reach && !this.localIsHovering)
                {
                    targetInteractable.DebugEnterHover();
                    this.localIsHovering = true;
                }

                // mouse clicks
                if (targetInteractable != null && this.localIsHovering && Input.GetMouseButtonDown(0) && !this.mousePressed)
                {
                    this.mousePressed = true;
                    targetInteractable.DebugGrab();
                }
                else if (!Input.GetMouseButtonDown(0) && this.mousePressed) this.mousePressed = false;
            }
        }
        else if (this.localIsHovering)
        {
            if (this.targetTransform != null)
            {
                FriendlyInteractable fi = this.targetTransform.GetComponent<FriendlyInteractable>();
                if (fi != null) fi.DebugExitHover();
            }
            this.targetTransform = null;
            this.localIsHovering = false;
        }
    }

    protected void FixedUpdate()
    {
        // mouse movement
        if (isActive && !Input.GetKey(KeyCode.F))
            if (!Input.GetKey(KeyCode.F))
            {
                // look with the mouse
                Transform cameraTransform = this.playerObject.transform.Find("DebugCamera");
                float rotateHorizontal = Input.GetAxis("Mouse X");
                float rotateVertical = Input.GetAxis("Mouse Y");
                this.playerObject.transform.Rotate(Vector3.up, rotateHorizontal * sensitivity, Space.World);
                if (cameraTransform == null) this.InitializePlayer();
                cameraTransform.Rotate(this.playerObject.transform.forward, rotateVertical * sensitivity, Space.World);
            }

        // keyboard input
        Vector3 movement = new Vector3(0, 0, 0);
        if (Input.GetKey(KeyCode.W))
            movement.x += 1;
        if (Input.GetKey(KeyCode.A))
            movement.z += 1;
        if (Input.GetKey(KeyCode.S))
            movement.x -= 1;
        if (Input.GetKey(KeyCode.D))
            movement.z -= 1;

        if (!isActive && !movement.Equals(new Vector3(0, 0, 0)))
            this.InitializePlayer();

        // move the Player
        Vector3 newPosition = Quaternion.Euler(this.playerObject.transform.rotation.eulerAngles) * movement * speed * Time.deltaTime;
        this.playerObject.transform.position += new Vector3(newPosition.x, 0, newPosition.z);

        // >> the following allows for "free flight" of the player <<
        //this.playerObject.transform.Translate(movement * speed * Time.deltaTime, this.playerObject.transform.Find("DebugCamera"));
    }

    private void InitializePlayer()
    {
        // reset the view (direction)
        GameObject.FindGameObjectWithTag("MainCamera").SetActive(false);
        GameObject newCamera = new GameObject("DebugCamera");
        newCamera.AddComponent<AudioListener>();
        newCamera.transform.SetParent(this.playerObject.transform);
        newCamera.AddComponent<Camera>();
        newCamera.transform.rotation = Quaternion.LookRotation(new Vector3(1, 0, 0));

        if (cameraChildObject != null)
        {
            cameraChildObject.transform.SetParent(newCamera.transform);
            cameraChildObject.transform.localPosition = cameraChildOffset;
        }

        // reset the view (height)
        newCamera.transform.Translate(new Vector3(0, height, 0));

        // break out after first movement input
        isActive = true;
        PlayerVisor.FindPlayerCamera();
    }
#endif
}