﻿using UnityEngine;
using System.Collections;

using UnityEngine.UI;

public class ThreatIndicator : MonoBehaviour 
{
    public void Start()
    {
        GetComponent<Image>().enabled = false;
    }

    public void Update()
    {
    }

    public void OnEnable()
    {
     //   EventManager.StartListening(EventManager.eAsteroidSpawnedEvent, newAsteroid);
     //   EventManager.StartListening(EventManager.eAsteroidDestroyedEvent, lessAsteroids);
    }

    public void OnDisable()
    {
     //   EventManager.StopListening(EventManager.eAsteroidSpawnedEvent, newAsteroid);
     //   EventManager.StopListening(EventManager.eAsteroidDestroyedEvent, lessAsteroids);
    }

    public void newAsteroid()
    {
      //  GetComponent<Image>().enabled = true;
    }

    public void lessAsteroids()
    {
       // if (GameManager.SharedInstance.asteroidQueue.Count == 0 || GameManager.SharedInstance.asteroidQueue==null)
        //GetComponent<Image>().enabled = false;
    }
}
