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
        private ShipBody      _shipBody;
        private ShipAltimeter _altimeter;
        private ForceDetector _forceDetector;
        private Autopilot     _autoPilot;
        private PlayerBody    _playerBodyLocator;

        // ── Telemetry cache (refreshed each LogShipData call) ────────────
        private Vector3    _shipPosition;
        private Quaternion _shipRotation;
        private Vector3    _shipVelocity;
        private Vector3    _shipAcceleration;
        private float      _shipMass;

        private Transform      _shipCurrTransform;
        private ReferenceFrame _shipCurrRefFrame;
        private OWRigidbody    _shipCurrRefFrameRigBody;
        private string         _shipCurrRefFrameRigBodyName;
        private Vector3        _shipRelVelWRTCurrRefFrameRigBody;

        //private Vector3 _shipRelVelWRTCurrRefFrame;
        //private AstroObject _currRefFrameAstroObj;
        //private float _currRefFrameOrbitSpeed;
        // ── Orbit state ──────────────────────────────────────────────────
        //private OrbitParameters _orbitParams;
        //private bool _psiOrbitActive;

        // ── Misc ─────────────────────────────────────────────────────────
        public bool foundShip { get; private set; }
        private IModHelper _modHelper;
        private DebugWindow _debugWindow;


        // ── Player Vars ──────────────────────────────────────────────────
        private Vector3 _playerPositionLocator;
        private Vector3 _playerVelocityLocator;
        private Vector3 _playerWorldCOMLocator;
        private ReferenceFrame _playerCurrRefFrameLocator;
        private string _playerCurrRefFrameNameLocator;
        private float _playerMassLocator;
        private string _playerBodyNamelocator;


        // ── Others ──────────────────────────────────────────────────
        private CenterOfTheUniverse _CenOTUni;
        private Vector3 _centerofUniPos;
        private Vector3 _centerofUniVel;

        private AstroObject _sunAstroObj;
        private AstroObject _timberAstroObj;
        private OWRigidbody _sunRigBody;
        private OWRigidbody _timberRigBody;
        private Vector3 _sunPos;
        private Vector3 _timberPos;
        private Vector3 _shipCurrRefFrameVel;
        //private AstroObject _shipCurrRefFrameAstroObj;
        //private OWRigidbody _shipCurrRefFrameAstroObjRigBody; are null when trying to read
        private Vector3 _shipCurrRefFramePos;
        private bool _arrivedAtDesti;
        private bool _isApproachingDestination;

        private OWRigidbody _locatorRefFrameRigBody;
        private string _locatorRefFrameRigBodyName;
        private ReferenceFrame _locatorCurrRefFrameIPF_True;

        public PSIPIDController(IModHelper modHelper, DebugWindow debugWindow)
        {
            _modHelper = modHelper;
            _debugWindow = debugWindow;
        }

        public void getMyPlayer()
        {
            _playerBodyLocator = (PlayerBody)Locator.GetPlayerBody();

            if (_playerBodyLocator != null)
            {
                _modHelper.Console.WriteLine("[PID] Player found.", MessageType.Success);
                _playerPositionLocator = _playerBodyLocator.GetPosition();
                _playerVelocityLocator = _playerBodyLocator.GetVelocity();
                _playerWorldCOMLocator = _playerBodyLocator.GetWorldCenterOfMass();
                _playerCurrRefFrameLocator = _playerBodyLocator.GetReferenceFrame();
                _playerCurrRefFrameNameLocator = _playerCurrRefFrameLocator.GetOWRigidBody()?.name ?? "unknown";
                _playerMassLocator = _playerBodyLocator.GetMass();
                _playerBodyNamelocator = _playerBodyLocator.name;
            }
            else
            {
                _modHelper.Console.WriteLine("[PID] Player not found.", MessageType.Error);
            }
        }

        public void getMyShip()
        {
            _shipBody = (ShipBody)Locator.GetShipBody();

            if (_shipBody != null)
            {
                _altimeter = _shipBody.GetComponent<ShipAltimeter>(); //not found here
                _forceDetector = _shipBody.GetComponent<ForceDetector>();
                _autoPilot = _shipBody.GetComponent<Autopilot>();
                //_isApproachingDestination = _autoPilot.IsApproachingDestination();

                foundShip = true;

                _modHelper.Console.WriteLine(
                    $"[PID] Ship found. " +
                    $"loc {_shipBody.name} " +
                    $"Altimeter={_altimeter != null} | ForceDetector={_forceDetector != null} | Autopilot={_autoPilot != null}",
                    MessageType.Success);
            }
            else
            {
                foundShip = false;
                _modHelper.Console.WriteLine("[PID] Ship not found.", MessageType.Warning);
            }

            //_autoPilot.OnArriveAtDestination += OnAPArriveDesti;

            _timberAstroObj = Locator.GetAstroObject(AstroObject.Name.TimberHearth);  //////not sure why this doesn't work but listing objects work
            if (_timberAstroObj != null)
            {
                _timberRigBody = _timberAstroObj.GetOWRigidbody();
                _modHelper.Console.WriteLine($"[PID] {_timberRigBody.name}  Found", MessageType.Success);
            }

            _sunAstroObj = Locator.GetAstroObject(AstroObject.Name.Sun);
            if (_sunAstroObj != null)
            {
                _sunRigBody = _sunAstroObj.GetOWRigidbody();
                _modHelper.Console.WriteLine($"[PID] {_sunRigBody.name} Found", MessageType.Success);
            }

            //AstroObject[] astroObjects = GameObject.FindObjectsOfType<AstroObject>();
            //foreach (var astro in astroObjects)
            
            _CenOTUni = Locator.GetCenterOfTheUniverse();
            if (_CenOTUni != null)
            {
                _modHelper.Console.WriteLine($"[PID] CofUniv: {_centerofUniPos} Found", MessageType.Success);
            }
            //_modHelper.Console.WriteLine(
            //        $"[PID] Other Objs. " +
            //        $"loc {_shipBody.name} " +
            //        $"Altimeter={_altimeter != null} | ForceDetector={_forceDetector != null} | Autopilot={_autoPilot != null}",
            //        MessageType.Success);

        }

        //private Autopilot.ArriveAtDestinationEvent OnAPArriveDesti(float arrivalError)
        //{
        //    _modHelper.Console.WriteLine("event: Arrived at destination");
        //    return;
        //}

        public void getReferenceFrame()
        {
            _locatorCurrRefFrameIPF_True = Locator.GetReferenceFrame(ignorePassiveFrame: true);

            if (_locatorCurrRefFrameIPF_True != null)
            {
                _locatorRefFrameRigBody = _locatorCurrRefFrameIPF_True.GetOWRigidBody();
                _locatorRefFrameRigBodyName = _locatorRefFrameRigBody.name;

                _modHelper.Console.WriteLine($"[PID] locator ref frame {_locatorRefFrameRigBodyName}" + MessageType.Success);
            }
            else
            {
                _modHelper.Console.WriteLine("[PID] Locator Ref frame not found.", MessageType.Warning);
            }
        }

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
            _shipCurrTransform = _shipBody._transform;
            var shipPplayerBodyCurrVel = _shipBody._playerBody._currentVelocity;
           
            _shipCurrRefFrame = _shipBody.GetReferenceFrame();
            if (_shipCurrRefFrame != null)
            {
                _shipCurrRefFramePos = _shipCurrRefFrame.GetPosition();
                _shipCurrRefFrameRigBody = _shipCurrRefFrame.GetOWRigidBody();
                _shipCurrRefFrameRigBodyName = _shipCurrRefFrameRigBody?.name ?? "unknown";
                _shipRelVelWRTCurrRefFrameRigBody = _shipCurrRefFrameRigBody != null
                                                        ? _shipBody.GetRelativeVelocity(_shipCurrRefFrameRigBody)
                                                        : Vector3.zero;


                _shipCurrRefFrameVel = _shipCurrRefFrame.GetVelocity();
                //_shipCurrRefFrameAstroObj = _shipCurrRefFrame.GetAstroObject(); //null
                //_shipCurrRefFrameAstroObjRigBody = _shipCurrRefFrameAstroObj?.GetOWRigidbody();



                // ── Minimal logging (only when something is wrong) ──
                if (_shipCurrRefFrameRigBody == null)
                    _modHelper.Console.WriteLine("[PID] RefFrame RB is NULL", MessageType.Warning);

                //if (_shipCurrRefFrameAstroObj == null)
                //    _modHelper.Console.WriteLine("[PID] RefFrame AstroObject is NULL", MessageType.Warning);

                //if (_shipCurrRefFrameAstroObj != null && _shipCurrRefFrameAstroObjRigBody == null)
                //    _modHelper.Console.WriteLine("[PID] AstroObject RB is NULL", MessageType.Warning);
            }
            else
            {
                _modHelper.Console.WriteLine($"[PID] _shipCurrRefFrame not found.", MessageType.Warning);
            }

            if (_CenOTUni != null)
            {
                _centerofUniPos = _CenOTUni.GetOffsetPosition();
                _centerofUniVel = _CenOTUni.GetOffsetVelocity();
            }
            else
            {
                _modHelper.Console.WriteLine("[PID] _CenOTUni not found.", MessageType.Warning);
            }

            if (_sunRigBody != null)
            {
                _sunPos = _sunRigBody.GetPosition();
            }
            else
            {
                _modHelper.Console.WriteLine("[PID] _sunRigBody not found.", MessageType.Warning);
            }

            if (_timberRigBody != null)
            {
                _timberPos = _timberRigBody.GetPosition();
            }
            else
            {
                _modHelper.Console.WriteLine("[PID] _timberRigBody not found.", MessageType.Warning);
            }


            //_refFrameOrbitSpeed = _refFrame.GetOrbitSpeed();, tangential ve;ocity 


            Vector3 forceAccel = _forceDetector != null
                ? _forceDetector.GetForceAcceleration()
                : Vector3.zero;


            if (_debugWindow != null)
            {
                
                _debugWindow.shipPos = _shipPosition;
                _debugWindow.shipVel = _shipVelocity;
                _debugWindow.shipAcc = _shipAcceleration;
                _debugWindow.shipForce = forceAccel;
                _debugWindow.shipRotEuler = _shipRotation.eulerAngles;

                _debugWindow.shipRefFrameName = _shipCurrRefFrameRigBodyName;
                _debugWindow.shipRelVelWRTRef = _shipRelVelWRTCurrRefFrameRigBody;

                // PLAYER
                _debugWindow.playerPos = _playerPositionLocator;
                _debugWindow.playerVel = _playerVelocityLocator;
                _debugWindow.playerCOM = _playerWorldCOMLocator;
                _debugWindow.playerRefFrame = _playerCurrRefFrameNameLocator;
                _debugWindow.playerMass = _playerMassLocator;
                _debugWindow.playerName = _playerBodyNamelocator;

                // OBJECTS
                _debugWindow.sunPos = _sunPos;
                _debugWindow.timberPos = _timberPos;
                _debugWindow.centerUniPos = _centerofUniPos;
                _debugWindow.centerUniVel = _centerofUniVel;

                // AUTOPILOT
                _debugWindow.isApproaching = _isApproachingDestination;
                _debugWindow.arrived = _arrivedAtDesti;
                _debugWindow.autoTarget = _shipCurrRefFrameRigBodyName;

            }
            else { _modHelper.Console.WriteLine("No debug window found"); }
        }

    }

}
        // ─────────────────────────────────────────────────────────────────
        // Orbit parameters
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the player confirms orbit settings in the config menu.
        /// For now: logs all parameters. Later: initialises PID setpoints.
        /// </summary>
        //public void SetOrbitParameters(OrbitParameters parameters)
        //{
        //    _orbitParams = parameters;
        //    _orbitActive = true;

        //    // ── Log everything clearly ────────────────────────────────────
        //    _modHelper.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", MessageType.Success);
        //    _modHelper.Console.WriteLine("[PID] ORBIT PARAMETERS CONFIRMED", MessageType.Success);
        //    _modHelper.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", MessageType.Success);

        //    _modHelper.Console.WriteLine(
        //        $"  Target body : {(parameters.targetBody != null ? parameters.targetBody.name : "null")}",
        //        MessageType.Message);
        //    _modHelper.Console.WriteLine(
        //        $"  Altitude    : {parameters.altitude:F0} m",
        //        MessageType.Message);
        //    _modHelper.Console.WriteLine(
        //        $"  Speed       : {parameters.speed:F2} m/s  " +
        //        $"({(parameters.userOverrideSpeed ? "manual override" : "auto circular orbit")})",
        //        MessageType.Message);
        //    _modHelper.Console.WriteLine(
        //        $"  Axis angle  : {parameters.axisAngle:F1}°  " +
        //        $"(0=equatorial, 90=polar)",
        //        MessageType.Message);
        //    _modHelper.Console.WriteLine(
        //        $"  Direction   : {(parameters.prograde ? "Prograde" : "Retrograde")}",
        //        MessageType.Message);

        //    // Log current ship state at moment of confirmation — useful for PID initialisation later
        //    if (_shipBody != null)
        //    {
        //        Vector3 vel = _shipBody.GetVelocity();
        //        Vector3 pos = _shipBody.GetPosition();
        //        _modHelper.Console.WriteLine(
        //            $"  Ship pos    : ({pos.x:F0}, {pos.y:F0}, {pos.z:F0})",
        //            MessageType.Message);
        //        _modHelper.Console.WriteLine(
        //            $"  Ship speed  : {vel.magnitude:F2} m/s",
        //            MessageType.Message);

        //        if (parameters.targetBody != null)
        //        {
        //            float distToTarget = (pos - parameters.targetBody.GetPosition()).magnitude;
        //            _modHelper.Console.WriteLine(
        //                $"  Dist to target: {distToTarget:F0} m  " +
        //                $"(target alt: {parameters.altitude:F0} m)",
        //                MessageType.Message);
        //        }
        //    }

        //    _modHelper.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", MessageType.Success);
        //}

        /// <summary>Disengages orbit mode (call when player wants manual control back).</summary>
        //public void ClearOrbitParameters()
        //{
        //    _orbitActive = false;
        //    _modHelper.Console.WriteLine("[PID] Orbit mode disengaged.", MessageType.Warning);
        //}

        //public bool IsOrbitActive() => _orbitActive;

        // ─────────────────────────────────────────────────────────────────
        // Telemetry logging
        // ─────────────────────────────────────────────────────────────────
//    }
//}
