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

        if(Input.GetKeyDown(KeyCode.F)){
            StartCoroutine(AnimateIntensity());
            crankSound.Play();

        }

        if(intens > 0){
            intens -= detiorate * Time.deltaTime;
            if(intens < 0) intens = 0;
        }
    }

    private IEnumerator AnimateIntensity(){

        isAnimating = true;
        float duration = lightCurve.keys[lightCurve.length - 1].time;
        float elapsed = 0f;
        float initialIntensity = intens;

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
