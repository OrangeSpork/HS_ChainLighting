using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HS_ChainLighting
{
    public class ChainLightingGUI : MonoBehaviour
    {

        private static Rect windowRect = new Rect(120, 220, 200, 125);
        private static readonly GUILayoutOption expandLayoutOption = GUILayout.ExpandWidth(true);

        private static GUIStyle labelStyle;
        private static GUIStyle selectedButtonStyle;

        private static bool guiLoaded = false;

        public static ChainLightingGUI Instance;

        private void Awake()
        {
            Instance = this;
            enabled = false;
        }

        public static void Show()
        {
            Instance.enabled = true;
        }

        public static void Hide()
        {
            Instance.enabled = false;
        }

        private void OnGUI()
        {
            if (!guiLoaded)
            {
                labelStyle = new GUIStyle(UnityEngine.GUI.skin.label);
                selectedButtonStyle = new GUIStyle(UnityEngine.GUI.skin.button);

                selectedButtonStyle.fontStyle = FontStyle.Bold;
                selectedButtonStyle.normal.textColor = Color.red;

                labelStyle.alignment = TextAnchor.MiddleRight;
                labelStyle.normal.textColor = Color.white;

                windowRect.x = Mathf.Min(Screen.width - windowRect.width, Mathf.Max(0, windowRect.x));
                windowRect.y = Mathf.Min(Screen.height - windowRect.height, Mathf.Max(0, windowRect.y));

                guiLoaded = true;
            }

            var rect = GUILayout.Window(8726, windowRect, DoDraw, "Chain Lighting");
            windowRect.x = rect.x;
            windowRect.y = rect.y;

            if (windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();
        }


        private void DoDraw(int id)
        {
            GUILayout.BeginVertical();
            {

                // Header
                GUILayout.BeginHorizontal(expandLayoutOption);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close Me", GUILayout.ExpandWidth(false))) Hide();
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                ChainLighting.Instance.LinkActive = GUILayout.Toggle(ChainLighting.Instance.LinkActive, "Link Active");
                GUILayout.Space(5);
                ChainLighting.Instance.ControlEmbeddedLights = GUILayout.Toggle(ChainLighting.Instance.ControlEmbeddedLights, "Control Embedded Lights");
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
