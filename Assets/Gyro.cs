using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
# if PLATFORM_ANDROID
using UnityEngine.Android;
# endif
public class Gyro : MonoBehaviour
{
    private string _gpsStage = "-";
    
    public float latitude;
    public float longitude;
    public float altitude;
    [SerializeField] private Text _text;
    private bool _gpsStopped;
    private bool _permissionRequested;

    private void Start()
    {
        StartCoroutine(Loop());
        DontDestroyOnLoad(gameObject);
    }

    public void RequestPermission()
    {
        Debug.Log("RequestPermission");    
#if UNITY_ANDROID
        Debug.Log("RequestPermission2");    
        Permission.RequestUserPermission(Permission.CoarseLocation);
        _permissionRequested = true;
#endif
    }
    void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"OnApplicationPause ={pauseStatus}");
        // check flag
        if (!pauseStatus && _permissionRequested)
        {
#if UNITY_ANDROID
            // has permission?
            if (Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
            {
                Debug.Log("got permission");
                //do something if got permission
                StartCoroutine(StartLocationService(0));
                _permissionRequested = false;
            }
#endif
        }
    }
    
    public void StartLocation()
    {
        Debug.Log("");
        _gpsStopped = false;
        _gpsStage = "";
        latitude = float.NaN;
        longitude = float.NaN;
        StartCoroutine(StartLocationService(0));
        Input.gyro.enabled = true;
        Input.compass.enabled = true;
    }

    public void StopLocation()
    {
        _gpsStopped = true;
        if(Input.location.status==LocationServiceStatus.Initializing||
           Input.location.status==LocationServiceStatus.Running)
            Input.location.Stop();
    }

    IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            var nl = System.Environment.NewLine;
            _text.text = $"{DateTime.Now} isEByUser={Input.location.isEnabledByUser} status={Input.location.status} {nl}"+
#if UNITY_ANDROID
                         $"hasPermission={Permission.HasUserAuthorizedPermission(Permission.CoarseLocation)}{nl}"+
#endif
                         $"stage={_gpsStage} gpsStopped={_gpsStopped}{nl}la={latitude:F3} lo={longitude:F3} al={altitude:F3}{nl}"+
                         $"q={Input.gyro.attitude.eulerAngles}{nl}deg={Input.compass.magneticHeading:F2}{nl}"+
                        $"tH={Input.compass.trueHeading}{nl}"+
                         $"rawVec={Input.compass.rawVector}{nl}";
        }
    }

    private IEnumerator StartLocationService(float wait)
    {
        yield return new WaitForSeconds(wait);
        _gpsStage = "starting...";
        yield return new WaitForSeconds(0.5f);
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("GPS not enabled");
            _gpsStage = "not enabled";
            yield break;
        }

        _gpsStage = "start";
        // Start service before querying location
        Input.location.Start();
        _gpsStage = "waiting not stopped";
        while (Input.location.status == LocationServiceStatus.Stopped)
        {
            yield return new WaitForSeconds(1);
            Debug.Log("...waiting");
            Input.location.Start();
            //Debug.Log("call Start again---");
        }

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }
        _gpsStage = $"stopped changed to {Input.location.status}";
        // Set locational infomations
        while (!_gpsStopped) {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                latitude = Input.location.lastData.latitude;
                longitude = Input.location.lastData.longitude;
                altitude = Input.location.lastData.altitude;
            }
            yield return new WaitForSeconds(5);
        }
        _gpsStage = "stopped";
    }
    double lastCompassUpdateTime = 0;
}
