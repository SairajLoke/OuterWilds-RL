using OWML.Common;
using OWML.ModHelper;
using System;
using UnityEngine;

namespace OuterWildsModPsi
{
    /// <summary>
    /// Manages ship state reading and (eventually) orbit PID execution.
    /// Currently: reads ship telemetry and logs it. Stores orbit parameters for future PID use.
    /// </summary>
    public class PSIPIDController
    {
        // ── Ship components ──────────────────────────────────────────────
        private ShipBody _shipBody;
        private ShipAltimeter _altimeter;
        private ForceDetector _forceDetector;

        // ── Telemetry cache (refreshed each LogShipData call) ────────────
        private Vector3 _shipPosition;
        private Quaternion _shipRotation;
        private Vector3 _shipVelocity;
        private Vector3 _shipAcceleration;
        private float _shipMass;
        private ReferenceFrame _refFrame;
        private string _refFrameName;
        private Vector3 _refFramePos;
        private Vector3 _refFrameVel;
        private Type _refFrameType;
        private AstroObject _refFrameAstroObj;
        private float _refFrameOrbitSpeed;
        private ReferenceFrame _shipOrientation;

        // ── Orbit state ──────────────────────────────────────────────────
        private OrbitParameters _orbitParams;
        private bool _orbitActive;

        // ── Misc ─────────────────────────────────────────────────────────
        public bool foundShip { get; private set; }
        private IModHelper _modHelper;
        private DebugWindow _debugWindow;


        // ─────────────────────────────────────────────────────────────────
        // Construction
        // ─────────────────────────────────────────────────────────────────

        public PSIPIDController(IModHelper modHelper, DebugWindow debugWindow)
        {
            _modHelper = modHelper;
            _debugWindow = debugWindow;
        }

        // ─────────────────────────────────────────────────────────────────
        // Ship acquisition
        // ─────────────────────────────────────────────────────────────────

        public void getMyShip()
        {
            _shipBody = (ShipBody)Locator.GetShipBody();

            if (_shipBody != null)
            {
                _altimeter = _shipBody.GetComponent<ShipAltimeter>();
                _forceDetector = _shipBody.GetComponent<ForceDetector>();
                foundShip = true;

                _modHelper.Console.WriteLine(
                    $"[PID] Ship found. " +
                    $"Altimeter={_altimeter != null} | ForceDetector={_forceDetector != null}",
                    MessageType.Success);
            }
            else
            {
                foundShip = false;
                _modHelper.Console.WriteLine("[PID] Ship not found.", MessageType.Warning);
            }
        }


        public void getReferenceFrame()
        {
            var _referenceFrame = Locator.GetReferenceFrame();

            if (_shipBody != null)
            {
                //_altimeter = _shipBody.GetComponent<ShipAltimeter>();
                //_forceDetector = _shipBody.GetComponent<ForceDetector>();
                //foundShip = true;

                _modHelper.Console.WriteLine(
                    $"[PID] ref " +
                    MessageType.Success);
            }
            else
            {
                //foundShip = false;
                _modHelper.Console.WriteLine("[PID] Ship not found.", MessageType.Warning);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Orbit parameters
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the player confirms orbit settings in the config menu.
        /// For now: logs all parameters. Later: initialises PID setpoints.
        /// </summary>
        public void SetOrbitParameters(OrbitParameters parameters)
        {
            _orbitParams = parameters;
            _orbitActive = true;

            // ── Log everything clearly ────────────────────────────────────
            _modHelper.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", MessageType.Success);
            _modHelper.Console.WriteLine("[PID] ORBIT PARAMETERS CONFIRMED", MessageType.Success);
            _modHelper.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", MessageType.Success);

            _modHelper.Console.WriteLine(
                $"  Target body : {(parameters.targetBody != null ? parameters.targetBody.name : "null")}",
                MessageType.Message);
            _modHelper.Console.WriteLine(
                $"  Altitude    : {parameters.altitude:F0} m",
                MessageType.Message);
            _modHelper.Console.WriteLine(
                $"  Speed       : {parameters.speed:F2} m/s  " +
                $"({(parameters.userOverrideSpeed ? "manual override" : "auto circular orbit")})",
                MessageType.Message);
            _modHelper.Console.WriteLine(
                $"  Axis angle  : {parameters.axisAngle:F1}°  " +
                $"(0=equatorial, 90=polar)",
                MessageType.Message);
            _modHelper.Console.WriteLine(
                $"  Direction   : {(parameters.prograde ? "Prograde" : "Retrograde")}",
                MessageType.Message);

            // Log current ship state at moment of confirmation — useful for PID initialisation later
            if (_shipBody != null)
            {
                Vector3 vel = _shipBody.GetVelocity();
                Vector3 pos = _shipBody.GetPosition();
                _modHelper.Console.WriteLine(
                    $"  Ship pos    : ({pos.x:F0}, {pos.y:F0}, {pos.z:F0})",
                    MessageType.Message);
                _modHelper.Console.WriteLine(
                    $"  Ship speed  : {vel.magnitude:F2} m/s",
                    MessageType.Message);

                if (parameters.targetBody != null)
                {
                    float distToTarget = (pos - parameters.targetBody.GetPosition()).magnitude;
                    _modHelper.Console.WriteLine(
                        $"  Dist to target: {distToTarget:F0} m  " +
                        $"(target alt: {parameters.altitude:F0} m)",
                        MessageType.Message);
                }
            }

            _modHelper.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", MessageType.Success);
        }

        /// <summary>Disengages orbit mode (call when player wants manual control back).</summary>
        public void ClearOrbitParameters()
        {
            _orbitActive = false;
            _modHelper.Console.WriteLine("[PID] Orbit mode disengaged.", MessageType.Warning);
        }

        public bool IsOrbitActive() => _orbitActive;

        // ─────────────────────────────────────────────────────────────────
        // Telemetry logging
        // ─────────────────────────────────────────────────────────────────

        public void LogShipData()
        {
            if (_shipBody == null)
            {
                _modHelper.Console.WriteLine("[PID] Ship body is null.", MessageType.Error);
                foundShip = false;
                return;
            }

            _shipPosition = _shipBody.GetPosition();
            _shipRotation = _shipBody.GetRotation();
            _shipVelocity = _shipBody.GetVelocity();
            _shipAcceleration = _shipBody.GetAcceleration();
            _shipMass = _shipBody.GetMass();


            _refFrame = _shipBody.GetReferenceFrame();
            _refFramePos = _refFrame.GetPosition();
            _refFrameVel = _refFrame.GetVelocity();
            _refFrameType = _refFrame.GetType();
            _refFrameAstroObj = _refFrame.GetAstroObject();
            _refFrameName = _refFrame?.GetOWRigidBody()?.name ?? "unknown";
            //_refFrameOrbitSpeed = _refFrame.GetOrbitSpeed();


            Vector3 forceAccel = _forceDetector != null
                ? _forceDetector.GetForceAcceleration()
                : Vector3.zero;

            // Compact telemetry line
            _modHelper.Console.WriteLine(
                $"[SHIP] \n  " +
                $"pos:{_shipPosition} | mag:{_shipPosition.magnitude,7:F0}  ..." +
                $"vel:{_shipVelocity} | mag:{_shipVelocity.magnitude,6:F1} m/s  ..." +
                $"acc:{_shipAcceleration} | mag:{_shipAcceleration.magnitude,5:F2} m/s²  ..." +
                $"force:{forceAccel} | mag:{forceAccel.magnitude,5:F2} m/s²  ..." +
                $"rot:{_shipRotation.eulerAngles.x:F1}°, {_shipRotation.eulerAngles.y:F1}°, {_shipRotation.eulerAngles.z:F1}°  ..." +
                $"orbit:{(_orbitActive ? $"ACTIVE → {_orbitParams.targetBody?.name ?? "?"}" : "inactive")}",
                MessageType.Success);


            // Compact telemetry line
            _modHelper.Console.WriteLine(
                $"[REF FRAME] \n  " +
                $"refPos: {_refFramePos,7:F0}  " +
                $"refVel: {_refFrameVel,6:F1}m/s  " +
                $"refType: {_refFrameType}  " +
                $"refObj: {_refFrameAstroObj}" +
                $"refFrameName: {_refFrameName}", MessageType.Success);

            if (_debugWindow != null)
            {
                _debugWindow.pos = _shipPosition;
                _debugWindow.vel = _shipVelocity;
                _debugWindow.acc = _shipAcceleration;
                _debugWindow.force = forceAccel;

                _debugWindow.rotEuler = _shipRotation.eulerAngles;

                _debugWindow.orbitActive = _orbitActive;
                _debugWindow.targetName = _orbitParams.targetBody?.name ?? "None";

                _debugWindow.refPos = _refFramePos;
                _debugWindow.refVel = _refFrameVel;
                _debugWindow.refType = _refFrameType?.Name ?? "null";
                _debugWindow.refFrameName = _refFrameName; 
                //_refFrameAstroObj?.name ?? "null";
            }
            else
            {
                _modHelper.Console.WriteLine("No debug window found");
            }
        }
    }
}
