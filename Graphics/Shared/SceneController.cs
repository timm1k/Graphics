﻿using ExtensibleSaveFormat;
using Graphics.Settings;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MessagePack;
using Studio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Graphics.LightManager;

namespace Graphics
{
    public class SceneController : SceneCustomFunctionController
    {
        public byte[] ExportSettingBytes()
        {
            return MessagePackSerializer.Serialize(DoSave());            
        }

        public void ImportSettingBytes(byte[] bytes)
        {
            DoLoad(MessagePackSerializer.Deserialize<PluginData>(bytes));
        }

        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            PluginData pluginData = GetExtendedData();

            StartCoroutine(LoadCoroutine(operation, pluginData, loadedItems));
        } 
        
        private IEnumerator LoadCoroutine(SceneOperationKind operation, PluginData pluginData, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            yield return null;
            yield return null;
            yield return null;

            if (operation == SceneOperationKind.Import)
            {
                // Copy over light settings from the newly imported lights
                if (pluginData != null && pluginData.data != null && pluginData.data.ContainsKey("lightDataBytes"))
                {
                    PerLightSettings[] settings = MessagePackSerializer.Deserialize<PerLightSettings[]>((byte[])pluginData.data["lightDataBytes"]);
                    if (settings != null && settings.Length > 0)
                        ImportLightSettings(settings, loadedItems);
                }
            }
            else
                DoLoad(pluginData);
        }

        private void DoLoad(PluginData pluginData)
        {
            Graphics parent = Graphics.Instance;
            parent?.PresetManager?.Load(pluginData);

            if (pluginData != null && pluginData.data != null && pluginData.data.ContainsKey("reflectionProbeBytes"))
            {
                ReflectionProbeSettings[] settings = MessagePackSerializer.Deserialize<ReflectionProbeSettings[]>((byte[])pluginData.data["reflectionProbeBytes"]);
                if (settings != null && settings.Length > 0)
                    ApplyReflectionProbeSettings(settings);
            }

            // load scene reflection probe information
            if (pluginData != null && pluginData.data != null && pluginData.data.ContainsKey("containsDefaultReflectionProbeData") && (bool)pluginData.data["containsDefaultReflectionProbeData"])
            {
                Graphics.Instance.SkyboxManager.SetupDefaultReflectionProbe(Graphics.Instance.LightingSettings, true);
            }
            else
            {
                Graphics.Instance.SkyboxManager.SetupDefaultReflectionProbe(Graphics.Instance.LightingSettings, false);
            }

            if (pluginData != null && pluginData.data != null && pluginData.data.ContainsKey("lightDataBytes"))
            {
                PerLightSettings[] settings = MessagePackSerializer.Deserialize<PerLightSettings[]>((byte[])pluginData.data["lightDataBytes"]);
               if (settings != null && settings.Length > 0)
                    ApplyLightSettings(settings);
            }            
        }

        protected override void OnSceneSave()
        {           
            SetExtendedData(DoSave());
        }

        private PluginData DoSave()
        {
            PluginData pluginData = Graphics.Instance?.PresetManager.GetExtendedData();

            if (pluginData != null)
            {
                // add scene reflection probe information
                pluginData.data.Add("reflectionProbeBytes", MessagePackSerializer.Serialize(BuildReflectionProbeSettings()));
                if (Graphics.Instance.SkyboxManager.DefaultReflectionProbe() != null && Graphics.Instance.SkyboxManager.DefaultReflectionProbe().intensity > 0)
                {
                    pluginData.data.Add("containsDefaultReflectionProbeData", true);
                }

                // add per light settings data
                pluginData.data.Add("lightDataBytes", MessagePackSerializer.Serialize(BuildLightSettings()));
            }

            return pluginData;
        }

        private static PerLightSettings FindMappedLightSettings(LightObject light, ReadOnlyDictionary<int, ObjectCtrlInfo> objects, PerLightSettings[] settings)
        {
#if DEBUG
            Graphics.Instance.Log.LogInfo($"Importing Light: {light?.ociLight} {light?.ociLight?.lightInfo?.dicKey} {light?.light?.gameObject?.transform}");
#endif
            if (light.ociLight != null)
            {
                if (objects.Values.Any(oci => oci.GetType() == typeof(OCILight) && oci?.objectInfo?.dicKey == light?.ociLight?.lightInfo?.dicKey))
                {
                    int oldKey = objects.FirstOrDefault(kvp => kvp.Value.GetType() == typeof(OCILight) && kvp.Value?.objectInfo?.dicKey == light?.ociLight?.lightInfo?.dicKey).Key;
                    PerLightSettings setting = settings.FirstOrDefault(s => s.LightId == oldKey);
                    if (setting != null)
                    {
#if DEBUG
                        Graphics.Instance.Log.LogInfo($"Found by dic key Old: {oldKey} New: {light.ociLight.lightInfo.dicKey}");
#endif
                        return setting;
                    }
                }
            }
#if DEBUG
            Graphics.Instance.Log.LogInfo($"No Settings Found");
#endif

            return null;
        }


        // Using a mix of hierarchy and object id key allows us to handle both Object lights and Scene lights
        private static PerLightSettings FindLightSettingsForLight(LightObject light, PerLightSettings[] settings)
        {
#if DEBUG
            Graphics.Instance.Log.LogInfo($"Light: {light?.ociLight} {light?.ociLight?.lightInfo?.dicKey} {light?.light?.gameObject?.transform}");
#endif

            PerLightSettings setting = null;
            if (light.ociLight != null)
            {
                // Try by dic key AND path
                setting = settings.FirstOrDefault(s => s.LightId == light?.ociLight?.lightInfo.dicKey && s.HierarchyPath != null && s.HierarchyPath.Matches(light.light.gameObject.transform));
                if (setting != null)
                {
#if DEBUG
                    Graphics.Instance.Log.LogInfo($"Found by dic key and hierarchy: {setting.LightId}:{setting.HierarchyPath}");
#endif
                    return setting;
                }

                // Try by dic key
                setting = settings.FirstOrDefault(s => s.LightId == light.ociLight.lightInfo.dicKey);
                if (setting != null)
                {
#if DEBUG
                    Graphics.Instance.Log.LogInfo($"Found by dic key only: {setting.LightId}:{setting.HierarchyPath}");
#endif
                    return setting;
                }
            }
            // Try by path
            setting = settings.FirstOrDefault(s => s.HierarchyPath.Matches(light.light.gameObject.transform));
#if DEBUG
            if (setting != null)
                Graphics.Instance.Log.LogInfo($"Found by hierarchy only: {setting.LightId}:{setting.HierarchyPath}");
            else
                Graphics.Instance.Log.LogInfo($"New Light");
#endif
            return setting;
        }

        public static void ImportLightSettings(PerLightSettings[] settings, ReadOnlyDictionary<int, ObjectCtrlInfo> importedObjects)
        {
            LightManager lightManager = Graphics.Instance.LightManager;
            lightManager.Light();
            if (settings.Length > 0 && settings[0].HierarchyPath != null)
            {
                foreach (ObjectCtrlInfo oci in importedObjects.Values)
                {
                    if (oci.GetType() == typeof(OCILight))
                    {
                        LightObject light = lightManager.allLights.FirstOrDefault(li => li.ociLight != null && li.ociLight == (OCILight)oci);
                        if (light != null)
                        {
                            FindMappedLightSettings(light, importedObjects, settings)?.ApplySettings(light);
                        }
                    }
                }             
            }
        }

        public static void ApplyLightSettings(PerLightSettings[] settings)
        {
            LightManager lightManager = Graphics.Instance.LightManager;
            lightManager.Light();
            int counter = 0;

            List<PerLightSettings> newDirectionalLights = new List<PerLightSettings>(settings.Where(pls => pls.Type == (int)LightType.Directional));

            if (settings.Length > 0 && settings[0].HierarchyPath != null)
            {
                foreach (LightObject light in lightManager.DirectionalLights)
                {
                    PerLightSettings setting = FindLightSettingsForLight(light, settings);
                    if (setting != null)
                    {
                        setting.ApplySettings(light);
                        newDirectionalLights.Remove(setting);
                    }
                }

                foreach (LightObject light in lightManager.PointLights)
                {
                    PerLightSettings setting = FindLightSettingsForLight(light, settings);
                    if (setting != null)
                        setting.ApplySettings(light);
                }

                foreach (LightObject light in lightManager.SpotLights)
                {
                    PerLightSettings setting = FindLightSettingsForLight(light, settings);
                    if (setting != null)
                        setting.ApplySettings(light);
                }
            }
            else
            {
                if (counter >= settings.Length)
                    return;

                foreach (LightObject light in lightManager.DirectionalLights)
                {
                    settings[counter++].ApplySettings(light);
                    if (counter >= settings.Length)
                        return;
                }

                foreach (LightObject light in lightManager.PointLights)
                {
                    settings[counter++].ApplySettings(light);
                    if (counter >= settings.Length)
                        return;
                }

                foreach (LightObject light in lightManager.SpotLights)
                {
                    settings[counter++].ApplySettings(light);
                    if (counter >= settings.Length)
                        return;
                }
            }

            if (!KKAPI.Studio.StudioAPI.InsideStudio && newDirectionalLights.Count > 0)
            {
                // Add extra lights if requested
                foreach (PerLightSettings setting in newDirectionalLights)
                {
#if DEBUG
                    Graphics.Instance.Log.LogInfo($"Adding Additional Light {setting.LightName} {setting.HierarchyPath} {setting.LightId}");
#endif

                    GameObject lightGameObject = new GameObject(setting.LightName);
                    Light lightComp = lightGameObject.AddComponent<Light>();
                    lightGameObject.GetComponent<Light>().type = LightType.Directional;
                }
                lightManager.Light();
                foreach (LightObject light in lightManager.DirectionalLights)
                {
                    PerLightSettings setting = FindLightSettingsForLight(light, settings);
                    if (setting != null)
                    {
                        setting.ApplySettings(light);
                    }
                }
            }

            PerLightSettings.FlushAliases();
        }

        public static PerLightSettings[] BuildLightSettings()
        {
            LightManager lightManager = Graphics.Instance.LightManager;
            lightManager.Light();
            PerLightSettings[] settings = new PerLightSettings[lightManager.DirectionalLights.Count + lightManager.PointLights.Count + lightManager.SpotLights.Count];
            int counter = 0;

            foreach (LightObject light in lightManager.DirectionalLights)
            {
                PerLightSettings setting = new PerLightSettings();
                setting.FillSettings(light);
                settings[counter++] = setting;
            }

            foreach (LightObject light in lightManager.PointLights)
            {
                PerLightSettings setting = new PerLightSettings();
                setting.FillSettings(light);
                settings[counter++] = setting;
            }

            foreach (LightObject light in lightManager.SpotLights)
            {
                PerLightSettings setting = new PerLightSettings();
                setting.FillSettings(light);
                settings[counter++] = setting;
            }

            return settings;
        }

        public static void ApplyReflectionProbeSettings(ReflectionProbeSettings[] settings)
        {

            ReflectionProbe[] probes = Graphics.Instance.SkyboxManager.GetReflectinProbes();
            if (probes != null && settings != null && settings.Length > 0)
            {
                if (settings.Length > 0 && settings[0].HierarchyPath == null) 
                {
                    string[] probeNames = settings.Select(s => s.Name).ToArray();
                    foreach (string probeName in probeNames)
                    {
                        int counter = 0;
                        foreach (ReflectionProbeSettings setting in settings.Where(s => s.Name == probeName).ToList())
                        {
                            if (probes.Where(p => p.name == probeName).Skip(counter).Any())
                            {
                                ReflectionProbe probe = probes.Where(p => p.name == probeName).Skip(counter).First();
                                setting.ApplySettings(probe);
                                counter++;
                            }
                        }
                    }
                }
                else
                {
                    foreach (ReflectionProbe probe in probes)
                    {
                        ReflectionProbeSettings setting = settings.FirstOrDefault(s => s.HierarchyPath != null && s.HierarchyPath.Matches(probe.gameObject.transform));
                        if (setting != null)
                            setting.ApplySettings(probe);
                    }
                }
            }
        }

        public static ReflectionProbeSettings[] BuildReflectionProbeSettings()
        {
            ReflectionProbe[] probes = Graphics.Instance.SkyboxManager.GetReflectinProbes();
            if (probes.Length > 0)
            {
                ReflectionProbeSettings[] settings = new ReflectionProbeSettings[probes.Length];
                for (int i = 0; i < probes.Length; i++)
                {
                    settings[i] = new ReflectionProbeSettings();
                    settings[i].FillSettings(probes[i]);
                }
                return settings;
            }
            return null;
        }
    }
}
