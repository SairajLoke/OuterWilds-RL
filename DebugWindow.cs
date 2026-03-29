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
        public Vector3 _shipPosition;
        public Vector3 _shipVelocity;
        public Vector3 _shipAcceleration;
        public Vector3 _shipForce;
        public Vector3 _shipRotationEuler;
        public string _shipCurrRefFrameRigBodyName;
        public Vector3 _shipRelVelWRTCurrRefFrameRigBody;

        // ── PLAYER ───────────────────────────
        public Vector3 _playerPositionLocator;
        public Vector3 _playerVelocityLocator;
        public Vector3 _playerWorldCOMLocator;
        public string _playerCurrRefFrameNameLocator;
        public float _playerMassLocator;
        public string _playerBodyNamelocator;

        // ── OBJECTS ──────────────────────────
        public Vector3 _sunPos;
        public Vector3 _timberPos;
        public Vector3 _centerofUniPos;
        public Vector3 _centerofUniVel;

        // ── AUTOPILOT ────────────────────────
        public bool _isApproachingDestination;
        public bool _arrivedAtDesti;
        public string _autoTarget;
        public Vector3 _orbVel; 


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
            GUILayout.Label($"_shipPosition: {FormatVec(_shipPosition)}");
            GUILayout.Label($"_shipVelocity: {FormatVec(_shipVelocity)} | {_shipVelocity.magnitude:F1}");
            GUILayout.Label($"_shipAcceleration: {FormatVec(_shipAcceleration)} | {_shipAcceleration.magnitude:F2}");
            GUILayout.Label($"_shipForce: {FormatVec(_shipForce)}");
            GUILayout.Label($"_shipRotationEuler: {FormatVec(_shipRotationEuler)}");
            GUILayout.Label($"_shipCurrRefFrameRigBodyName: {_shipCurrRefFrameRigBodyName}");
            GUILayout.Label($"_shipRelVelWRTCurrRefFrameRigBody: {FormatVec(_shipRelVelWRTCurrRefFrameRigBody)}");

            GUILayout.EndVertical();
        }

        void DrawObjectsSection()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(230));

            GUILayout.Label("<b>OBJECTS</b>");
            GUILayout.Label($"_sunPos: {FormatVec(_sunPos)}");
            GUILayout.Label($"_timberPos: {FormatVec(_timberPos)}");
            GUILayout.Label($"_centerofUniPos: {FormatVec(_centerofUniPos)}");
            GUILayout.Label($"_centerofUniVel: {FormatVec(_centerofUniVel)}");

            GUILayout.EndVertical();
        }

        void DrawPlayerSection()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(230));

            GUILayout.Label("<b>PLAYER</b>");
            GUILayout.Label($"_playerBodyNamelocator: {_playerBodyNamelocator}");
            GUILayout.Label($"_playerPositionLocator: {FormatVec(_playerPositionLocator)}");
            GUILayout.Label($"_playerVelocityLocator: {FormatVec(_playerVelocityLocator)}");
            GUILayout.Label($"_playerWorldCOMLocator: {FormatVec(_playerWorldCOMLocator)}");
            GUILayout.Label($"_playerCurrRefFrameNameLocator: {_playerCurrRefFrameNameLocator}");
            GUILayout.Label($"_playerMassLocator: {_playerMassLocator:F1}");

            GUILayout.EndVertical();
        }
        void DrawAutopilotSection()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(230));

            GUILayout.Label("<b>AUTOPILOT</b>");
            GUILayout.Label($"_isApproachingDestination: {_isApproachingDestination}");
            GUILayout.Label($"_arrivedAtDesti: {_arrivedAtDesti}");
            GUILayout.Label($"_autoTarget: {_autoTarget}");
            GUILayout.Label($"_orbVel: {FormatVec(_orbVel)}");

            GUILayout.EndVertical();
        }


        string FormatVec(Vector3 v) =>
            $"({v.x:F1}, {v.y:F1}, {v.z:F1})";

        string FormatVecWithMag(Vector3 v) =>
            $"({v.x:F1}, {v.y:F1}, {v.z:F1} | {v.magnitude:F1})";
    }
}
