using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightingZone : EffectZone
{
    public static List<LightingZone> LightingZones = new List<LightingZone>();

    public class WeightInfo
    {
        public LightingZone Zone;
        public float Weight;
    }
    public static List<WeightInfo> CurZoneWeights = new List<WeightInfo>();
    public static void UpdateZoneWeights(Vector3 _gameCenter)
    {
        CurZoneWeights.Clear();
        foreach (LightingZone zone in LightingZone.LightingZones)
        {
            float zoneWeight = zone.GetZoneWeight(_gameCenter);
            if (zoneWeight > 0.0f)
            {
                CurZoneWeights.Add(new WeightInfo() { Zone = zone, Weight = zoneWeight });
            }
        }
    }

    public int Priority = 0;

    public bool OverrideAmbientLight;
    public LightSettings AmbientLightSettings;
    public bool OverrideKeyLight;
    public LightSettings KeyLightSettings;
    public bool OverrideFillLight;
    public LightSettings FillLightSettings;
    public bool AlwaysNight;

    protected override void Start()
    {
        base.Start();               

        if (OverrideAmbientLight) AmbientLightSettings.UpdateCurves();
        if (OverrideKeyLight) KeyLightSettings.UpdateCurves();
        if (OverrideFillLight) FillLightSettings.UpdateCurves();

        LightingZones.Add(this);
    }
 
    /*
#if UNITY_EDITOR
    protected void Update()
    {
        if (OverrideAmbientLight) AmbientLightSettings.UpdateCurves();
        if (OverrideKeyLight) KeyLightSettings.UpdateCurves();
        if (OverrideFillLight) FillLightSettings.UpdateCurves();
    }
#endif
*/
}
