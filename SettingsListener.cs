using IllusionPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HS_ChainLighting
{
    public class SettingsListener : MonoBehaviour
    {
        protected bool LoadSettings()
        {
            ChainLighting.Instance.DefaultLinkActive = ModPrefs.GetBool("ChainLighting", "DefaultLinkActive", false, true);
            ChainLighting.Instance.DefaultControlEmbedded = ModPrefs.GetBool("ChainLighting", "DefaultControlEmbeddedLights", false, true);
            ChainLighting.Instance.Hotkey = ModPrefs.GetString("ChainLighting", "Hotkey", "L", true);
            ChainLighting.Instance.HotkeyMod = ModPrefs.GetString("ChainLighting", "HotkeyMod", "LeftAlt", true);
            ChainLighting.Instance.HotkeyModAlt = ModPrefs.GetString("ChainLighting", "HotkeyModAlt", "RightAlt", true);

            UnityEngine.Debug.Log($"Chain Lighting Settings Updated");

            return true;
        }
    }
}
