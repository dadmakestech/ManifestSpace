﻿using UnityEngine;
using System.Collections;

using UnityEngine.UI;

public class RadialFill : MonoBehaviour 
{
    bool fill;
    bool instant;
    Image imageComponent;

    int state = 0;

    bool rechargeAnim = false;

	// Use this for initialization
	void Start () 
    {
        imageComponent = GetComponent<Image>();
	}
	
	// Update is called once per frame
	void Update () 
    {
        if(state==0)
        {
            imageComponent.fillAmount = 0.0f; 
        }
        else if(state==1)
        {
            imageComponent.fillAmount += Mathf.Lerp(0, 1, 3f * Time.deltaTime);
        }
        else if (state==2)
        {
            if (instant)
            {
                imageComponent.fillAmount = 0;
                state = 0;
            }
            else
            {
                imageComponent.fillAmount -= Mathf.Lerp(1, 0, .5f * Time.deltaTime);

                if (imageComponent.fillAmount <= 0)
                    state = 0;
            }

            if(rechargeAnim&& imageComponent.fillAmount ==0)
            {
                state = 1;
            }
        }
        else if(state==3)
        {
            rechargeAnim = true;
            state = 2;
            instant = false;
        }
	}

    public void RechargeFillAnimation()
    {
        state = 3;
    }

    public void StartRadialFill()
    {
        state = 1;
    }

    public void StopRadialFill(bool instant)
    {
        state = 2;
        this.instant = instant;
    }

    public bool ProgressBarFull()
    {
        return imageComponent.fillAmount == 1; 
    }

}
