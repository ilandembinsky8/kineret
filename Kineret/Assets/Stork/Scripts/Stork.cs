using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stork : MonoBehaviour
{
    private Animator stork;
    public GameObject MainCamera;
    public float gravity = 1.0f;
    private Vector3 moveDirection = Vector3.zero;
    CharacterController characterController;

    void Start ()
    {
        stork = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }
	void Update ()
    {
        characterController.Move(moveDirection * Time.deltaTime);
        moveDirection.y = gravity * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.W))
        {
            stork.SetBool("walk", true);
            stork.SetBool("idle", false);
        }
        if ((Input.GetKeyUp(KeyCode.W))||(Input.GetKeyUp(KeyCode.A))||(Input.GetKeyUp(KeyCode.D)))
        {
            stork.SetBool("idle", true);
            stork.SetBool("walk", false);
            stork.SetBool("walkleft", false);
            stork.SetBool("walkright", false);
            stork.SetBool("flyleft", false);
            stork.SetBool("flyright", false);
            stork.SetBool("fly", true);
        }
        if ((stork.GetCurrentAnimatorStateInfo(0).IsName("landing")) || (stork.GetCurrentAnimatorStateInfo(0).IsName("idle")) || (stork.GetCurrentAnimatorStateInfo(0).IsName("takeoff")))
        {
            stork.SetBool("landing", false);
            stork.SetBool("takeoff", false);
            stork.SetBool("standup", false);
        }
        if ((stork.GetCurrentAnimatorStateInfo(0).IsName("fly"))||(stork.GetCurrentAnimatorStateInfo(0).IsName("flyleft"))||(stork.GetCurrentAnimatorStateInfo(0).IsName("flyright")))
        {
            stork.SetBool("idle", false);
            stork.SetBool("walkleft", false);
            stork.SetBool("walkright", false);
        }
        if ((stork.GetCurrentAnimatorStateInfo(0).IsName("walk")) || (stork.GetCurrentAnimatorStateInfo(0).IsName("walkleft")) || (stork.GetCurrentAnimatorStateInfo(0).IsName("walkright")))
        {
            stork.SetBool("fly", false);
            stork.SetBool("flyright", false);
            stork.SetBool("flyleft", false);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            stork.SetBool("takeoff", true);
            stork.SetBool("idle", false);
            stork.SetBool("landing", false);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            if (stork.GetCurrentAnimatorStateInfo(0).IsName("fly"))
            {
                Debug.Log("fly is current");
                stork.SetBool("fly", false);
                stork.SetBool("landing", true);
                stork.SetBool("takeoff", false);
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            stork.SetBool("idle", false);
            stork.SetBool("sitdown", true);
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            stork.SetBool("standup", true);
            stork.SetBool("idle2", false);
            stork.SetBool("sitdown", false);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            stork.SetBool("glide", true);
            stork.SetBool("fly", false);
        }
        if (Input.GetKeyUp(KeyCode.F))
        {
            stork.SetBool("fly", true);
            stork.SetBool("glide", false);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            stork.SetBool("eat", true);
            stork.SetBool("idle", false);
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            stork.SetBool("idle", true);
            stork.SetBool("eat", false);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            stork.SetBool("walkleft", true);
            stork.SetBool("walk", false);
            stork.SetBool("idle", false);
            stork.SetBool("flyleft", true);
            stork.SetBool("flyright", false);
            stork.SetBool("fly", false);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            stork.SetBool("walkright", true);
            stork.SetBool("walk", false);
            stork.SetBool("idle", false);
            stork.SetBool("flyleft", false);
            stork.SetBool("flyright", true);
            stork.SetBool("fly", false);
        }
        if (Input.GetKeyDown("down"))
        {
            MainCamera.GetComponent<CameraFollow>().enabled = false;
        }
        if (Input.GetKeyUp("down"))
        {
            MainCamera.GetComponent<CameraFollow>().enabled = true;
        }
    }
}
