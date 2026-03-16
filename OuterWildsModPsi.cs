// OuterWildsModPsi.cs
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OuterWildsModPsi
{
    public class OuterWildsModPsi : ModBehaviour
    {
        public static OuterWildsModPsi Instance;
        private PSIPIDController psiPIDController;
        private bool logging;
        private float logTimer;
        private const float LOG_INTERVAL = 0.1f;

        public void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Instance = this;
        }

        public void Start()
        {
            // FIX 1: instantiate with ModHelper injected
            psiPIDController = new PSIPIDController(ModHelper);

            ModHelper.Console.WriteLine(
                $"My mod {nameof(OuterWildsModPsi)} is loaded!",
                MessageType.Success);

            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;

            // call manually in case we're already in solar system
            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.SolarSystem);
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            if (newScene != OWScene.SolarSystem) return;
            ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
            psiPIDController.getMyShip();
        }

        public override void Configure(IModConfig config)
        {
            var newOrbitalControllerType =
                config.GetSettingsValue<string>("NumberControllers");
            ModHelper.Console.WriteLine(
                $"Orbital Controller changed to: {newOrbitalControllerType}!");
        }

        public void Update()
        {
            if (psiPIDController == null) return;

            // FIX 2: only search if not found yet
            if (!psiPIDController.foundShip)
            {
                psiPIDController.getMyShip();
                return; // don't run rest of update until ship found
            }

            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                logging = !logging;
                logTimer = 0f;
                ModHelper.Console.WriteLine(
                    $"Logging: {(logging ? "ON" : "OFF")}",
                    MessageType.Success);

                if (!logging)
                    ModHelper.Console.WriteLine(
                        "=== LOGGING STOPPED ===", MessageType.Warning);
            }

            // FIX 3: LogShipData inside the timer block
            if (logging)
            {
                logTimer += Time.deltaTime;
                if (logTimer >= LOG_INTERVAL)
                {
                    logTimer = 0f;
                    psiPIDController.LogShipData();
                }
            }
        }
    }
}