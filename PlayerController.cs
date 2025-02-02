using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using Cinemachine;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Base Settings")]
    [SerializeField] private Transform orient;
    public float groundDrag;
    [SerializeField] private LayerMask isGround;
    [SerializeField] private LayerMask isNarrow;
    


    
    [Header("Movement Settings")]
    [Tooltip("Walking speed of the player (changeable)")]
    public float moveSpeed;

    [Tooltip("Multiplies the walking speed by this number to create sprint speed (changeable)")]
    public float sprintMultiplier = 7f;

    public float narrowSlow = 4;

    [Header("Stamina")]
    public float staminaMax = 200f;
    [Tooltip("Rate at which stamina Recovers")]
    [Range(0.001f, 0.1f)]
    public float staminaRecovRate = 0.02f;
    [Tooltip("Rate at which stamina Deteriorates")]
    [Range(0.001f, 0.1f)]
    public float staminaDetRate = 0.002f;
    [Tooltip("After running out of stamina, % threshold of Max stamina until player can run again")]
    public float staminaThresh = 0.25f;
    

    
    [Header("For debugging")]
    [Tooltip("Checks if player is on the ground or not")]
    [SerializeField] private bool grounded;
    [Tooltip("Current Stamina")]
    public float stamina;
    private bool canSprint = true;
    private float lastStamina;
    private float staminaChangeTime;

    [Tooltip("Current sprinting speed (Based On multiplier)")]
    public float sprintSpeed;

    Vector3 moveDir;
    private float Forward;
    private float Side;

    [Header("Player Height Cast Ray")]
    [Tooltip("To be used for controlling player height for offset - i.e., crouching or jumping.")]
    public float playerHeight = 2.0f;
    public float playerHeightOffset = 0.5f;
    public bool isCrouch = false;

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        stamina = staminaMax;
        sprintSpeed = moveSpeed * sprintMultiplier;
        lastStamina = stamina;
        staminaChangeTime = Time.time;
    }

    private void FixedUpdate() {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight + playerHeightOffset, isGround);
        UnityEngine.Debug.DrawRay(transform.position, Vector3.down * 0.5f, Color.green);
        Forward = Input.GetAxisRaw("Vertical");
        Side = Input.GetAxisRaw("Horizontal");

        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }
        //Added narrow slowness via lineardampaening also added that you have to be grounded to run so they cant run in these areas
        else if(Physics.Raycast(transform.position, Vector3.down, playerHeight + playerHeightOffset, isNarrow)){
            rb.linearDamping= narrowSlow;
        }
        else
        {
            rb.linearDamping = 0;
        }

        SpeedControl();
        Moving();
        StaminaTracker();
    }


    #region  Movement
    private void Moving(){

        
        moveDir = orient.forward * Forward + orient.right * Side;

        if(stamina <= 0){
            StaminaEmpty();
        }


        
        if(Input.GetKey(KeyCode.LeftShift) && stamina > 0 && canSprint && grounded){
            rb.AddForce(moveDir.normalized * sprintSpeed * sprintMultiplier, ForceMode.Force);
            stamina -= staminaMax * staminaDetRate;
            UpdateStaminaChange();
        }
        else{
            rb.AddForce(moveDir.normalized * moveSpeed * sprintMultiplier, ForceMode.Force);
        }

        //May want to replace with an InputService Axis, otherwise have a variable that permits movement that can be toggled...
        if (Input.GetKey(KeyCode.LeftControl) && !isCrouch)
        {
            isCrouch = true;
            //Player was not crouching, now is!
            playerHeight = 0.5f;
            transform.localScale = new Vector3(1.0f, 0.5f, 1.0f);
            transform.position -= new Vector3(0.0f, 0.5f, 0.0f);
            //Bad, bad, bad! Change to using a raycast to determine offset from the floor an adjust that accordingly.
            //As is, we are subject to physics nonesense if we are on a slope.



        } else if(!Input.GetKey(KeyCode.LeftControl) && isCrouch)
        {
            //PLUS equal:

            playerHeight = 1.0f;
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            transform.position += new Vector3(0.0f, 0.5f, 0.0f);

            isCrouch = false;
            //Player was crouching, now is not!

        }
    }

    //SpeedControl was originally created to make sure the player doesnt move to quickly/slide 
    private void SpeedControl(){

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if(flatVel.magnitude > moveSpeed){
            Vector3 newVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(newVel.x, rb.linearVelocity.y, newVel.z);
        }
    }

    #endregion

    #region Stamina
    private void StaminaEmpty(){
        stamina = 0;
        canSprint = false;
        sprintSpeed = moveSpeed;
        StartCoroutine(StaminaCooldown());

    }

    private IEnumerator StaminaCooldown(){
        yield return new WaitForSecondsRealtime(3);
        UpdateStaminaChange();
    }

    private void StaminaTracker(){

        if(Time.time - staminaChangeTime >= 3f && stamina < staminaMax){
            stamina += staminaMax * staminaRecovRate;


            if(stamina > staminaMax * staminaThresh){
                canSprint = true;
                sprintSpeed = moveSpeed * sprintMultiplier;
            }
        }

        if(stamina > staminaMax){
            stamina = staminaMax;
        }

    }

    private void UpdateStaminaChange(){
        if(stamina != lastStamina){
            staminaChangeTime = Time.time;
            lastStamina = stamina;
        }
    }
    
    #endregion


}
