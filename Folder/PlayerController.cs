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
    // check if we are touching the ground using a raycast
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight + playerHeightOffset, isGround);
        //debugging used to see the raycast and see if it matches the players height
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

        //normalized movement
        moveDir = orient.forward * Forward + orient.right * Side;

        //running out of stamina
        if(stamina <= 0){
            StaminaEmpty();
        }


        //ability to run, uses stamina
        if(Input.GetKey(KeyCode.LeftShift) && stamina > 0 && canSprint && grounded){
            rb.AddForce(moveDir.normalized * sprintSpeed * sprintMultiplier, ForceMode.Force);
            stamina -= staminaMax * staminaDetRate;
            UpdateStaminaChange();
        }
        else{
            //walking speed
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

    //SpeedControl was originally created to make sure the player doesn't move to quickly/slide when on ground
    private void SpeedControl(){

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if(flatVel.magnitude > moveSpeed){
            Vector3 newVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(newVel.x, rb.linearVelocity.y, newVel.z);
        }
    }

    #endregion

    //when our character runs out of stamina begins the recovery process as well as slowing them to punish wasting all stamina
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

    //In charge of tracking our stamina and when it has changed
    private void StaminaTracker(){
        //if our stamina is not max and the character has not ran in the last 3 seconds begin recovering
        if(Time.time - staminaChangeTime >= 3f && stamina < staminaMax){
            stamina += staminaMax * staminaRecovRate;

            //Give our player back the ability to run after recovering a certain amount of stamina
            if(stamina > staminaMax * staminaThresh){
                canSprint = true;
                sprintSpeed = moveSpeed * sprintMultiplier;
            }
        }
        //don't let our stamina go over the maximum
        if(stamina > staminaMax){
            stamina = staminaMax;
        }

    }
    //helps track if our stamina has changed for staminaTracker()
    private void UpdateStaminaChange(){
        if(stamina != lastStamina){
            staminaChangeTime = Time.time;
            lastStamina = stamina;
        }
    }
    
    #endregion


}
