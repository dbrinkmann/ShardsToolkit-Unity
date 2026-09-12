using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class LightSettings
{
    GameObject LightReference;

    public Color DaylightColor;
    public float DaylightIntensity;
    public Color SunsetColor;
    public float SunsetIntensity;
    public Color NighttimeColor;
    public float NighttimeIntensity;
    public AnimationCurve IntensityCurve { get { return intensityCurve; } }
    public AnimationCurve RedCurve { get { return redCurve; } }
    public AnimationCurve GreenCurve { get { return greenCurve; } }
    public AnimationCurve BlueCurve { get { return blueCurve; } }    

    public void UpdateCurves()
    {
        intensityCurve = new AnimationCurve(new Keyframe[3] { new Keyframe(0.0f, NighttimeIntensity), new Keyframe(0.66f, SunsetIntensity), new Keyframe(1.0f, DaylightIntensity) });
        redCurve = new AnimationCurve(new Keyframe[3] { new Keyframe(0.0f, NighttimeColor.r), new Keyframe(0.66f, SunsetColor.r), new Keyframe(1.0f, DaylightColor.r) });
        greenCurve = new AnimationCurve(new Keyframe[3] { new Keyframe(0.0f, NighttimeColor.g), new Keyframe(0.66f, SunsetColor.g), new Keyframe(1.0f, DaylightColor.g) });
        blueCurve = new AnimationCurve(new Keyframe[3] { new Keyframe(0.0f, NighttimeColor.b), new Keyframe(0.66f, SunsetColor.b), new Keyframe(1.0f, DaylightColor.b) });
    }

    public override bool Equals(object obj)
    {
        return ((LightSettings)obj).DaylightColor == DaylightColor && ((LightSettings)obj).DaylightIntensity == DaylightIntensity
            && ((LightSettings)obj).SunsetColor == SunsetColor && ((LightSettings)obj).SunsetIntensity == SunsetIntensity
            && ((LightSettings)obj).NighttimeColor == NighttimeColor && ((LightSettings)obj).NighttimeIntensity == NighttimeIntensity;
    }

    private AnimationCurve intensityCurve;
    private AnimationCurve redCurve;
    private AnimationCurve greenCurve;
    private AnimationCurve blueCurve;
}

public class DayNightCycle : MonoBehaviour 
{
    public static DayNightCycle Instance;

    public Transform GameCenter;

    public Light KeyLight;
    public Transform SunPivot;
    public Light MoonLight;
    
    //public Light FillLight;
    
    public Material[] SkyboxMaterials;    

    public LightSettings AmbientLightSettings = new LightSettings()
    {
        DaylightColor = new Color(0.47f, 0.46f, 0.57f),
        SunsetColor = new Color(0.64f, 0.42f, 0.91f),
        NighttimeColor = new Color(0.03f, 0.01f, 0.22f),

        DaylightIntensity = 1.0f,
        SunsetIntensity = 1.0f,
        NighttimeIntensity = 1.0f,
    };

    public LightSettings KeyLightSettings = new LightSettings()
    {
        DaylightColor = new Color(0.88f, 0.79f, 0.62f),
        SunsetColor = new Color(0.5f, 0.24f, 0.24f),
        NighttimeColor = new Color(0.26f, 0.39f, 0.52f),

        DaylightIntensity = 0.9f,
        SunsetIntensity = 1.2f,
        NighttimeIntensity = 0.6f,
    };

    public LightSettings FillLightSettings = new LightSettings()
    {
        DaylightColor = new Color(0.5f, 0.53f, 0.67f),
        SunsetColor = new Color(0.89f, 0.80f, 0.0f),
        NighttimeColor = new Color(0.0f, 0.0f, 0.0f),

        DaylightIntensity = 0.03f,
        SunsetIntensity = 0.02f,
        NighttimeIntensity = 0.0f,
    };

    public bool AlwaysNight = false;

	public double dayDurationSecs=10;
	public double nightDurationSecs=10;
	public double transitionSpeed = 0.2;
    public float LightSunsetTime = 0.75f;
    public float LightSunriseTime = 0.95f;
    public float LightMoonsetTime = 0.975f;
    public float LightMoonriseTime = 0.7f;
    public float LightMoonlightIntensity = 0.8f;
    public float LightMoonlightRampTime = 0.05f;
    public bool ShowLightingDebug = false;
	public double currentTimeSecs;
    
    public List<DayNightObject> DayNightObjects = new List<DayNightObject>();

    public void AddDayNightObject(DayNightObject target)
    {
        DayNightObjects.Add(target);        
    }

    public void ToggleSkybox()
    {
        
    }

    private void Awake()
    {
        Instance = this;
        
    }
    
    // Use this for initialization
    void Start () 
	{
        
	}

	// Update is called once per frame
	void Update () 
	{
        
    }    
}
