using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Canvas))]
public class SafeArea : MonoBehaviour
{
    public static UnityEvent onOrientationChange = new UnityEvent();
    public static UnityEvent onResolutionChange = new UnityEvent();
    public static bool IsLandscape { get; private set; }

    private static readonly List<SafeArea> _helpers = new List<SafeArea>();

    private static bool _screenChangeVarsInitialized = false;
    private static ScreenOrientation _lastOrientation = ScreenOrientation.Portrait;
    private static Vector2 _lastResolution = Vector2.zero;
    private static Rect _lastSafeArea = Rect.zero;
    private static Rect CurrentSafeArea => SafeAreaUtil.SafeArea;

    private Canvas _canvas;
    private RectTransform _rectTransform;

    private RectTransform _safeAreaTransform;

    void Awake()
    {
        if (!_helpers.Contains(this))
            _helpers.Add(this);

        _canvas = GetComponent<Canvas>();
        _rectTransform = GetComponent<RectTransform>();

        _safeAreaTransform = transform.Find("SafeArea") as RectTransform;

        if (!_screenChangeVarsInitialized)
        {
            _lastOrientation = Screen.orientation;
            _lastResolution.x = Screen.width;
            _lastResolution.y = Screen.height;
            _lastSafeArea = CurrentSafeArea;

            _screenChangeVarsInitialized = true;
        }
    }

    private void Start()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        if (_helpers[0] != this)
            return;

        if (Application.isMobilePlatform)
        {
            if (Screen.orientation != _lastOrientation)
                OrientationChanged();

            if (CurrentSafeArea != _lastSafeArea)
                SafeAreaChanged();
        }
        else
        {
            //resolution of mobile devices should stay the same always, right?
            // so this check should only happen everywhere else
            if (Screen.width != _lastResolution.x || Screen.height != _lastResolution.y)
                ResolutionChanged();
        }
    }

    void ApplySafeArea()
    {
        if (_safeAreaTransform == null)
            return;

        var safeArea = CurrentSafeArea;

        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= _canvas.pixelRect.width;
        anchorMin.y /= _canvas.pixelRect.height;
        anchorMax.x /= _canvas.pixelRect.width;
        anchorMax.y /= _canvas.pixelRect.height;

        _safeAreaTransform.anchorMin = anchorMin;
        _safeAreaTransform.anchorMax = anchorMax;

        // Debug.Log(
        // "ApplySafeArea:" +
        // "\n Screen.orientation: " + Screen.orientation +
        // #if UNITY_IOS
        // "\n Device.generation: " + UnityEngine.iOS.Device.generation.ToString() +
        // #endif
        // "\n Screen.safeArea.position: " + Screen.safeArea.position.ToString() +
        // "\n Screen.safeArea.size: " + Screen.safeArea.size.ToString() +
        // "\n Screen.width / height: (" + Screen.width.ToString() + ", " + Screen.height.ToString() + ")" +
        // "\n canvas.pixelRect.size: " + canvas.pixelRect.size.ToString() +
        // "\n anchorMin: " + anchorMin.ToString() +
        // "\n anchorMax: " + anchorMax.ToString());
    }

    void OnDestroy()
    {
        if (_helpers != null && _helpers.Contains(this))
            _helpers.Remove(this);
    }

    private static void OrientationChanged()
    {
        //Debug.Log("Orientation changed from " + lastOrientation + " to " + Screen.orientation + " at " + Time.time);

        _lastOrientation = Screen.orientation;
        _lastResolution.x = Screen.width;
        _lastResolution.y = Screen.height;

        IsLandscape = _lastOrientation == ScreenOrientation.LandscapeLeft ||
                      _lastOrientation == ScreenOrientation.LandscapeRight ||
                      _lastOrientation == ScreenOrientation.Landscape;
        onOrientationChange.Invoke();
    }

    private static void ResolutionChanged()
    {
        if (_lastResolution.x == Screen.width && _lastResolution.y == Screen.height)
            return;

        //Debug.Log("Resolution changed from " + lastResolution + " to (" + Screen.width + ", " + Screen.height + ") at " + Time.time);

        _lastResolution.x = Screen.width;
        _lastResolution.y = Screen.height;

        IsLandscape = Screen.width > Screen.height;
        onResolutionChange.Invoke();
    }

    private static void SafeAreaChanged()
    {
        if (_lastSafeArea == CurrentSafeArea)
            return;

        //Debug.Log("Safe Area changed from " + lastSafeArea + " to " + Screen.safeArea.size + " at " + Time.time);

        _lastSafeArea = CurrentSafeArea;

        for (int i = 0; i < _helpers.Count; i++)
        {
            _helpers[i].ApplySafeArea();
        }
    }

    public static Vector2 GetCanvasSize()
    {
        return _helpers[0]._rectTransform.sizeDelta;
    }

    public static Vector2 GetSafeAreaSize()
    {
        for (int i = 0; i < _helpers.Count; i++)
        {
            if (_helpers[i]._safeAreaTransform != null)
            {
                return _helpers[i]._safeAreaTransform.sizeDelta;
            }
        }

        return GetCanvasSize();
    }
}