using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;


/*TODO
 * Get force direction (hence the acc (f/m) 
 * altitude only when inside plannet? how to measure otherwise? raw coordinate distance?
 * relative motion coords,? are they?
 * check out orientation ...
 * check pose, velocity vecs
 * distance..
 * make sep class for logging,
 * sep class for pid controller
 * 
 * 
 */

namespace OuterWildsModPsi
{
    public class OuterWildsModPsi : ModBehaviour
    {
        public static OuterWildsModPsi Instance;
        private ShipAltimeter altimeter;
        private ForceDetector forceDetector;
        private bool logging;
        private float logTimer;
        private const float LOG_INTERVAL = 0.1f; // Log every 100ms

        public void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());  // Fixed: Use CreateAndPatchAll
            Instance = this;
        }

        public void Start()
        {
            ModHelper.Console.WriteLine($"My mod {nameof(OuterWildsModPsi)} is loaded!", MessageType.Success);
            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen);
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }
        private void LogShipData()
        {
            var shipController = Locator.GetShipBody();
            if (shipController == null) return;

            Transform shipTransform = shipController.transform;
            OWRigidbody owRb = shipController.GetComponent<OWRigidbody>();
            Vector3 shipPos = shipTransform.position;
            Vector3 shipVel = owRb != null ? owRb.GetVelocity() : Vector3.zero;
            Quaternion shipRot = shipTransform.rotation;

            string line = string.Format("{0:F3},{1:F2},{2:F2},{3:F2},{4:F2},{5:F2},{6:F2}," +
                                        "{7:F3},{8:F3},{9:F3},{10:F3}",
                Time.time, shipPos.x, shipPos.y, shipPos.z, shipVel.x, shipVel.y, shipVel.z,
                shipRot.x, shipRot.y, shipRot.z, shipRot.w);


            ModHelper.Console.WriteLine(string.Format("[SHIP] POS:{0,6:F0} SPD:{1:F1} ROT:{2:F1}",
                shipPos.magnitude, shipVel.magnitude, shipRot.eulerAngles.y), MessageType.Message);
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            if (newScene != OWScene.SolarSystem) return;
            ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);

            var shipBody = Locator.GetShipBody();
            if (shipBody != null)
            {
                altimeter = shipBody.GetComponentInChildren<ShipAltimeter>();
                forceDetector = shipBody.GetComponentInChildren<ForceDetector>();
                ModHelper.Console.WriteLine($"Altimeter: {altimeter != null} | Force: {forceDetector != null}", MessageType.Success);
            }
            else
            {
                ModHelper.Console.WriteLine("Ship detector not found", MessageType.Error);
            }
        }

        public override void Configure(IModConfig config)
        {
            var newOrbitalControllerType = config.GetSettingsValue<string>("NumberControllers");  // Fixed: Typo
            ModHelper.Console.WriteLine($"You changed your Orbital Controller to: {newOrbitalControllerType}!");
        }

        
        public void Update()
        {
            // P key: Toggle logging (time scale toggle removed)
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                logging = !logging;
                if (logging)
                {
                    //InitializeLogging();
                    logTimer = 0f;
                }
                else
                {
                    ModHelper.Console.WriteLine("=== LOGGING STOPPED ===", MessageType.Warning);
                }
                ModHelper.Console.WriteLine($"Logging: {(logging ? "ON" : "OFF")}", MessageType.Success);
            }

            if (logging)
            {
                logTimer += Time.deltaTime;
                if (logTimer >= LOG_INTERVAL)
                {
                    LogShipData();
                    logTimer = 0f;
                }
            }

            // Altitude/force display
            if (altimeter == null || forceDetector == null)
            {
                var shipBody = Locator.GetShipBody();
                if (shipBody != null)
                {
                    altimeter = shipBody.GetComponentInChildren<ShipAltimeter>();
                    forceDetector = shipBody.GetComponentInChildren<ForceDetector>();
                }
                return;
            }

            bool isActive = altimeter.AltimeterIsActive();
            float terrainAlt = altimeter.GetTerrainAltitude();
            float shipAlt = altimeter.GetShipAltitude();
            float shipAboveTerrain = altimeter.GetShipAltitudeAboveTerrain();

            Vector3 forceAccel = forceDetector.GetForceAcceleration();
            float accelMag = forceAccel.magnitude;

            ModHelper.Console.WriteLine($"Alt: Active={isActive} T={terrainAlt:F0}m S={shipAlt:F0}m Above={shipAboveTerrain:F0}m | Force: {accelMag:F2}m/s²", MessageType.Message);
        }


    }
}