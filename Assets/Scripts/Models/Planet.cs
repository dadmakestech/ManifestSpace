﻿using UnityEngine;
using System.Collections;

using System.Collections.Generic;


using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Planet : MonoBehaviour 
{
    public  enum PlanetStateEnum { virgin, colonized, destroyed };

    public float minRotationSpeed;
    public float maxRotationSpeed;
    public bool bool_PlanetVisited = false;
    
    public GameObject SpaceStationPrefab;
    private GameObject _spaceStationInstance;
    private CircleCollider2D _planetCollider;
    public GameObject PlanetGlow;

    public Text planetCount;

    public float float_RotationSpeed;
    int int_RotationDirection;

    PlanetStateEnum _currentPlanetState;
    public PlanetStateEnum CurrentPlanetState
    {
        get { return _currentPlanetState; }
    }

    public bool InitSightlineForHomePlanet = false;

    bool cleanupMode = false;

    public void OnEnable()
    {
        EventManager.StartListening(EventManager.ePlanetsAquiredEvent, levelEndChallenge);
    }

    public void OnDisable()
    {
        EventManager.StopListening(EventManager.ePlanetsAquiredEvent, levelEndChallenge);
    }

    void Start()
    {
        _planetCollider = GetComponent<CircleCollider2D>();
        planetCanvas = GetComponentInChildren<Canvas>();
        int_RotationDirection = (Random.Range(0, 2) == 0) ? -1 : 1;
        float_RotationSpeed = (Random.Range(minRotationSpeed * 100, maxRotationSpeed * 100) / 100) * int_RotationDirection;

    }

    void levelEndChallenge()
    {
        int level = GameManager.SharedInstance.levelIndex;
        
        if((level%5==0 || level==7 || level==10)&&level!=0)
            StartCoroutine(spinSwitch());

        if (level>5 && ((level % 10 == 0) || level%6==0))
            StartCoroutine(speedUp());
    }

    IEnumerator spinSwitch()
    {
        while(true)
        {
            float_RotationSpeed *= -1f;
            yield return new WaitForSeconds(Random.Range(.5f,5f));
        }
    }

    IEnumerator speedUp()
    {
        float_RotationSpeed *= 2;
        yield return null;
    }

    void Update()
    {
        if (planetCount!=null)
        {
            gameObject.transform.Rotate(0, 0, float_RotationSpeed);  //rotate at constant speed
            planetCount.text = HumanCount.ToString();                //display current number of humans
        }

    }
    public Canvas planetCanvas;
    
    /*
    public void OnMouseDown()
    {
        //if(IsPointerOverUIObject(planetCanvas))
        //{
            print("MANIFESTSPACE>> selected planet");
            SelectPlanet();
        //}
        
    }
    */

    /// <summary>
    /// Cast a ray to test if screenPosition is over any UI object in canvas. This is a replacement
    /// for IsPointerOverGameObject() which does not work on Android in 4.6.0f3
    /// </summary>
    private bool IsPointerOverUIObject(Canvas canvas)
    {
        // Referencing this code for GraphicRaycaster https://gist.github.com/stramit/ead7ca1f432f3c0f181f
        // the ray cast appears to require only eventData.position.
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = Input.mousePosition;

        GraphicRaycaster uiRaycaster = canvas.gameObject.GetComponent<GraphicRaycaster>();
        List<RaycastResult> results = new List<RaycastResult>();
        uiRaycaster.Raycast(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    public void SelectPlanet()
    {
        if (!cleanupMode && _currentPlanetState == PlanetStateEnum.colonized)
        {
            MusicPlayer.SharedInstance.playPlanetSelectSFX();
            SetSelectedState(true);
        }
    }

    public void DestroyChildren()
    {
        cleanupMode = true;
        Destroy(_spaceStationInstance);
        StartCoroutine(_destroychildren());
    }

    IEnumerator _destroychildren()
    {
        foreach (Transform child in gameObject.transform)
        {
            if (child != null)
                Destroy(child.gameObject);
            yield return null;
        }
        cleanupMode = false;
    }

    public float PlanetRadius
    {
        get { return _planetCollider.radius * gameObject.transform.localScale.y; }
    }

    int _humanCount;
    public int HumanCount
    {
        get { return _humanCount; }
        set 
        {
            if(value<=0)
            {
                _changeToPlanetState(PlanetStateEnum.virgin);
            }
            else if (value>0 & _humanCount==0)
            {
                _changeToPlanetState(PlanetStateEnum.colonized);
            }

            _humanCount = value; 
        }
    }

    private void _changeToPlanetState(PlanetStateEnum newState)
    {
        switch(newState)
        {
            case PlanetStateEnum.virgin:
            {
                _humanCount = 0;
                
                Destroy(_spaceStationInstance);
                
                if(_currentPlanetState==PlanetStateEnum.colonized)
                {
                    GameManager.SharedInstance.CurrentLevel.ColonizedPlanetCount--;
                    SetSelectedState(false);
                }
                break;
            }
            case PlanetStateEnum.colonized:
            {
                _planetCollider = GetComponent<CircleCollider2D>();
                
                //distance from center = planet radius + 1/4 of space station (25% of station is hidden behind planet)
                // multiplied by planet's local scale since station is a child
                Vector3 stationPosition = transform.position+(transform.up*(_planetCollider.radius+(SpaceStationPrefab.transform.localScale.y*.25f))*gameObject.transform.localScale.y);
                _spaceStationInstance = Instantiate(SpaceStationPrefab,stationPosition, Quaternion.identity) as GameObject; 
                _spaceStationInstance.transform.SetParent(gameObject.transform);

                if(GameManager.SharedInstance.CurrentLevel.ColonizedPlanetCount==1)
                {
                    _spaceStationInstance.GetComponent<SpaceStation>().ConfigureSightLine(true);
                }

                break;
            }
            case PlanetStateEnum.destroyed:
            {
                break;
            }
        }
        _currentPlanetState = newState; 
    }


    #region public methods
    public void LaunchCrew()
    {
        if (_currentPlanetState == PlanetStateEnum.colonized)
        {
            this.gameObject.GetComponentInChildren<SpaceStation>().LaunchHumans(); 
        }
    }


    public void LaunchMissile(bool powerUpEnabled)
    {
        this.gameObject.GetComponentInChildren<SpaceStation>().launchMissiles(powerUpEnabled); 
    }

    public void SetSelectedState(bool selected)
    {
        if (selected)
        {
            GameObject curSelected = GameManager.SharedInstance.CurrentSelectedPlanet;
            
            //turn off previous selection
            if(curSelected!=null)
                curSelected.GetComponent<Planet>().SetSelectedState(false); 
            
            PlanetGlow.SetActive(true); //select new
            
            if(_spaceStationInstance!=null)
                _spaceStationInstance.GetComponent<SpaceStation>().ConfigureSightLine(true); //turn on sightline

            GameManager.SharedInstance.CurrentSelectedPlanet = gameObject; //update game manager reference
        }
        else
        {
            PlanetGlow.SetActive(false); 
            
            if(_spaceStationInstance!=null)
            {
                _spaceStationInstance.GetComponent<SpaceStation>().ConfigureSightLine(false);
            }
        }
    }

    #endregion
}
