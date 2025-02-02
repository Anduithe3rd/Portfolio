using System;
using System.Collections;
using UnityEngine;

public class Flashlight : MonoBehaviour
{

    public Light li;
    public bool on = false;
    public AnimationCurve lightCurve;
    public float intens;
    public float detiorate = 0.005f;

    private bool isAnimating = false;

    public float intensMax = 1.9f;
    public AudioSource crankSound;

    private void Start()
    {
        li.intensity = 0;

    }
    void Update()
    {
        
        li.intensity = intens;
        //when we press F key power 'crank' the light
        if(Input.GetKeyDown(KeyCode.F)){
            StartCoroutine(AnimateIntensity());
            //play cranking sound
            crankSound.Play();

        }    
        //constant deterioration of flashlight power
        if(intens > 0){
            intens -= detiorate * Time.deltaTime;
            if(intens < 0) intens = 0;
        }
    }
    //use of AnimationCurve to simulate how a crank flashlight works

    // - When key is pressed power the flashlight a certain amount, the curve has an arc so that when pressed the intensity will peak for a moment before stabilizing
    // - at a slightly lower intensity to give better feel and add to horror elements
    private IEnumerator AnimateIntensity(){
        //to see when our animation is happening to avoid overlaying the effect
        isAnimating = true;
        
        float duration = lightCurve.keys[lightCurve.length - 1].time;
        float elapsed = 0f;
        float initialIntensity = intens;

        //over a duration of time change the light intesnsity to match that of the animation curve
        while(elapsed < duration){
            elapsed += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float curveValue = lightCurve.Evaluate(normalizedTime);

            intens = Mathf.Clamp(initialIntensity + curveValue, 0, intensMax);

            yield return null;

        }

        isAnimating = false;

    }
}
