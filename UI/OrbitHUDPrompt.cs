using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using OWML.Common;

namespace OuterWildsModPsi
{
    /// <summary>
    /// A small screen-space prompt shown in the top-right corner after autopilot arrives,
    /// styled to match the game's existing HUD prompts (white text, no background).
    /// Listens for [O] keypress to open the OrbitConfigMenu.
    /// </summary>
    public class OrbitHUDPrompt : MonoBehaviour
    {
        // ── UI references ────────────────────────────────────────────────
        private Canvas          _canvas;
        private RectTransform   _panel;
        private Text            _promptText;

        // ── State ────────────────────────────────────────────────────────
        private OWRigidbody     _targetBody;
        private OrbitConfigMenu _configMenu;
        private IModHelper      _modHelper;
        private bool            _visible;

        // ── Layout constants (match game's top-right prompt block) ───────
        private const float PANEL_WIDTH   = 260f;
        private const float PANEL_HEIGHT  = 36f;
        private const float MARGIN_RIGHT  = 20f;
        private const float MARGIN_TOP    = 200f; // sit below existing prompts
        private const int   FONT_SIZE     = 18;

        // ─────────────────────────────────────────────────────────────────
        // Factory – call this once from OuterWildsModPsi.Start()
        // ─────────────────────────────────────────────────────────────────
        public static OrbitHUDPrompt Create(OrbitConfigMenu configMenu, IModHelper modHelper)
        {
            GameObject go = new GameObject("OrbitHUDPrompt");
            DontDestroyOnLoad(go);
            OrbitHUDPrompt prompt = go.AddComponent<OrbitHUDPrompt>();
            prompt._configMenu = configMenu;
            prompt._modHelper  = modHelper;
            prompt.BuildUI();
            prompt.Hide();
            return prompt;
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Show the prompt for a specific target body.</summary>
        public void Show(OWRigidbody targetBody)
        {
            if (targetBody == null) return;
            _targetBody = targetBody;
            _canvas.gameObject.SetActive(true);
            _visible = true;
            _modHelper.Console.WriteLine(
                $"[OrbitPrompt] Showing prompt for {targetBody.name}", MessageType.Info);
        }

        /// <summary>Hide the prompt without opening the menu.</summary>
        public void Hide()
        {
            _canvas.gameObject.SetActive(false);
            _visible = false;
        }

        // ─────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_visible) return;

            // Hide if player unbuckles (leaves the cockpit seat)
            if (!PlayerState.AtFlightConsole())
            {
                Hide();
                return;
            }

            // O key opens the config menu
            if (Keyboard.current.oKey.wasPressedThisFrame)
            {
                Hide();
                _configMenu.Show(_targetBody);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // UI construction
        // ─────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Canvas ───────────────────────────────────────────────────
            GameObject canvasGO = new GameObject("OrbitPromptCanvas");
            canvasGO.transform.SetParent(transform);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            canvasGO.AddComponent<CanvasScaler>();  // default 800×600 ref
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Panel (anchor: top-right) ────────────────────────────────
            GameObject panelGO = new GameObject("PromptPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);

            _panel = panelGO.AddComponent<RectTransform>();
            _panel.anchorMin        = new Vector2(1f, 1f);
            _panel.anchorMax        = new Vector2(1f, 1f);
            _panel.pivot            = new Vector2(1f, 1f);
            _panel.anchoredPosition = new Vector2(-MARGIN_RIGHT, -MARGIN_TOP);
            _panel.sizeDelta        = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);

            // No background image — match game style (text only)

            // ── Text ─────────────────────────────────────────────────────
            GameObject textGO = new GameObject("PromptText");
            textGO.transform.SetParent(panelGO.transform, false);

            RectTransform textRect   = textGO.AddComponent<RectTransform>();
            textRect.anchorMin       = Vector2.zero;
            textRect.anchorMax       = Vector2.one;
            textRect.offsetMin       = Vector2.zero;
            textRect.offsetMax       = Vector2.zero;

            _promptText              = textGO.AddComponent<Text>();
            _promptText.text         = "[O]  Begin Orbit";
            _promptText.font         = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _promptText.fontSize     = FONT_SIZE;
            _promptText.fontStyle    = FontStyle.Normal;
            _promptText.color        = Color.white;
            _promptText.alignment    = TextAnchor.MiddleRight;

            // Key label styled like the game's coloured key badges
            // We simulate it by prefixing with a bracket-wrapped label in the same text.
            // A richer approach would be two Text components but this is clean and readable.
        }
    }
}
