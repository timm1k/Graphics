﻿using Graphics.Settings;
using KKAPI.Studio;
using System.Collections.Generic;
using UnityEngine;
using static Graphics.Inspector.Util;
using static Graphics.LightManager;

namespace Graphics.Inspector
{
    internal static class LightInspector
    {
        private static Vector2 dirLightScrollView;
        private static Vector2 pointLightScrollView;
        private static Vector2 spotLightScrollView;
        private static Vector2 inspectorScrollView;
        private static int customLightIndex = 0;
        private static SEGI segi;
        internal static void Draw(GlobalSettings renderingSettings, LightManager lightManager, bool showAdvanced)
        {
            lightToDestroy = null;
            GUILayout.BeginVertical(GUIStyles.Skin.box);
            {
                if (showAdvanced)
                {
                    Toggle("Use Alloy Light", lightManager.UseAlloyLight, false, useAlloy => lightManager.UseAlloyLight = useAlloy);
                    Toggle("Lights Use Linear Intensity", renderingSettings.LightsUseLinearIntensity, false, useLinear => renderingSettings.LightsUseLinearIntensity = useLinear);
                    Toggle("Lights Use Color Temperature", renderingSettings.LightsUseColorTemperature, false, useTemperature => renderingSettings.LightsUseColorTemperature = useTemperature);
                }
                GUILayout.BeginHorizontal();
                {
                    if (KKAPI.KoikatuAPI.GetCurrentGameMode() != KKAPI.GameMode.Studio)
                    {
                        if (GUILayout.Button("Save Map Lights to Preset"))
                        {
                            Graphics.Instance.PresetManager.SaveMapLights(false);
                        }
                        if (GUILayout.Button("Load Map Lights Preset"))
                        {
                            Graphics.Instance.PresetManager.LoadMapLights(false);
                        }
                        if (GUILayout.Button("Load Default Map Lights"))
                        {
                            Graphics.Instance.PresetManager.LoadMapLights(true);
                            Graphics.Instance.PresetManager.SaveMapLights(false);
                        }
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical(GUIStyles.Skin.box, GUILayout.Width(200), GUILayout.MaxWidth(250));
                    {
                        LightGroup(lightManager, "Directional Lights", LightSettings.LightType.Directional);
                        if (0 < lightManager.DirectionalLights.Count)
                        {
                            GUILayout.BeginVertical(GUIStyles.Skin.box);
                            dirLightScrollView = GUILayout.BeginScrollView(dirLightScrollView);
                            lightManager.DirectionalLights.ForEach(l => LightOverviewModule(lightManager, l));
                            GUILayout.FlexibleSpace();
                            GUILayout.EndScrollView();
                            GUILayout.EndVertical();
                        }
                        LightGroup(lightManager, "Point Lights", LightSettings.LightType.Point);
                        if (0 < lightManager.PointLights.Count)
                        {
                            GUILayout.BeginVertical(GUIStyles.Skin.box);
                            pointLightScrollView = GUILayout.BeginScrollView(pointLightScrollView);
                            lightManager.PointLights.ForEach(l => LightOverviewModule(lightManager, l));
                            GUILayout.FlexibleSpace();
                            GUILayout.EndScrollView();
                            GUILayout.EndVertical();
                        }
                        LightGroup(lightManager, "Spot Lights", LightSettings.LightType.Spot);
                        if (0 < lightManager.SpotLights.Count)
                        {
                            GUILayout.BeginVertical(GUIStyles.Skin.box);
                            spotLightScrollView = GUILayout.BeginScrollView(spotLightScrollView);
                            lightManager.SpotLights.ForEach(l => LightOverviewModule(lightManager, l));
                            GUILayout.FlexibleSpace();
                            GUILayout.EndScrollView();
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(GUIStyles.Skin.box);
                    {
                        if (null != lightManager.SelectedLight)
                        {
                            if (lightManager.SelectedLight.enabled)
                            {
                                AlloyAreaLight alloyLight = null;
                                if (lightManager.UseAlloyLight)
                                {
                                    alloyLight = lightManager.SelectedLight.light.GetComponent<AlloyAreaLight>();
                                }
                                if (Graphics.Instance.IsStudio())
                                {
                                    string currentAliasedName = PerLightSettings.NameForLight(lightManager.SelectedLight.light);
                                    Text("Light Name", currentAliasedName, lightName => {
                                            if (lightName != currentAliasedName && lightName != lightManager.SelectedLight.light.name)
                                            {
                                                PerLightSettings.SetAlias(lightManager.SelectedLight.light, lightName);
                                            }
                                            else if (lightName == lightManager.SelectedLight.light.name && PerLightSettings.AliasedLight(lightManager.SelectedLight.light))
                                            {
                                                PerLightSettings.ClearAlias(lightManager.SelectedLight.light);
                                            }
                                        }, 
                                        !lightManager.SelectedLight.light.transform.IsChildOf(GameObject.Find("StudioScene/Camera").transform));
                                }
                                else
                                {
                                    Label(lightManager.SelectedLight.light.name, "", true);
                                }
                                GUILayout.BeginVertical(GUIStyles.Skin.box);
                                inspectorScrollView = GUILayout.BeginScrollView(inspectorScrollView);
                                {
                                    Label("Colour", "", true);
                                    SliderColor("Colour", lightManager.SelectedLight.color, c => lightManager.SelectedLight.color = c);
                                    if (renderingSettings.LightsUseColorTemperature)
                                    {
                                        Slider("Temperature (K)", lightManager.SelectedLight.light.colorTemperature, 0f, 30000f, "N0", t => lightManager.SelectedLight.light.colorTemperature = t);
                                    }
                                    GUILayout.Space(10);
                                    Label("Shadows", "", true);
                                    Selection("Shadow Type", lightManager.SelectedLight.shadows, type => lightManager.SelectedLight.shadows = type);
                                    Slider("Strength", lightManager.SelectedLight.light.shadowStrength, 0f, 1f, "N2", strength => lightManager.SelectedLight.light.shadowStrength = strength);
                                    if (LightType.Directional == lightManager.SelectedLight.type && renderingSettings.UsePCSS)
                                        Slider("Resolution", lightManager.SelectedLight.ShadowCustomResolution, 0, PCSSLight.MaxShadowCustomResolution, resolution => lightManager.SelectedLight.ShadowCustomResolution = resolution);
                                    else
                                        Selection("Resolution", lightManager.SelectedLight.light.shadowResolution, resolution => lightManager.SelectedLight.light.shadowResolution = resolution, 2);
                                    Slider("Bias", lightManager.SelectedLight.light.shadowBias, 0f, 2f, "N3", bias => lightManager.SelectedLight.light.shadowBias = bias);
                                    Slider("Normal Bias", lightManager.SelectedLight.light.shadowNormalBias, 0f, 3f, "N2", nbias => lightManager.SelectedLight.light.shadowNormalBias = nbias);
                                    Slider("Near Plane", lightManager.SelectedLight.light.shadowNearPlane, 0f, 10f, "N2", np => lightManager.SelectedLight.light.shadowNearPlane = np);
                                    GUILayout.Space(10);
                                    Slider("Intensity", lightManager.SelectedLight.intensity, LightSettings.IntensityMin, LightSettings.IntensityMax, "N2", i => lightManager.SelectedLight.intensity = i);
                                    Slider("Indirect Multiplier", lightManager.SelectedLight.light.bounceIntensity, LightSettings.IntensityMin, LightSettings.IntensityMax, "N0", bi => lightManager.SelectedLight.light.bounceIntensity = bi);
                                    GUILayout.Space(10);
                                    if (lightManager.SelectedLight.type == LightType.Directional)
                                    {
                                        if (null != Graphics.Instance.CameraSettings.MainCamera && null == segi)
                                            segi = Graphics.Instance.CameraSettings.MainCamera.GetComponent<SEGI>();

                                        if (null != segi && segi.enabled)
                                        {
                                            bool isSEGISun = ReferenceEquals(lightManager.SelectedLight.light, segi.sun);
                                            if (null != segi.sun && !isSEGISun)
                                                GUI.enabled = false;
                                            Toggle("SEGI Sun source", isSEGISun, false, sun =>
                                            {
                                                if (sun)
                                                {
                                                    segi.sun = lightManager.SelectedLight.light;
                                                }
                                                else
                                                {
                                                    segi.sun = null;
                                                }
                                            });
                                            GUI.enabled = true;
                                        }

                                        if (lightManager.SelectedLight.light.name != "Cam Light" || !lightManager.SelectedLight.light.transform.IsChildOf(GameObject.Find("StudioScene/Camera").transform))
                                        {
                                            if (!lightManager.SelectedLight.AdditionalCamLight)
                                            {
                                                Vector3 rot = lightManager.SelectedLight.rotation;
                                                Slider("Vertical Rotation", rot.x, LightSettings.RotationXMin, LightSettings.RotationXMax, "N1", x => { rot.x = x; });
                                                Slider("Horizontal Rotation", rot.y, LightSettings.RotationYMin, LightSettings.RotationYMax, "N1", y => { rot.y = y; });

                                                if (rot != lightManager.SelectedLight.rotation)
                                                {
                                                    lightManager.SelectedLight.rotation = rot;
                                                }
                                            }
                                            else
                                            {
                                                if (!StudioAPI.InsideStudio)
                                                {
                                                    GraphicsAdditionalCamLight graphicsAdditionalCamLight = lightManager.SelectedLight.light.gameObject.GetComponent<GraphicsAdditionalCamLight>();
                                                    Vector3 rot = graphicsAdditionalCamLight.OriginalRotation.eulerAngles;
                                                    Slider("Vertical Rotation", rot.x, LightSettings.RotationXMin, LightSettings.RotationXMax, "N1", x => { rot.x = x; });
                                                    Slider("Horizontal Rotation", rot.y, LightSettings.RotationYMin, LightSettings.RotationYMax, "N1", y => { rot.y = y; });

                                                    if (rot != graphicsAdditionalCamLight.OriginalRotation.eulerAngles)
                                                    {
                                                        graphicsAdditionalCamLight.OriginalRotation = Quaternion.Euler(rot);
                                                    }
                                                }
                                                else
                                                {
                                                    Vector3 rot = lightManager.SelectedLight.ociLight.guideObject.changeAmount.rot;
                                                    Slider("Vertical Rotation", rot.x, LightSettings.RotationXMin, LightSettings.RotationXMax, "N1", x => { rot.x = x; });
                                                    Slider("Horizontal Rotation", rot.y, LightSettings.RotationYMin, LightSettings.RotationYMax, "N1", y => { rot.y = y; });

                                                    if (rot != lightManager.SelectedLight.ociLight.guideObject.changeAmount.rot)
                                                    {
                                                        lightManager.SelectedLight.ociLight.guideObject.changeAmount.rot = rot;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Slider("Light Range", lightManager.SelectedLight.range, 0.1f, 500f, "N1", range => { lightManager.SelectedLight.range = range; });
                                        if (lightManager.SelectedLight.type == LightType.Spot)
                                        {
                                            Slider("Spot Angle", lightManager.SelectedLight.spotAngle, 1f, 179f, "N1", angle => { lightManager.SelectedLight.spotAngle = angle; });
                                        }
                                    }
                                    GUILayout.Space(10);
                                    if (lightManager.UseAlloyLight && alloyLight.HasSpecularHighlight && null != alloyLight)
                                    {
                                        Slider("Specular Highlight", alloyLight.Radius, 0f, 1f, "N2", i => alloyLight.Radius = i);

                                        if (lightManager.SelectedLight.type == LightType.Point)
                                        {
                                            Slider("Length", alloyLight.Length, 0f, 1f, "N2", i => alloyLight.Length = i);
                                        }
                                    }
                                    if (!lightManager.SelectedLight.IsNotAdditionalCamAvailable && (lightManager.SelectedLight.type == LightType.Directional || lightManager.SelectedLight.type == LightType.Spot))
                                    {
                                        Toggle("Additional Cam Light", lightManager.SelectedLight.AdditionalCamLight, false, addCamLight => lightManager.SelectedLight.AdditionalCamLight = addCamLight);
                                    }
                                    if (showAdvanced)
                                    {
                                        Selection("Render Mode", lightManager.SelectedLight.light.renderMode, mode => lightManager.SelectedLight.light.renderMode = mode);
                                        SelectionMask("Culling Mask", lightManager.SelectedLight.light.cullingMask, mask => lightManager.SelectedLight.light.cullingMask = mask);
                                    }
                                }
                                GUILayout.EndScrollView();
                                GUILayout.EndVertical();
                                GUILayout.FlexibleSpace();
                            }
                            else
                            {
                                Label("Selected light is disabled.", "");
                            }
                        }
                        else
                        {
                            Label("Select a light source on the left panel.", "");
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            if (lightToDestroy != null)
            {
                GameObject.DestroyImmediate(lightToDestroy.gameObject);
                lightManager.Light();
            }

        }

        private static void LightGroup(LightManager lightManager, string typeName, LightSettings.LightType type)
        {
            GUILayout.BeginHorizontal();
            {
                Label(typeName, "", true);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("All ON"))
                {
                    switch (type)
                    {
                        case LightSettings.LightType.Directional:
                            lightManager.DirectionalLights.ForEach(l => l.enabled = true);
                            break;
                        case LightSettings.LightType.Point:
                            lightManager.PointLights.ForEach(l => l.enabled = true);
                            break;
                        case LightSettings.LightType.Spot:
                            lightManager.SpotLights.ForEach(l => l.enabled = true);
                            break;
                    }
                }
                GUILayout.Space(5);
                if (GUILayout.Button("All OFF"))
                {
                    switch (type)
                    {
                        case LightSettings.LightType.Directional:
                            lightManager.DirectionalLights.ForEach(l => l.enabled = false);
                            break;
                        case LightSettings.LightType.Point:
                            lightManager.PointLights.ForEach(l => l.enabled = false);
                            break;
                        case LightSettings.LightType.Spot:
                            lightManager.SpotLights.ForEach(l => l.enabled = false);
                            break;
                    }
                }
                GUILayout.Space(15);
                if (Graphics.Instance.IsStudio())
                {
                    if (GUILayout.Button("+"))
                    {
                        Singleton<Studio.Studio>.Instance.AddLight((int)type);
                        lightManager.Light();
                    }
                }
                //add custom directional lights in maker
                else if (LightSettings.LightType.Directional == type)
                {
                    if (GUILayout.Button("+"))
                    {
                        customLightIndex += 1;
                        GameObject lightGameObject = new GameObject("(Graphics) Directional Light " + customLightIndex);
                        Light lightComp = lightGameObject.AddComponent<Light>();
                        lightGameObject.GetComponent<Light>().type = LightType.Directional;
                        lightManager.Light();
                    }
                }                
            }
            GUILayout.EndHorizontal();
        }

        private static GameObject lightToDestroy = null;
        private static void LightOverviewModule(LightManager lightManager, LightObject l)
        {
            if (null == l || null == l.light)
            {
                lightManager.Light();
                return;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);

            if (ToggleButton(PerLightSettings.NameForLight(l.light), ReferenceEquals(l, lightManager.SelectedLight), true))
            {
                lightManager.SelectedLight = l;
            }
            GUILayout.FlexibleSpace();
            l.enabled = ToggleButton(l.enabled ? " ON" : "OFF", l.enabled, true);
            if (!KKAPI.Studio.StudioAPI.InsideStudio && l.light.name.StartsWith("(Graphics)"))
            {
                if (GUILayout.Button("-"))
                {
                    lightToDestroy = l.light.gameObject;
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}