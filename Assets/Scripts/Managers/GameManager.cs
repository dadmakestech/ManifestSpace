﻿using UnityEngine;
using System.Collections;

using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject SpawnerObject;
    public AsteroidSpawner asteroidSpawner;

    public int levelIndex;
    public float TotalScore;
    
    public SolarSystemSeed  CurrentLevel;
    public Vector2          CurrentHomePosition;
    public GameObject       CurrentSelectedPlanet;
    
    //this should go to the asteroid spawner script...
    public ArrayList        AsteroidThreatList;

    public enum LevelState {Colonizing,LocatingPortal,Paused,Lost,Won};
    public LevelState CurrentLevelState;
    LevelState _stateBeforePause;

    public int LivesLeft = 2;

    public int landedPlanetTimeAward = 3;

    public bool isPaused = false;

    /// <summary>
    /// TODO: move to its own analytics utility
    /// </summary>
    /// 
    bool _playedWarning = false;
    int  _maxPlanets = 0;
	int  _maxHumans = 0;
    int _totalHumans = 0;
    int _totalPlanets = 0;
    int _totalSystems = 0;
    float _levelStartTime;
    public float TimeRemaining;

    public int TotalHumans  { get { return _totalHumans; }}
    public int TotalPlanets { get { return _totalPlanets; }}
    public int TotalSystems { get { return _totalSystems; }}

    public static GameManager SharedInstance { get { return GameManager.mInstance; }}
    private static GameManager mInstance;

    #region Unity Events
 
    void Awake()
    {
        mInstance = this;
    }
    
    public void OnEnable()
    {
        EventManager.StartListening(EventManager.eSolarSystemDidFinishSpawning, _solarSystemSpawned);
        EventManager.StartListening(EventManager.ePortalEnteredEvent, _levelWinHandler);
        EventManager.StartListening(EventManager.eNextHomeIsReadyEvent, _nextLevelReadyHandler);
        EventManager.StartListening(EventManager.eAsteroidDangerEvent, asteroidThreatBegan);
        EventManager.StartListening(EventManager.eAsteroidDestroyedEvent, asteroidThreadOver);
    }
    
    public void OnDisable()
    {
        EventManager.StopListening(EventManager.eSolarSystemDidFinishSpawning, _solarSystemSpawned);
        EventManager.StartListening(EventManager.ePortalEnteredEvent, _levelWinHandler);
        EventManager.StartListening(EventManager.eNextHomeIsReadyEvent, _nextLevelReadyHandler);
        EventManager.StopListening(EventManager.eAsteroidDangerEvent, asteroidThreatBegan);
        EventManager.StopListening(EventManager.eAsteroidDestroyedEvent, asteroidThreadOver);
    }
    
    public void Start()
    {
        CurrentLevelState = LevelState.Paused;
        AsteroidThreatList = new ArrayList();
        CurrentHomePosition = new Vector2(0, 0);
        levelIndex = 0;

        asteroidSpawner = SpawnerObject.GetComponent<AsteroidSpawner>();

         _initializeSolarSystem();
    }
    
    public void Update()
    {
        if((CurrentLevelState == LevelState.Colonizing || CurrentLevelState == LevelState.LocatingPortal)&&!isPaused)
        {
            TimeRemaining -= Time.deltaTime;
            UserInterface.SharedInstance.DisplayCurrentData(); //update User Interface
            _update_analytics();
            _update_levelcheck();
        }
    }

    public void _update_analytics()
    {
        if (CurrentLevel.HumanPopulation > _maxHumans)
            _maxHumans = CurrentLevel.HumanPopulation;

        if (CurrentLevel.ColonizedPlanetCount > _maxPlanets)
            _maxPlanets = CurrentLevel.ColonizedPlanetCount;
    }
    public void _update_levelcheck()
    {
        if (CurrentLevel.ColonizedPlanetCount >= CurrentLevel.RequiredPlanets && CurrentLevelState == LevelState.Colonizing)
        {
            CurrentLevelState = LevelState.LocatingPortal;
            StartCoroutine(_removeAsteroidThreats());
            MusicPlayer.SharedInstance.planetAchievementSound();
            UserInterface.SharedInstance.DisplayPlanetGoalAchievedImages(true);
            EventManager.PostEvent(EventManager.ePlanetsAquiredEvent);
        }
        if ((CurrentLevelState == LevelState.Colonizing || CurrentLevelState == LevelState.LocatingPortal) && (CurrentLevel.HumanPopulation <= 0) || TimeRemaining<=0)
        {
            _levelLostHandler();
        }
    }
    
    #endregion

    #region Game Events

    void _levelWinHandler()
    {
        //      Analytics stuff     //
        _totalHumans += CurrentLevel.HumanPopulation;
        _totalPlanets += CurrentLevel.ColonizedPlanetCount;
        _totalSystems++;
        UserInterface.SharedInstance.LevelUI.SetActive(false);
        asteroidSpawner.enabled = false;
        UserInterface.SharedInstance.DisplaySessionEndedPanel(true, true);
        CurrentLevelState = LevelState.Won;
        int newLevel = levelIndex + 1;
        
        _transitionToNextLevel(newLevel);
    }
    
    void _levelLostHandler()
    {
        CurrentLevelState = LevelState.Lost;
        _totalHumans  += CurrentLevel.HumanPopulation;
        _totalPlanets += CurrentLevel.ColonizedPlanetCount;
        asteroidSpawner.enabled = false;

        UserInterface.SharedInstance.LevelUI.SetActive(false);

        if(LivesLeft==0) //Game Over
        {
            UserInterface.SharedInstance.DisplayPlanetGoalAchievedImages(false);
            UserInterface.SharedInstance.DisplaySessionEndedPanel(true, false);
        }
        else
        {
            UserInterface.SharedInstance.DisplayRetryDialog();
        }
        
        
        Debug.Log("Lost game, removing asteroids, closing portals, & other shenanigans");
        StartCoroutine(_removeAsteroidThreats());
        GameObject portal = SpawnerObject.GetComponent<PortalSpawner>().CurrentPortal;
        if (portal != null)
            Destroy(portal);
        _transitionToNextLevel(levelIndex);
    }
    
    void _nextLevelReadyHandler()
    {
        asteroidSpawner.enabled = true;
        UserInterface.SharedInstance.LevelUI.SetActive(true);
        CurrentLevelState = LevelState.Colonizing;
        _levelStartTime = Time.time;
        TimeRemaining = CurrentLevel.LevelDuration();
    }

    void _solarSystemSpawned()
    {
        if (levelIndex == 0 && CurrentLevelState!=LevelState.Lost)
        {
            _levelStartTime = Time.time;
            CurrentLevelState = LevelState.Colonizing;
            TimeRemaining = CurrentLevel.LevelDuration();
            Debug.Log(">> First Solar System Spawned");
            UserInterface.SharedInstance.MainCanvas.SetActive(true);
            CurrentLevel.ColonizedPlanetCount = 1;
            
        }
        else if(CurrentLevelState==LevelState.Lost)
        {
            Planet home = SpawnerObject.GetComponent<SolarSystemGenerator>().CurrentHomePlanet().GetComponent<Planet>();
            CurrentLevel.ColonizedPlanetCount = 1;
        }
        else
        {
            Debug.Log(">> Next Solar System Spawned. Index: ["+levelIndex+"]");
        }
    }

    void asteroidThreatBegan()
    {
        Debug.Log("New Asteroid in play area let's warn the user..");
        UserInterface.SharedInstance.AsteroidWarningButton.SetActive(true);
        
    }
    
    void asteroidThreadOver()
    {
        if (AsteroidThreatList.Count == 0)
        {
            Debug.Log("Asteroid was destroyed.. let's supress the warning");
            UserInterface.SharedInstance.AsteroidWarningButton.SetActive(false);
        }
    }

    #endregion

    #region UserInterface Actions

    public void NextLevelButtonTapped()
    {
        UserInterface.SharedInstance.DisplaySessionEndedPanel(false, false);
        if (CurrentLevelState == LevelState.Lost)
        {
            _levelStartTime = Time.time;
            TimeRemaining = CurrentLevel.LevelDuration();
            asteroidSpawner.enabled = true;
            CurrentLevelState = LevelState.Colonizing;
            UserInterface.SharedInstance.MainCanvas.SetActive(true);
            Planet earth = SpawnerObject.GetComponent<SolarSystemGenerator>().CurrentHomePlanet().GetComponent<Planet>();

            Camera.main.GetComponent<MobileCameraControl>().StartPanMode(MobileCameraControl.CameraMode.panHome);
            Invoke("_cleanupOnRetry", 1.0f);
        }
        else if (CurrentLevelState == LevelState.Won) //triggers portal animation
        {
            Camera.main.GetComponent<MobileCameraControl>().StartPanMode(MobileCameraControl.CameraMode.panNewHome);
            UserInterface.SharedInstance.DisplayPlanetGoalAchievedImages(false);
        }
        else
        {
            Debug.Log("this shouldn't happen");
        }
    }

    public void PauseGame()
    {
        EventManager.PostEvent(EventManager.eGamePausedEvent);
        isPaused = true;
    }

    public void ResumeGame()
    {
        EventManager.PostEvent(EventManager.eGameResumeEvent);
        isPaused = false;
    }

    void _cleanupOnRetry()
    {
        SpawnerObject.GetComponent<SolarSystemGenerator>().RunCleanup();
    }

    #endregion

    void _transitionToNextLevel(int newLevelIndex)
    {
        Vector2 nextHomePosition = Random.insideUnitCircle * (CurrentLevel.SolarSystemRadius * 8.0f);
        levelIndex = newLevelIndex;
        CurrentHomePosition = nextHomePosition;
        SolarSystemSeed nextLevel = new SolarSystemSeed(newLevelIndex);
        CurrentLevel = nextLevel;
        _initializeSolarSystem();
    }

    void _initializeSolarSystem()
    {
        Debug.Log("Initializing solar system with index: " + levelIndex);

        GameManager.SharedInstance.CurrentLevel = new SolarSystemSeed(levelIndex);
        SpawnerObject.SetActive(true);

        SolarSystemGenerator planetSeed = SpawnerObject.GetComponent<SolarSystemGenerator>();
        planetSeed.minPlanets     = CurrentLevel.MinPlanetCount;
        planetSeed.maxPlanets     = CurrentLevel.MaxPlanetCount;
        planetSeed.MinimumPlanetDistance = CurrentLevel.MinPlanetDistance;
        planetSeed.MinPlanetScale = CurrentLevel.MinPlanetScale;
        planetSeed.MaxPlanetScale = CurrentLevel.MaxPlanetScale;

        asteroidSpawner.minSpawnInterval = CurrentLevel.AsteroidThreatMinInterval;
        asteroidSpawner.maxSpawnInterval = CurrentLevel.AsteroidThreatMaxInterval;
        
        StartCoroutine(SpawnerObject.GetComponent<SolarSystemGenerator>().GenerateSolarSystem());
        asteroidSpawner.enabled = true;
    }

    IEnumerator _removeAsteroidThreats()
    {
        while (AsteroidThreatList.Count != 0)
        {
            GameObject asteroid = (GameObject)AsteroidThreatList[0];
            Destroy(asteroid);
            AsteroidThreatList.RemoveAt(0);
            yield return null;
        }
    }

}
