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
    public static Gyro Instance { set; get; }
    private string _gpsStage = "-";
    
    GameObject dialog = null;
    
    public float latitude;
    public float longitude;
    public float altitude;
    [SerializeField] private Text _text;
    [SerializeField] private GameObject FlatNorthMark;
    [SerializeField] private GameObject MulMark;
    [SerializeField] private GameObject GyroMark;
    [SerializeField] private GameObject GyroOldMark;
    private bool _gpsStopped;
    private bool _permissionRequested;
    private void Start()
    {
        Instance = this;
        StartCoroutine(Loop());
        DontDestroyOnLoad(gameObject);
// #if PLATFORM_ANDROID
//         if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
//         {
//             Permission.RequestUserPermission(Permission.CoarseLocation);
//             dialog = new GameObject();
//         }
// #endif
    }
//     void OnGUI()
//     {
// #if PLATFORM_ANDROID
//         if(!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
//         {
//             //ユーザーがマイクを使用する権限を拒否しました。
//             //なぜそれが必要なのかを説明するメッセージを Yes/No ボタンとともに表示します。
//             //ユーザーが Yes と答えると、リクエストを再度表示します。
//             //ここにダイアログを表示します。
//             dialog.AddComponent<PermissionsRationaleDialog>();
//             return;
//         }
//         else if(dialog != null)
//         {
//             Destroy(dialog);
//         }
// #endif
    //
    //     //これで、マイクを使う作業を行うことができます。
    // }
    
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
            // var q=Quaternion.LookRotation(Input.compass.rawVector,-Input.gyro.gravity);
            // RawVectorMark.gameObject.transform.rotation = q;
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
        // // Service didn't initialize in 20 seconds
        // if (maxWait <= 0)
        // {
        //     Debug.Log("Timed out");
        //     _gpsStage = "timeout";
        //     yield break;
        // }
        // _gpsStage = "initialized";
        // // Connection has failed
        // if (Input.location.status == LocationServiceStatus.Failed)
        // {
        //     Debug.Log("Unable to determine device location");
        //     _gpsStage = "failed";
        //     yield break;
        // }
        _gpsStage = $"stopped changed to {Input.location.status}";
        // Set locational infomations
        while (!_gpsStopped) {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                latitude = Input.location.lastData.latitude;
                longitude = Input.location.lastData.longitude;
                //_gpsStage = $"lat={latitude:F2} long={longitude:F2}";
                altitude = Input.location.lastData.altitude;
            }
            yield return new WaitForSeconds(5);
        }

        _gpsStage = "stopped";
    }
    
    double lastCompassUpdateTime = 0;
    Quaternion correction = Quaternion.identity;
    Quaternion targetCorrection = Quaternion.identity;

    // Androidの場合はScreen.orientationに応じてrawVectorの軸を変換
    static Vector3 compassRawVector
    {
        get
        {
            Vector3 ret = Input.compass.rawVector;
            
            if(Application.platform == RuntimePlatform.Android)
            {
                switch(Screen.orientation)
                {
                    case ScreenOrientation.LandscapeLeft:
                        ret = new Vector3(-ret.y, ret.x, ret.z);
                        break;
                    case ScreenOrientation.LandscapeRight:
                        ret = new Vector3(ret.y, -ret.x, ret.z);
                        break;
                    case ScreenOrientation.PortraitUpsideDown:
                        ret = new Vector3(-ret.x, -ret.y, ret.z);
                        break;
                }
            }
            
            return ret;
        }
    }
    
    // Quaternionの各要素がNaNもしくはInfinityかどうかチェック
    static bool isNaN(Quaternion q)
    {
        bool ret = 
            float.IsNaN(q.x) || float.IsNaN(q.y) || 
            float.IsNaN(q.z) || float.IsNaN(q.w) || 
            float.IsInfinity(q.x) || float.IsInfinity(q.y) || 
            float.IsInfinity(q.z) || float.IsInfinity(q.w);
        
        return ret;
    }
    
    static Quaternion changeAxis(Quaternion q)
    {
        return new Quaternion(-q.x, -q.y, q.z, q.w);
    }
    
    // void Update () 
    // {
    //         
    //         Quaternion gorientation = changeAxis(Input.gyro.attitude);
    //         GyroOldMark.transform.rotation = Input.gyro.attitude;
    //         GyroMark.transform.rotation = gorientation;
    //         if (Input.compass.timestamp > lastCompassUpdateTime)
    //         {
    //             lastCompassUpdateTime = Input.compass.timestamp;
    //
    //             Vector3 gravity = Input.gyro.gravity.normalized;
    //             Vector3 rawvector = compassRawVector;
    //             Vector3 flatnorth = rawvector - 
    //                 Vector3.Dot(gravity, rawvector) * gravity;
    //             FlatNorthMark.transform.rotation = Quaternion.LookRotation(flatnorth, -gravity);
    //             Quaternion corientation = changeAxis(
    //                 Quaternion.Inverse(
    //                     Quaternion.LookRotation(flatnorth, -gravity)));
    //
    //             // +zを北にするためQuaternion.Euler(0,0,180)を入れる。
    //             Quaternion tcorrection = corientation *
    //                                      Quaternion.Inverse(gorientation);// *Quaternion.Euler(0, 0, 180);
    //             MulMark.transform.rotation = tcorrection;
    //             // 計算結果が異常値になったらエラー
    //             // そうでない場合のみtargetCorrectionを更新する。
    //             if(!isNaN(tcorrection))
    //                 targetCorrection = tcorrection;
    //         }
    //
    //         if (Quaternion.Angle(correction, targetCorrection) < 45)
    //         {
    //             correction = Quaternion.Slerp(
    //                 correction, targetCorrection, 0.02f);
    //         }
    //         else 
    //             correction = targetCorrection;
    //
    //         transform.localRotation = correction * gorientation;        
    // }

}
