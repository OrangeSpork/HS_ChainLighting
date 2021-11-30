using Harmony;
using IllusionPlugin;
using IllusionUtility.GetUtility;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace HS_ChainLighting
{
    public class ChainLighting : IEnhancedPlugin
    {
        public const string PluginName = "ChainLighting";
        public const string GUID = "orange.spork.chainlighting";
        public const string PluginVersion = "1.0.0";

        public static ChainLighting Instance;


        public string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64" };} }

        public string Name => PluginName;

        public string Version => PluginVersion;

        public bool DefaultLinkActive { get; set; } = false;
        public bool DefaultControlEmbedded { get; set; } = false;
        public string Hotkey { get; set; } = "L";
        public string HotkeyMod { get; set; } = "LeftAlt";
        public string HotkeyModAlt { get; set; } = "RightAlt";

        private bool _linkActive;
        public bool LinkActive
        {
            get { return _linkActive; }
            set
            {
                if (_linkActive == value)
                    return;
#if DEBUG
                UnityEngine.Debug.Log($"Chain Lighting: Link Active Set to {value}");
#endif
                _linkActive = value;
                if (_linkActive)
                    UpdateAllLightingStates();
                else
                    ReactivateNonOCILights();
            }
        }

        private bool _controlEmbeddedLights;
        public bool ControlEmbeddedLights
        {
            get { return _controlEmbeddedLights; }
            set
            {
                if (_controlEmbeddedLights == value)
                    return;
#if DEBUG
                UnityEngine.Debug.Log($"Chain Lighting: Control Embedded Lights Set to {value}");
#endif
                _controlEmbeddedLights = value;
                if (_controlEmbeddedLights)
                    UpdateAllLightingStates();
                else if (LinkActive)
                    ReactivateNonOCILights();

            }
        }
        public bool SceneLoading { get; set; } = false;


        public void OnApplicationQuit()
        {
            
        }
       
        public void OnApplicationStart()
        {
            try
            {
#if DEBUG
                UnityEngine.Debug.Log($"Chain Lighting: Starting");
#endif
                Instance = this;

                HSExtSave.HSExtSave.RegisterHandler(PluginName, null, null, OnSceneLoad, OnSceneImport, OnSceneSave, null, null);

#if DEBUG
                UnityEngine.Debug.Log($"Chain Lighting: Registered EXT Save Handlers");
#endif


                HarmonyInstance harmonyInstance = HarmonyInstance.Create(GUID);

                harmonyInstance.Patch(typeof(AddObjectFolder).GetMethod(nameof(AddObjectFolder.Load), new Type[] { typeof(OIFolderInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) }), null, new HarmonyMethod(typeof(ChainLighting).GetMethod(nameof(ChainLighting.RegisterOnVisibleDelegate), AccessTools.all)));
                harmonyInstance.Patch(typeof(AddObjectLight).GetMethod(nameof(AddObjectLight.Load), new Type[] { typeof(OILightInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) }), null, new HarmonyMethod(typeof(ChainLighting).GetMethod(nameof(ChainLighting.RegisterOnVisibleDelegate), AccessTools.all)));

                harmonyInstance.Patch(typeof(OCIChar).GetMethod(nameof(OCIChar.OnVisible)), null, new HarmonyMethod(typeof(ChainLighting).GetMethod(nameof(ChainLighting.UpdateLightingState), AccessTools.all)));
                harmonyInstance.Patch(typeof(OCIFolder).GetMethod(nameof(OCIFolder.OnVisible)), null, new HarmonyMethod(typeof(ChainLighting).GetMethod(nameof(ChainLighting.UpdateLightingState), AccessTools.all)));
                harmonyInstance.Patch(typeof(OCIItem).GetMethod(nameof(OCIItem.OnVisible)), null, new HarmonyMethod(typeof(ChainLighting).GetMethod(nameof(ChainLighting.UpdateLightingState), AccessTools.all)));
                harmonyInstance.Patch(typeof(OCILight).GetMethod(nameof(OCILight.OnVisible)), null, new HarmonyMethod(typeof(ChainLighting).GetMethod(nameof(ChainLighting.UpdateLightingState), AccessTools.all)));

#if DEBUG
                UnityEngine.Debug.Log($"Chain Lighting: Patched");
#endif

                DefaultLinkActive = ModPrefs.GetBool("ChainLighting", "DefaultLinkActive", false, true);
                DefaultControlEmbedded = ModPrefs.GetBool("ChainLighting", "DefaultControlEmbeddedLights", false, true);
                Hotkey = ModPrefs.GetString("ChainLighting", "Hotkey", KeyCode.L.ToString(), true);
                HotkeyMod = ModPrefs.GetString("ChainLighting", "HotkeyMod", KeyCode.LeftAlt.ToString(), true);
                HotkeyModAlt = ModPrefs.GetString("ChainLighting", "HotkeyModAlt", KeyCode.RightAlt.ToString(), true);

#if DEBUG
                UnityEngine.Debug.Log($"Chain Lighting: Preferences Wired");
#endif

                LinkActive = DefaultLinkActive;
                ControlEmbeddedLights = DefaultControlEmbedded;
            }
            catch (Exception errStart) { UnityEngine.Debug.LogError($"Chain Lighting: Error Starting {errStart.Message}\n{errStart.StackTrace}"); }

        }

        private static void UpdateLightingState(ObjectCtrlInfo __instance, bool _visible)
        {
            if (!Instance.LinkActive || __instance?.guideObject?.transformTarget == null)
                return;

            try
            {
                // Look for OCILight Children
                if (__instance.GetType() == typeof(OCILight))
                {
                    // Disable OCILight's this way so they read correctly on the interface
                    OCILight ociLight = (OCILight)__instance;
                    if (Instance.SceneLoading)
                    {
                        ociLight.SetEnable((!_visible || ociLight.light.enabled) && _visible);
                    }
                    else
                    {
                        ociLight.SetEnable(_visible);
                    }
                }

                if (Instance.ControlEmbeddedLights)
                {
                    List<Transform> allStudioObjects = Singleton<Studio.Studio>.Instance.dicObjectCtrl.Values.ToList().Select<ObjectCtrlInfo, Transform>(oci => oci.guideObject.transformTarget).ToList();
                    foreach (Transform child in __instance.guideObject.transformTarget)
                    {
                        RecurseForNonOCILights(child, allStudioObjects, _visible);
                    }
                }
            }
            catch (Exception errLightUpdate) { UnityEngine.Debug.Log($"Chain Lighting: Error updating lighting state: {errLightUpdate.Message}\n{errLightUpdate.StackTrace}"); }
        }

        private static void ReactivateNonOCILights()
        {
            if (!Instance.StudioLoaded)
                return;

            List<Transform> allStudioObjects = Singleton<Studio.Studio>.Instance.dicObjectCtrl.Values.ToList().Select<ObjectCtrlInfo, Transform>(oci => oci.guideObject.transformTarget).ToList();
            foreach (ObjectCtrlInfo oci in Studio.Studio.Instance.dicObjectCtrl.Values)
            {
                if (oci.GetType() == typeof(OCILight))
                    continue;

                foreach (Transform child in oci.guideObject.transformTarget)
                {
                    RecurseForNonOCILights(child, allStudioObjects, true);
                }
            }
        }
        private static void RecurseForNonOCILights(Transform item, List<Transform> allStudioObjects, bool _visible)
        {
            if (allStudioObjects.Contains(item))
            {
                return;
            }

            Light light = item.GetComponent<Light>();
            if (light != null)
            {
                if (Instance.SceneLoading)
                {
                    light.enabled = (!_visible || light.enabled) && _visible;
                }
                else
                {
                    light.enabled = _visible;
                }
            }

            foreach (Transform child in item)
            {
                RecurseForNonOCILights(child, allStudioObjects, _visible);
            }
        }


        private static void RegisterOnVisibleDelegate(ObjectCtrlInfo __result)
        {
            try
            {
                __result.treeNodeObject.onVisible = (TreeNodeObject.OnVisibleFunc)Delegate.Combine(__result.treeNodeObject.onVisible, new TreeNodeObject.OnVisibleFunc(__result.OnVisible));
            }
            catch (Exception registerDelegate)
            {
                UnityEngine.Debug.LogError($"Chain Lighting: {registerDelegate.Message}\n{registerDelegate.StackTrace}");
            }
        }

        public void UpdateAllLightingStates()
        {
            if (!StudioLoaded)
                return;
            try
            {
                foreach (ObjectCtrlInfo oci in Singleton<Studio.Studio>.Instance.dicObjectCtrl.Values)
                {                
                    if (oci.treeNodeObject.parent == null)
                        oci.treeNodeObject.SetVisible(oci.treeNodeObject.visible);
                }
            }
            catch (Exception errLightUpdate) { UnityEngine.Debug.LogError($"Chain Lighting: Error updating ALL lighting states: {errLightUpdate.Message}\n{errLightUpdate.StackTrace}"); }
        }

        private void OnSceneSave(string path, XmlTextWriter writer)
        {
            writer.WriteStartElement("chainlighting");
            writer.WriteAttributeString("linkActive", LinkActive.ToString());
            writer.WriteAttributeString("controlEmbed", ControlEmbeddedLights.ToString());
            writer.WriteEndElement();
        }

        private void OnSceneImport(string path, XmlNode node)
        {
            // Do Nothing
        }

        private void OnSceneLoad(string path, XmlNode node)
        {
            SceneLoading = true;
            LinkActive = false;
            ControlEmbeddedLights = false;
            if (node != null)
            {
                node = node.FirstChild;
                if (bool.TryParse(node.Attributes.GetNamedItem("linkActive").Value, out bool _link))
                    LinkActive = _link;
                else
                    LinkActive = DefaultLinkActive;
                if (bool.TryParse(node.Attributes.GetNamedItem("controlEmbed").Value, out bool _control))
                    ControlEmbeddedLights = _control;
                else
                    ControlEmbeddedLights = DefaultControlEmbedded;
            }
            SceneLoading = false;
        }

        public void OnFixedUpdate()
        {
            
        }

        public void OnLateUpdate()
        {            
        }

        public void OnLevelWasInitialized(int level)
        {

            if (level == 3)
            {
                var gameobject = GameObject.Find(PluginName);
                if (gameobject != null) GameObject.DestroyImmediate(gameobject);
                GameObject go = new GameObject(PluginName);
                go.AddComponent<SettingsListener>();
                go.AddComponent<ChainLightingGUI>();

                UnityEngine.Debug.Log($"Chain Lighting: GUI Online");
                StudioLoaded = true;
            }
        }

        public void OnLevelWasLoaded(int level)
        {
        }

       

        public void OnUpdate()
        {

            try
            {
                if (HotkeyMod == "" || HotkeyMod == "None" || HotkeyModAlt == "" || HotkeyModAlt == "None" || Hotkey == "" || Hotkey == "None")
                    return;

                if ((Input.GetKey((KeyCode)Enum.Parse(typeof(KeyCode), HotkeyMod)) || Input.GetKey((KeyCode)Enum.Parse(typeof(KeyCode), HotkeyModAlt))) && Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), Hotkey)))
                {
                    ChainLightingGUI.Instance.enabled = !ChainLightingGUI.Instance.enabled;
                }
            }
            catch (Exception errInvalidHotkeys) { }
        }

        private bool StudioLoaded = false;

    }
}
