using UnityEngine;
using UnityEngine.UIElements;

namespace OuterWildsModPsi
{
    public class DebugWindow : MonoBehaviour
    {
        public bool show = true;
        private Rect windowRect = new Rect(20, 20, 500, 600);

        // ── Ship ─────────────────────────────
        public Vector3 pos;
        public Vector3 vel;
        public Vector3 acc;
        public Vector3 force;
        public Vector3 rotEuler;

        // ── Orbit ────────────────────────────
        public bool orbitActive;
        public string targetName;

        // ── Reference Frame ──────────────────
        public Vector3 refPos;
        public Vector3 refVel;
        public string refType;
        public string refFrameName;
        Vector2 scroll;
        public string refObj;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
                show = !show;
        }

        void OnGUI()
        {
            if (show)
                windowRect = GUI.Window(0, windowRect, DrawWindow, "PSI Debug");
        }

        void DrawWindow(int id)
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(400), GUILayout.Height(600));
            GUILayout.BeginVertical("box");

            // ── SHIP ─────────────────────────
            GUILayout.Label("<b>SHIP</b>");
            GUILayout.Label($"Pos   : {FormatVec(pos)} | {pos.magnitude:F0}");
            GUILayout.Label($"Vel   : {FormatVec(vel)} | {vel.magnitude:F1} m/s");
            GUILayout.Label($"Accel : {FormatVec(acc)} | {acc.magnitude:F2}");
            GUILayout.Label($"Force : {FormatVec(force)} | {force.magnitude:F2}");
            GUILayout.Label($"Rot   : ({rotEuler.x:F1}, {rotEuler.y:F1}, {rotEuler.z:F1})");

            GUILayout.Space(5);

            // ── ORBIT ────────────────────────
            GUILayout.Label("<b>ORBIT</b>");
            GUILayout.Label($"State : {(orbitActive ? "ACTIVE" : "OFF")}");
            GUILayout.Label($"Target: {targetName}");

            GUILayout.Space(5);

            // ── REF FRAME ────────────────────
            GUILayout.Label("<b>SHIP REFERENCE FRAME</b>");
            GUILayout.Label($"Pos   : {FormatVec(refPos)}");
            GUILayout.Label($"Vel   : {FormatVec(refVel)} | {refVel.magnitude:F1}");
            GUILayout.Label($"Type  : {refType}");
            GUILayout.Label($"Obj   : {refFrameName}");
            GUILayout.Label($"Obj   : {refObj}");

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        string FormatVec(Vector3 v) =>
            $"({v.x:F1}, {v.y:F1}, {v.z:F1})";
    }
}