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
        // ── Singleton (used by Harmony patch to call back in) ────────────
        public static OuterWildsModPsi Instance;

        // ── Systems ──────────────────────────────────────────────────────
        private PSIPIDController _pidController;
        private OrbitHUDPrompt   _orbitPrompt;
        private OrbitConfigMenu _orbitMenu;

        // ── Autopilot state ──────────────────────────────────────────────
        private Autopilot        _autopilot;
        private ReferenceFrame   _autopilotTargetFrame; // set by Harmony patch

        // ── Logging ──────────────────────────────────────────────────────
        private bool  _logging;
        private float _logTimer;
        private const float LOG_INTERVAL = 0.1f;

        private DebugWindow _debugWindow;

        // ─────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────

        public void Awake()
        {
            Instance = this;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        public void Start()
        {
            ModHelper.Console.WriteLine(
                $"[PSI] Mod {nameof(OuterWildsModPsi)} loaded.", MessageType.Success);

            //// Build UI objects once — they survive scene loads via DontDestroyOnLoad
            //_orbitMenu   = OrbitConfigMenu.Create(ModHelper);
            //_orbitPrompt = OrbitHUDPrompt.Create(_orbitMenu, ModHelper);

            //// Wire confirm callback: menu → PID controller
            //_orbitMenu.OnConfirm += OnOrbitConfirmed;

            //debug window...........
            GameObject debugObj = new GameObject("PSI_DebugWindow");
            _debugWindow = debugObj.AddComponent<DebugWindow>();
            GameObject.DontDestroyOnLoad(debugObj);
            if(_debugWindow  != null)
            {
                ModHelper.Console.WriteLine($"[PSI] Debug window created: {_debugWindow != null}", MessageType.Success);
            }
            else
            {
                ModHelper.Console.WriteLine($"No debug window created", MessageType.Error);
            }


            // Build PID controller
            _pidController = new PSIPIDController(ModHelper, _debugWindow);

            // Subscribe to scene loads
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
            

            // Handle case where we're already in the solar system at mod load
            //OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen);

        }

        // ─────────────────────────────────────────────────────────────────
        // Scene management
        // ─────────────────────────────────────────────────────────────────

        private void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            if (newScene != OWScene.SolarSystem) return;

            ModHelper.Console.WriteLine("[PSI] Solar system loaded.", MessageType.Success);

            // Get ship and initialize PID
            _pidController.getMyShip();

            // Hook autopilot events — must be done after scene load when ship exists
            HookAutopilotEvents();
        }

        private void HookAutopilotEvents()
        {
            OWRigidbody shipBody = Locator.GetShipBody();
            if (shipBody == null)
            {
                ModHelper.Console.WriteLine("[PSI] Ship not found when hooking autopilot.", MessageType.Error);
                return;
            }

            _autopilot = shipBody.GetComponent<Autopilot>();
            if (_autopilot == null)
            {
                ModHelper.Console.WriteLine("[PSI] Autopilot component not found.", MessageType.Error);
                return;
            }else
            {;
                ModHelper.Console.WriteLine("[PSI] Ship autopilot found", MessageType.Success);
            }

            // Clean up any previous subscriptions to avoid double-firing
            _autopilot.OnArriveAtDestination -= OnAutopilotArrived;
            _autopilot.OnAbortAutopilot      -= OnAutopilotAborted;

            // Subscribe
            _autopilot.OnArriveAtDestination += OnAutopilotArrived;
            _autopilot.OnAbortAutopilot      += OnAutopilotAborted;

            ModHelper.Console.WriteLine("[PSI] Autopilot events hooked.", MessageType.Success);
        }

        // ─────────────────────────────────────────────────────────────────
        // Autopilot callbacks
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by HarmonyAutopilotPatch when FlyToDestination() is invoked.
        /// This gives us the target ReferenceFrame before it's stored privately.
        /// </summary>
        public void OnAutopilotTargetSet(ReferenceFrame referenceFrame)
        {
            _autopilotTargetFrame = referenceFrame;
            string name = referenceFrame?.GetOWRigidBody()?.name ?? "unknown";
            ModHelper.Console.WriteLine($"[PSI] Autopilot target set: {name}", MessageType.Info);
        }

        /// <summary>Fired by Autopilot.OnArriveAtDestination when ship reaches the target.</summary>
        private void OnAutopilotArrived(float arrivalError)
        {
            ModHelper.Console.WriteLine(
                $"[PSI] Autopilot arrived. Error: {arrivalError:F1}m", MessageType.Success);

            if (_autopilotTargetFrame == null)
            {
                ModHelper.Console.WriteLine(
                    "[PSI] No target frame stored — did FlyToDestination fire first?", MessageType.Warning);
                return;
            }

            OWRigidbody targetBody = _autopilotTargetFrame.GetOWRigidBody();
            if (targetBody == null)
            {
                ModHelper.Console.WriteLine("[PSI] Target frame has no OWRigidbody.", MessageType.Warning);
                return;
            }

            // Show the [O] orbit prompt in the HUD
            _orbitPrompt.Show(targetBody);
        }

        /// <summary>Fired when autopilot is aborted mid-flight — hide any orbit UI.</summary>
        private void OnAutopilotAborted()
        {
            _orbitPrompt.Hide();
            _orbitMenu.Hide();
            ModHelper.Console.WriteLine("[PSI] Autopilot aborted — orbit UI hidden.", MessageType.Warning);
        }

        // ─────────────────────────────────────────────────────────────────
        // Orbit confirmation
        // ─────────────────────────────────────────────────────────────────

        private void OnOrbitConfirmed(OrbitParameters parameters)
        {
            _pidController.SetOrbitParameters(parameters);
        }

        // ─────────────────────────────────────────────────────────────────
        // Update loop
        // ─────────────────────────────────────────────────────────────────

        public void Update()
        {
            // Re-acquire ship if lost (e.g. scene reload)
            if (!_pidController.foundShip)
            {
                _pidController.getMyShip();
                return;
            }

            // P key: toggle logging
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                _logging = !_logging;
                _logTimer = 0f;
                ModHelper.Console.WriteLine(
                    $"[PSI] Logging: {(_logging ? "ON" : "OFF")}",
                    _logging ? MessageType.Success : MessageType.Warning);
            }

            // Timed logging
            if (_logging)
            {
                _logTimer += Time.deltaTime;
                if (_logTimer >= LOG_INTERVAL)
                {
                    _logTimer = 0f;
                    _pidController.LogShipData();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // OWML config
        // ─────────────────────────────────────────────────────────────────

        public override void Configure(IModConfig config)
        {
            string controllerType = config.GetSettingsValue<string>("NumberControllers");
            ModHelper.Console.WriteLine(
                $"[PSI] Orbital Controller changed to: {controllerType}", MessageType.Info);
        }
    }


    // ─────────────────────────────────────────────────────────────────────
    // Harmony patch — intercepts FlyToDestination to capture target frame
    // ─────────────────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(Autopilot), nameof(Autopilot.FlyToDestination))]
    public static class HarmonyAutopilotPatch
    {
        /// <summary>
        /// Prefix fires before FlyToDestination executes.
        /// The ReferenceFrame parameter is the one the autopilot is about to store privately.
        /// We grab it here and hand it to our mod instance.
        /// </summary>
        public static void Prefix(ReferenceFrame referenceFrame)
        {
            if (OuterWildsModPsi.Instance == null) return;
            OuterWildsModPsi.Instance.OnAutopilotTargetSet(referenceFrame);
        }
    }

    //[HarmonyPatch]
    //public class MyPatchClass
    //{
    //    [HarmonyPostfix]
    //    [HarmonyPatch(typeof(DeathManager), nameof(DeathManager.KillPlayer))]
    //    public static void DeathManager_KillPlayer_Prefix()
    //    {

    //        if (OuterWildsModPsi.Instance == null) return;
    //        OuterWildsModPsi.Instance.OnAutopilotTargetSet(referenceFrame);
    //    }
    //}

}
