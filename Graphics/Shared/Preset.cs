﻿using Graphics.CTAA;
using Graphics.GTAO;
using Graphics.Settings;
using Graphics.Textures;
using MessagePack;
using System;
using System.IO;
using UnityEngine;

namespace Graphics
{
    // TODO: Find better way to save the data... maybe builder? idk...
    [MessagePackObject(keyAsPropertyName: true)]
    public struct Preset
    {
        public GlobalSettings global;
        public CameraSettings camera;
        public LightingSettings lights;
        public PostProcessingSettings pp;
        public SSSSettings sss;
        public SkyboxParams skybox;
        public SkyboxSettings skyboxSetting;
        public GTAOSettings gtao;
        public CTAASettings ctaa;

        public Preset(GlobalSettings global, CameraSettings camera, LightingSettings lights, PostProcessingSettings pp, SkyboxParams skybox, SSSSettings sss)
        {
            this.camera = camera;
            this.global = global;
            this.lights = lights;
            this.pp = pp;
            this.skybox = skybox;
            this.sss = sss;
            this.gtao = GTAOManager.settings;
            this.ctaa = CTAAManager.CTaaSettings;

            // Skybox setting is generated when preset is being saved.
            skyboxSetting = null;
        }

        public void UpdateParameters()
        {
            pp.SaveParameters();
            sss?.SaveParameters();
            gtao = GTAOManager.settings;
            ctaa = CTAAManager.CTaaSettings;
            SkyboxManager manager = Graphics.Instance.SkyboxManager;

            Material mat = manager.Skybox;
            if (mat)
            {
                SkyboxSettings setting = null;

                // Generate Setting Class
                // TODO: Find better way...
                // TODO: Add EnviroSky Support (AI)
                // TODO: Add AIOSky Support (HS2)
                // TODO: Stronger exception handling for different games.
                if (mat.shader.name == ProceduralSkyboxSettings.shaderName) setting = new ProceduralSkyboxSettings();
                else if (mat.shader.name == TwoPointColorSkyboxSettings.shaderName) setting = new TwoPointColorSkyboxSettings();
                else if (mat.shader.name == FourPointGradientSkyboxSetting.shaderName) setting = new FourPointGradientSkyboxSetting();
                else if (mat.shader.name == HemisphereGradientSkyboxSetting.shaderName) setting = new HemisphereGradientSkyboxSetting();

                if (setting != null)
                {
                    setting.Save();
                    skyboxSetting = setting;
                }
            }
            ReflectionProbe defaultProbe = manager.DefaultReflectionProbe();
            if (defaultProbe != null && defaultProbe.intensity > 0)
            {
                lights.DefaultReflectionProbeSettings = new ReflectionProbeSettings();
                lights.DefaultReflectionProbeSettings.FillSettings(manager.DefaultReflectionProbe());
            } 
            else
            {
                lights.DefaultReflectionProbeSettings = null;
            }
            skybox = manager.skyboxParams;

        }
        public byte[] Serialize()
        {
            return MessagePackSerializer.Serialize(this);
        }
        public void Save(string targetPath, bool overwrite = true)
        {          
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            UpdateParameters();
            byte[] bytes = Serialize();
            if (File.Exists(targetPath) && overwrite)
            {
                File.Delete(targetPath);
                File.WriteAllBytes(targetPath, bytes);
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(targetPath), "debug.json"), MessagePackSerializer.ToJson(this));
            }
            else
            {
                File.WriteAllBytes(targetPath, bytes);
            }
        }
        public bool Load(string targetPath, string name)
        {
           if (File.Exists(targetPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(targetPath);
                    Load(bytes);
                    return true;
                }
                catch (Exception e)
                {
                    Graphics.Instance.Log.Log(BepInEx.Logging.LogLevel.Error, string.Format("Couldn't open preset file '{0}' at {1}", name + ".preset", targetPath));
                    Graphics.Instance.Log.Log(BepInEx.Logging.LogLevel.Error, e.Message + "\n" + e.StackTrace);
                    return false;
                }
            }
            else
            {
                Graphics.Instance.Log.Log(BepInEx.Logging.LogLevel.Error, string.Format("Couldn't find preset file '{0}' at {1}", name + ".preset", targetPath));
                return false;
            }
        }

        public void Load(byte[] bytes)
        {
            Deserialize(bytes);
            ApplyParameters();
        }

        public void Deserialize(byte[] bytes)
        {
            this = MessagePackSerializer.Deserialize<Preset>(bytes);
        }

        public void ApplyParameters()
        {
#if DEBUG
            Graphics.Instance.Log.LogInfo($"Applying Parameters");
#endif
            pp.LoadParameters();
#if DEBUG
            Graphics.Instance.Log.LogInfo($"Done with PP");
#endif
            sss?.LoadParameters();
#if DEBUG
            Graphics.Instance.Log.LogInfo($"Done with SSS");
#endif
            GTAOManager.settings = gtao;
            GTAOManager.UpdateSettings();

            if (ctaa == null)
                ctaa = new CTAASettings();

            CTAAManager.CTaaSettings = ctaa;            
            CTAAManager.ApplySetting();

#if DEBUG
            Graphics.Instance.Log.LogInfo($"Done with GTAO && CTAA...");
#endif
            SkyboxManager manager = Graphics.Instance.SkyboxManager;
            if (manager)
            {
                if (skyboxSetting != null)
                    manager.dynSkyboxSetting = skyboxSetting;
                manager.skyboxParams = skybox;
                manager.PresetUpdate = true;
                manager.LoadSkyboxParams();

                manager.SetupDefaultReflectionProbe(Graphics.Instance.LightingSettings);
            }
#if DEBUG
            Graphics.Instance.Log.LogInfo($"Done with skybox");
#endif
            Graphics.Instance.LightingSettings.DefaultReflectionProbeSettings = lights.DefaultReflectionProbeSettings;
#if DEBUG
            Graphics.Instance.Log.LogInfo($"Done with Default RP");
#endif
        }
    }
}
