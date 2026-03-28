using UnityEngine;
using UnityEngine.UIElements;

namespace OuterWildsModPsi
{
    public class DebugWindow : MonoBehaviour
    {
        public bool show = true;
        private Rect windowRect = new Rect(20, 20, 500, 600);

        // ── Ship ─────────────────────────────
        //public Vector3 pos;
        //public Vector3 vel;
        //public Vector3 acc;
        //public Vector3 force;
        //public Vector3 rotEuler;

        //// ── Orbit ────────────────────────────
        //public bool orbitActive;
        //public string targetName;

        //// ── Reference Frame ──────────────────
        //public Vector3 refPos;
        //public Vector3 refVel;
        //public string refType;
        //public string refFrameName;

        // ── SHIP ─────────────────────────────
        public Vector3 shipPos;
        public Vector3 shipVel;
        public Vector3 shipAcc;
        public Vector3 shipForce;
        public Vector3 shipRotEuler;
        public string  shipRefFrameName;
        public Vector3 shipRelVelWRTRef;

        // ── PLAYER ───────────────────────────
        public Vector3 playerPos;
        public Vector3 playerVel;
        public Vector3 playerCOM;
        public string  playerRefFrame;
        public float   playerMass;
        public string  playerName;

        // ── OBJECTS ──────────────────────────
        public Vector3 sunPos;
        public Vector3 timberPos;
        public Vector3 centerUniPos;
        public Vector3 centerUniVel;

        // ── AUTOPILOT ────────────────────────
        public bool isApproaching;
        public bool arrived;
        public string autoTarget;


        Vector2 scroll;

        //void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.F2))
        //        show = !show;
        //}

        void OnGUI()
        {
            if (show)
                windowRect = GUI.Window(0, windowRect, DrawWindow, "PSI Debug");
        }

        void DrawWindow(int id)
        {
            //scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(500), GUILayout.Height(600));
            //GUILayout.BeginVertical("box");
            //scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(500), GUILayout.Height(680));

            // Row 1
            GUILayout.BeginHorizontal();
            DrawShipSection();
            DrawObjectsSection();
            GUILayout.EndHorizontal();

            // Row 2
            GUILayout.BeginHorizontal();
            DrawPlayerSection();
            DrawAutopilotSection();
            GUILayout.EndHorizontal();

            //GUILayout.EndScrollView();
            GUI.DragWindow();

        }
        void DrawShipSection()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(230));

            GUILayout.Label("<b>SHIP</b>");
            GUILayout.Label($"shipPos: {FormatVec(shipPos)}");
            GUILayout.Label($"shipVel: {FormatVec(shipVel)} | {shipVel.magnitude:F1}");
            GUILayout.Label($"shipAcc: {FormatVec(shipAcc)} | {shipAcc.magnitude:F2}");
            GUILayout.Label($"shipForce: {FormatVec(shipForce)}");
            GUILayout.Label($"shipRotEuler: {FormatVec(shipRotEuler)}");
            GUILayout.Label($"shipRefFrame: {shipRefFrameName}");
            GUILayout.Label($"shipRelVelWRTRef: {FormatVec(shipRelVelWRTRef)}");

            GUILayout.EndVertical();
        }

        void DrawObjectsSection()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(230));

            GUILayout.Label("<b>OBJECTS</b>");
            GUILayout.Label($"sunPos: {FormatVec(sunPos)}");
            GUILayout.Label($"timberPos: {FormatVec(timberPos)}");
            GUILayout.Label($"centerUniPos: {FormatVec(centerUniPos)}");
            GUILayout.Label($"centerUniVel: {FormatVec(centerUniVel)}");

            GUILayout.EndVertical();
        }

        void DrawPlayerSection()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(230));

            GUILayout.Label("<b>PLAYER</b>");
            GUILayout.Label($"playerName: {playerName}");
            GUILayout.Label($"playerPos: {FormatVec(playerPos)}");
            GUILayout.Label($"playerVel: {FormatVec(playerVel)}");
            GUILayout.Label($"playerCOM: {FormatVec(playerCOM)}");
            GUILayout.Label($"playerRefFrame: {playerRefFrame}");
            GUILayout.Label($"playerMass: {playerMass:F1}");

            GUILayout.EndVertical();
        }
        void DrawAutopilotSection()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(230));

            GUILayout.Label("<b>AUTOPILOT</b>");
            GUILayout.Label($"isApproaching: {isApproaching}");
            GUILayout.Label($"arrived: {arrived}");
            GUILayout.Label($"target: {autoTarget}");

            GUILayout.EndVertical();
        }


        string FormatVec(Vector3 v) =>
            $"({v.x:F1}, {v.y:F1}, {v.z:F1})";
    }
}
