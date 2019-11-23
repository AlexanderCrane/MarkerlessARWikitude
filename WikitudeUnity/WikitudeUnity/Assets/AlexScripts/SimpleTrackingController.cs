using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Wikitude;
using System;

using Plane = UnityEngine.Plane;

public class SimpleTrackingController : SampleController
{
    /* The GameObject that contains the UI elements used to initialize instant tracking. */
    public GameObject InitializationControls;

    public GameObject GeometryButton;

    /* The label indicating the current DeviceHeightAboveGround. */
    public Text HeightLabel;

    public InstantTracker Tracker;

    public Button ResetButton;

    public GameObject cube;

    public GameObject point;

    List<GameObject> points = new List<GameObject>();

    List<GameObject> cubes = new List<GameObject>();

    public Material floorMat;

    /* Status bar at the bottom of the screen, indicating if the scene is being tracked or not. */
    public Image ActivityIndicator;

    /* The colors of the bottom status status bar */
    public Color EnabledColor = new Color(0.2f, 0.75f, 0.2f, 0.8f);
    public Color DisabledColor = new Color(1.0f, 0.2f, 0.2f, 0.8f);

    /* Controller that moves the furniture based on user input. */
    private MoveController _moveController;

    /* Renders the grid used when initializing the tracker, indicating the ground plane. */
    private GridRenderer _gridRenderer;

    /* The state in which the tracker currently is. */
    private InstantTrackingState _currentState = InstantTrackingState.Initializing;
    public InstantTrackingState CurrentState {
        get { return _currentState; }
    }
    private bool isInitialized = false;

    private void Awake() {
        Application.targetFrameRate = 60;

        _moveController = GetComponent<MoveController>();
        _gridRenderer = GetComponent<GridRenderer>();
    }

    protected override void Start() {
        base.Start();
        QualitySettings.shadowDistance = 4.0f;

        /* The Wikitude SDK needs to be fully started before we can query for platform assisted tracking support
         * SDK initialization happens during start, so we wait one frame in a coroutine
         */
        StartCoroutine(CheckPlatformAssistedTrackingSupport());
    }

    private IEnumerator CheckPlatformAssistedTrackingSupport() {
        yield return null;
        if (Tracker.SMARTEnabled) {
            Tracker.IsPlatformAssistedTrackingSupported((SmartAvailability smartAvailability) => {
                // print(smartAvailability);
            });
        }
    }

    protected override void Update() {
        base.Update();
        if (_currentState == InstantTrackingState.Initializing) {
            /* Change the color of the grid to indicate if tracking can be started or not. */
            if (Tracker.CanStartTracking()) {
                _gridRenderer.TargetColor = Color.green;
            } else {
                _gridRenderer.TargetColor = GridRenderer.DefaultTargetColor;
            }
        } else {
            _gridRenderer.TargetColor = GridRenderer.DefaultTargetColor;
        }

        if (Input.GetMouseButtonDown(0) && isInitialized){ //GetMouseButtonDown can also be used to detect taps on a touchscreen, like here

            var cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane p = new Plane(Vector3.up, Vector3.zero);
            float enter;
            if (p.Raycast(cameraRay, out enter)) {
                if(points.Count < 4){
                    GameObject newpoint = Instantiate(point, cameraRay.GetPoint(enter), Quaternion.identity);
                    newpoint.gameObject.SetActive(true);
                    points.Add(newpoint);
                }
            }
        }

    }

    #region UI Events
    public void OnInitializeButtonClicked() {
        Tracker.SetState(InstantTrackingState.Tracking);
    }

    public void OnHeightValueChanged(float newHeightValue) {
        HeightLabel.text = string.Format("{0:0.##} m", newHeightValue);
        Tracker.DeviceHeightAboveGround = newHeightValue;
    }

    public void OnResetButtonClicked() {
        Tracker.SetState(InstantTrackingState.Initializing);
        isInitialized = false;
        foreach(GameObject i in cubes){
            GameObject.Destroy(i);   
        }
        cubes = new List<GameObject>();
        foreach(GameObject i in points){
            GameObject.Destroy(i);   
        }
        points = new List<GameObject>();
    }
    #endregion

    #region Tracker Events
    public void OnSceneRecognized(InstantTarget target) {
        SetSceneActive(true);
    }

    public void OnSceneLost(InstantTarget target) {
        SetSceneActive(false);
    }

    private void SetSceneActive(bool active) {

        if (ActivityIndicator) {
            ActivityIndicator.color = active ? EnabledColor : DisabledColor;
        }

        if (_gridRenderer) {
            _gridRenderer.enabled = active;
        }
        isInitialized = true;
    }

    public void OnStateChanged(InstantTrackingState newState) {
        _currentState = newState;
        if (newState == InstantTrackingState.Tracking) {
            if (InitializationControls != null) {
                InitializationControls.SetActive(false);
                GeometryButton.SetActive(true);
            }
            isInitialized = true;
        } else {
            if (InitializationControls != null) {
                InitializationControls.SetActive(true);
                GeometryButton.SetActive(false);
            }
        }
    }

    public void OnError(Error error) {
        PrintError("Instant Tracker error!", error);
    }

    public void OnFailedStateChange(InstantTrackingState failedState, Error error) {
        PrintError("Failed to change state to " + failedState, error);
    }
    #endregion

    #region Game Object Events
    public void BuildGeometry() {
        if(points.Count == 4){
            GeometryButton.SetActive(false);

            foreach(GameObject i in points){
                i.SetActive(false);
            }
            Vector3 center1 = new Vector3(points[0].transform.position.x + points[1].transform.position.x, points[0].transform.position.y + points[1].transform.position.y, points[0].transform.position.z + points[1].transform.position.z) / 2;
            Vector3 center2 = new Vector3(points[2].transform.position.x + points[1].transform.position.x, points[2].transform.position.y + points[1].transform.position.y, points[2].transform.position.z + points[1].transform.position.z) / 2;
            Vector3 center3 = new Vector3(points[2].transform.position.x + points[3].transform.position.x, points[2].transform.position.y + points[3].transform.position.y, points[2].transform.position.z + points[3].transform.position.z) / 2;
            Vector3 center4 = new Vector3(points[0].transform.position.x + points[3].transform.position.x, points[0].transform.position.y + points[3].transform.position.y, points[0].transform.position.z + points[3].transform.position.z) / 2;

            GameObject newcube1 = Instantiate(cube, center1, Quaternion.identity);
            float scaleX = Mathf.Abs(points[0].transform.position.x - points[1].transform.position.x);
            float scaleZ = Mathf.Abs(points[0].transform.position.z - points[1].transform.position.z);
            Vector3 newScale = new Vector3(scaleX, 3, scaleZ);
            newcube1.gameObject.SetActive(true);
            newcube1.transform.localScale = newScale;
            newcube1.transform.position = new Vector3(newcube1.transform.position.x, newcube1.transform.position.y + (newScale.y/2), newcube1.transform.position.z);
            cubes.Add(newcube1);

            GameObject newcube2 = Instantiate(cube, center2, Quaternion.identity);
            scaleX = Mathf.Abs(points[2].transform.position.x - points[1].transform.position.x);
            scaleZ = Mathf.Abs(points[2].transform.position.z - points[1].transform.position.z);
            newScale = new Vector3(scaleX, 3, scaleZ);
            newcube2.gameObject.SetActive(true);
            newcube2.transform.localScale = newScale;
            newcube2.transform.position = new Vector3(newcube2.transform.position.x, newcube2.transform.position.y + (newScale.y/2), newcube2.transform.position.z);
            cubes.Add(newcube2);

            GameObject newcube3 = Instantiate(cube, center3, Quaternion.identity);
            scaleX = Mathf.Abs(points[2].transform.position.x - points[3].transform.position.x);
            scaleZ = Mathf.Abs(points[2].transform.position.z - points[3].transform.position.z);
            newScale = new Vector3(scaleX, 3, scaleZ);
            newcube3.gameObject.SetActive(true);
            newcube3.transform.localScale = newScale;
            newcube3.transform.position = new Vector3(newcube3.transform.position.x, newcube3.transform.position.y + (newScale.y/2), newcube3.transform.position.z);
            cubes.Add(newcube3);

            GameObject newcube4 = Instantiate(cube, center4, Quaternion.identity);
            scaleX = Mathf.Abs(points[0].transform.position.x - points[3].transform.position.x);
            scaleZ = Mathf.Abs(points[0].transform.position.z - points[3].transform.position.z);
            newScale = new Vector3(scaleX, 3, scaleZ);
            newcube4.gameObject.SetActive(true);
            newcube4.transform.localScale = newScale;
            newcube4.transform.position = new Vector3(newcube4.transform.position.x, newcube4.transform.position.y + (newScale.y/2), newcube4.transform.position.z);
            cubes.Add(newcube4);

            GameObject newpoint = Instantiate(point, (center1 + center3)/2, Quaternion.identity);
            newpoint.SetActive(false);
            points.Add(newpoint);
            
            GameObject floorCube = Instantiate(cube, newpoint.transform.position, Quaternion.identity);
            floorCube.SetActive(true);
            cubes.Add(floorCube);

            if(Mathf.Abs(points[2].transform.position.x - points[3].transform.position.x) > Mathf.Abs(points[0].transform.position.x - points[1].transform.position.x)){
                scaleX = Mathf.Abs(points[2].transform.position.x - points[3].transform.position.x);
            } else {
                scaleX = Mathf.Abs(points[0].transform.position.x - points[1].transform.position.x);
            }
            
            if(Mathf.Abs(points[2].transform.position.z - points[1].transform.position.z) > Mathf.Abs(points[0].transform.position.z - points[3].transform.position.z)){
                scaleZ = Mathf.Abs(points[2].transform.position.z - points[1].transform.position.z);
            } else {
                scaleZ = Mathf.Abs(points[0].transform.position.z - points[3].transform.position.z);
            }

            newScale = new Vector3(scaleX, 0.01f, scaleZ);
            floorCube.transform.localScale = newScale;

            floorCube.GetComponent<MeshRenderer>().material = floorMat;

            GameObject ceilingCube = Instantiate(floorCube, new Vector3(newpoint.transform.position.x, newpoint.transform.position.y+3, newpoint.transform.position.z), Quaternion.identity);
            cubes.Add(ceilingCube);
        }
    }
    #endregion
}
