using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using OWML.Common;

namespace OuterWildsModPsi
{
    /// <summary>
    /// A screen-space configuration panel that appears when the player presses [O].
    /// Lets the player configure altitude, speed (auto or manual), axis angle,
    /// and direction (prograde / retrograde) before confirming orbit insertion.
    ///
    /// Visual style: dark semi-transparent panel with white text, slim sliders —
    /// minimal and readable inside the cockpit, consistent with the game's HUD language.
    /// </summary>
    public class OrbitConfigMenu : MonoBehaviour
    {
        // ── Callbacks ────────────────────────────────────────────────────
        public System.Action<OrbitParameters> OnConfirm;

        // ── UI references ────────────────────────────────────────────────
        private Canvas _canvas;
        private GameObject _panel;

        // Altitude row
        private Slider _altSlider;
        private Text _altValueText;

        // Speed row
        private Slider _speedSlider;
        private Text _speedValueText;
        private Toggle _speedOverrideToggle;
        private InputField _speedInputField;

        // Axis row
        private Slider _axisSlider;
        private Text _axisValueText;

        // Direction row
        private Toggle _progradeToggle;
        private Toggle _retrogradeToggle;

        // Confirm
        private Button _confirmButton;

        // ── State ────────────────────────────────────────────────────────
        private OWRigidbody _targetBody;
        private IModHelper _modHelper;
        private bool _visible;

        // Ship world position captured at the moment the menu opens.
        // Used as the surface-distance reference for auto orbit speed calculation.
        // (Ship has just arrived via autopilot so distance ≈ arrival radius above surface.)
        private Vector3 _shipPositionAtArrival;

        // Slider ranges
        private const float ALT_MIN = 200f;
        private const float ALT_MAX = 5000f;
        private const float ALT_DEF = 1500f;
        private const float SPD_MIN = 10f;
        private const float SPD_MAX = 500f;
        private const float AXIS_MIN = 0f;
        private const float AXIS_MAX = 180f;
        private const float AXIS_DEF = 0f;

        // Panel dimensions
        private const float PANEL_W = 320f;
        private const float PANEL_H = 380f;

        // Colours
        private static readonly Color BG_COLOR = new Color(0.05f, 0.08f, 0.12f, 0.92f);
        private static readonly Color ACCENT_COLOR = new Color(0.40f, 0.85f, 1.00f, 1.00f); // cyan-ish to match game
        private static readonly Color TEXT_COLOR = Color.white;
        private static readonly Color DIM_COLOR = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color HEADER_COLOR = new Color(0.40f, 0.85f, 1.00f, 1.00f);

        // ─────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────
        public static OrbitConfigMenu Create(IModHelper modHelper)
        {
            GameObject go = new GameObject("OrbitConfigMenu");
            DontDestroyOnLoad(go);
            OrbitConfigMenu menu = go.AddComponent<OrbitConfigMenu>();
            menu._modHelper = modHelper;
            menu.BuildUI();
            menu.Hide();
            return menu;
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────
        public void Show(OWRigidbody targetBody)
        {
            _targetBody = targetBody;
            _canvas.gameObject.SetActive(true);
            _visible = true;

            // Capture ship position NOW — autopilot just arrived, so this is our
            // surface-distance reference for the circular orbit speed formula.
            OWRigidbody shipBody = Locator.GetShipBody();
            _shipPositionAtArrival = shipBody != null
                ? shipBody.GetPosition()
                : Vector3.zero;

            // Reset to defaults
            _altSlider.value = ALT_DEF;
            _axisSlider.value = AXIS_DEF;
            _speedOverrideToggle.isOn = false;
            _progradeToggle.isOn = true;

            // Calculate auto speed at default altitude
            RefreshAutoSpeed();

            _modHelper.Console.WriteLine(
                $"[OrbitMenu] Opened for target: {(targetBody != null ? targetBody.name : "null")}",
                MessageType.Info);
        }

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

            // Escape / Unbuckle closes menu
            if (Keyboard.current.escapeKey.wasPressedThisFrame || !PlayerState.AtFlightConsole())
            {
                Hide();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Event handlers
        // ─────────────────────────────────────────────────────────────────
        private void OnAltitudeChanged(float value)
        {
            _altValueText.text = $"{value:F0} m";
            if (!_speedOverrideToggle.isOn)
                RefreshAutoSpeed();
        }

        private void OnSpeedSliderChanged(float value)
        {
            if (_speedOverrideToggle.isOn)
                _speedValueText.text = $"{value:F1} m/s";
        }

        private void OnOverrideToggleChanged(bool isOn)
        {
            // Enable/disable manual speed controls
            _speedSlider.interactable = isOn;
            _speedInputField.interactable = isOn;

            if (isOn)
            {
                // Seed the override fields with the current auto value
                float autoSpd = OrbitParameters.CalculateCircularOrbitSpeed(
                    _targetBody, _shipPositionAtArrival, _altSlider.value);
                _speedSlider.value = Mathf.Clamp(autoSpd, SPD_MIN, SPD_MAX);
                _speedInputField.text = autoSpd.ToString("F1");
                _speedValueText.color = TEXT_COLOR;
            }
            else
            {
                RefreshAutoSpeed();
                _speedValueText.color = DIM_COLOR;
            }
        }

        private void OnSpeedInputChanged(string text)
        {
            if (!_speedOverrideToggle.isOn) return;
            if (float.TryParse(text, out float val))
            {
                val = Mathf.Clamp(val, SPD_MIN, SPD_MAX);
                _speedSlider.value = val;
                _speedValueText.text = $"{val:F1} m/s";
            }
        }

        private void OnAxisChanged(float value)
        {
            string label = value < 5f ? "Equatorial" :
                           value > 175f ? "Polar" :
                           $"{value:F0}°";
            _axisValueText.text = label;
        }

        private void OnConfirmClicked()
        {
            OrbitParameters p = new OrbitParameters();
            p.targetBody = _targetBody;
            p.altitude = _altSlider.value;
            p.axisAngle = _axisSlider.value;
            p.prograde = _progradeToggle.isOn;
            p.userOverrideSpeed = _speedOverrideToggle.isOn;

            if (p.userOverrideSpeed)
            {
                // Try to parse the input field first; fall back to slider
                if (!float.TryParse(_speedInputField.text, out p.speed))
                    p.speed = _speedSlider.value;
            }
            else
            {
                p.speed = OrbitParameters.CalculateCircularOrbitSpeed(
                    _targetBody, _shipPositionAtArrival, p.altitude);
            }

            _modHelper.Console.WriteLine($"[OrbitMenu] Confirmed: {p}", MessageType.Success);

            OnConfirm?.Invoke(p);
            Hide();
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────
        private void RefreshAutoSpeed()
        {
            float spd = OrbitParameters.CalculateCircularOrbitSpeed(
                _targetBody, _shipPositionAtArrival, _altSlider.value);
            _speedValueText.text = spd > 0f ? $"{spd:F1} m/s  (auto)" : "N/A";
            _speedValueText.color = DIM_COLOR;
        }

        // ─────────────────────────────────────────────────────────────────
        // UI construction
        // ─────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Canvas ───────────────────────────────────────────────────
            GameObject canvasGO = new GameObject("OrbitMenuCanvas");
            canvasGO.transform.SetParent(transform);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 101; // above HUD prompt

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Outer panel (centred) ────────────────────────────────────
            _panel = CreatePanel(canvasGO.transform, "MenuPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(PANEL_W, PANEL_H), BG_COLOR, addOutline: true);

            float cursorY = PANEL_H / 2f - 20f; // start near top, in panel-local coords

            // ── Header ───────────────────────────────────────────────────
            CreateLabel(_panel.transform, "Header", "ORBIT PARAMETERS",
                new Vector2(0f, cursorY - 12f), new Vector2(PANEL_W - 20f, 24f),
                HEADER_COLOR, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            cursorY -= 40f;

            // Thin separator line
            CreateLine(_panel.transform, cursorY);
            cursorY -= 16f;

            // ── Altitude row ─────────────────────────────────────────────
            CreateLabel(_panel.transform, "AltLabel", "ALTITUDE",
                new Vector2(-60f, cursorY), new Vector2(100f, 20f),
                DIM_COLOR, 11, FontStyle.Normal, TextAnchor.MiddleLeft);

            _altValueText = CreateLabel(_panel.transform, "AltValue", $"{ALT_DEF:F0} m",
                new Vector2(100f, cursorY), new Vector2(100f, 20f),
                TEXT_COLOR, 13, FontStyle.Normal, TextAnchor.MiddleRight);
            cursorY -= 22f;

            _altSlider = CreateSlider(_panel.transform, "AltSlider",
                new Vector2(0f, cursorY), new Vector2(PANEL_W - 30f, 18f),
                ALT_MIN, ALT_MAX, ALT_DEF);
            _altSlider.onValueChanged.AddListener(OnAltitudeChanged);
            cursorY -= 36f;

            // ── Speed row ────────────────────────────────────────────────
            CreateLabel(_panel.transform, "SpdLabel", "ORBITAL SPEED",
                new Vector2(-60f, cursorY), new Vector2(130f, 20f),
                DIM_COLOR, 11, FontStyle.Normal, TextAnchor.MiddleLeft);

            _speedValueText = CreateLabel(_panel.transform, "SpdValue", "",
                new Vector2(80f, cursorY), new Vector2(130f, 20f),
                DIM_COLOR, 13, FontStyle.Normal, TextAnchor.MiddleRight);
            cursorY -= 22f;

            _speedSlider = CreateSlider(_panel.transform, "SpdSlider",
                new Vector2(0f, cursorY), new Vector2(PANEL_W - 30f, 18f),
                SPD_MIN, SPD_MAX, 100f);
            _speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
            _speedSlider.interactable = false;
            cursorY -= 28f;

            // Override toggle + input field on the same row
            GameObject overrideRow = CreateRow(_panel.transform, "OverrideRow",
                new Vector2(0f, cursorY), new Vector2(PANEL_W - 30f, 22f));

            _speedOverrideToggle = CreateToggle(overrideRow.transform, "Override speed",
                new Vector2(-80f, 0f), new Vector2(160f, 22f), ACCENT_COLOR);
            _speedOverrideToggle.onValueChanged.AddListener(OnOverrideToggleChanged);

            _speedInputField = CreateInputField(overrideRow.transform, "SpeedInput",
                new Vector2(85f, 0f), new Vector2(90f, 22f));
            _speedInputField.onEndEdit.AddListener(OnSpeedInputChanged);
            _speedInputField.interactable = false;
            cursorY -= 38f;

            // ── Axis row ─────────────────────────────────────────────────
            CreateLabel(_panel.transform, "AxisLabel", "ORBIT PLANE ANGLE",
                new Vector2(-50f, cursorY), new Vector2(160f, 20f),
                DIM_COLOR, 11, FontStyle.Normal, TextAnchor.MiddleLeft);

            _axisValueText = CreateLabel(_panel.transform, "AxisValue", "Equatorial",
                new Vector2(80f, cursorY), new Vector2(110f, 20f),
                TEXT_COLOR, 13, FontStyle.Normal, TextAnchor.MiddleRight);
            cursorY -= 22f;

            _axisSlider = CreateSlider(_panel.transform, "AxisSlider",
                new Vector2(0f, cursorY), new Vector2(PANEL_W - 30f, 18f),
                AXIS_MIN, AXIS_MAX, AXIS_DEF);
            _axisSlider.onValueChanged.AddListener(OnAxisChanged);

            // Axis hint labels
            CreateLabel(_panel.transform, "AxisHintL", "0° Equatorial",
                new Vector2(-80f, cursorY - 16f), new Vector2(120f, 16f),
                DIM_COLOR, 9, FontStyle.Italic, TextAnchor.MiddleLeft);
            CreateLabel(_panel.transform, "AxisHintR", "180° Polar",
                new Vector2(80f, cursorY - 16f), new Vector2(100f, 16f),
                DIM_COLOR, 9, FontStyle.Italic, TextAnchor.MiddleRight);
            cursorY -= 46f;

            // ── Direction row ────────────────────────────────────────────
            CreateLabel(_panel.transform, "DirLabel", "DIRECTION",
                new Vector2(-60f, cursorY), new Vector2(100f, 20f),
                DIM_COLOR, 11, FontStyle.Normal, TextAnchor.MiddleLeft);
            cursorY -= 24f;

            // Prograde / Retrograde as a pair of toggles sharing a ToggleGroup
            ToggleGroup dirGroup = _panel.AddComponent<ToggleGroup>();
            dirGroup.allowSwitchOff = false;

            GameObject dirRow = CreateRow(_panel.transform, "DirRow",
                new Vector2(0f, cursorY), new Vector2(PANEL_W - 30f, 24f));

            _progradeToggle = CreateToggle(dirRow.transform, "Prograde",
                new Vector2(-60f, 0f), new Vector2(120f, 24f), ACCENT_COLOR);
            _progradeToggle.group = dirGroup;
            _progradeToggle.isOn = true;

            _retrogradeToggle = CreateToggle(dirRow.transform, "Retrograde",
                new Vector2(60f, 0f), new Vector2(120f, 24f), ACCENT_COLOR);
            _retrogradeToggle.group = dirGroup;
            _retrogradeToggle.isOn = false;

            cursorY -= 42f;

            // Thin separator
            CreateLine(_panel.transform, cursorY);
            cursorY -= 16f;

            // ── Confirm button ───────────────────────────────────────────
            _confirmButton = CreateButton(_panel.transform, "CONFIRM ORBIT",
                new Vector2(0f, cursorY), new Vector2(180f, 34f));
            _confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        // ─────────────────────────────────────────────────────────────────
        // UI helpers  (keep BuildUI readable)
        // ─────────────────────────────────────────────────────────────────

        private GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size,
            Color bgColor, bool addOutline = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.color = bgColor;

            if (addOutline)
            {
                Outline outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0.40f, 0.85f, 1.00f, 0.35f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            return go;
        }

        private Text CreateLabel(Transform parent, string name, string text,
            Vector2 anchoredPos, Vector2 size,
            Color color, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Text t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = alignment;

            return t;
        }

        private Slider CreateSlider(Transform parent, string name,
            Vector2 anchoredPos, Vector2 size,
            float min, float max, float value)
        {
            // Root
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Slider slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            // Background track
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            RectTransform bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.25f);
            bgRT.anchorMax = new Vector2(1f, 0.75f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(1f, 1f, 1f, 0.12f);

            // Fill area
            GameObject fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(go.transform, false);
            RectTransform fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5f, 0f);
            fillAreaRT.offsetMax = new Vector2(-15f, 0f);

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            RectTransform fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.sizeDelta = new Vector2(10f, 0f);
            Image fillImg = fillGO.AddComponent<Image>();
            fillImg.color = ACCENT_COLOR;
            slider.fillRect = fillRT;

            // Handle
            GameObject handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(go.transform, false);
            RectTransform handleAreaRT = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRT.anchorMin = Vector2.zero;
            handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(10f, 0f);
            handleAreaRT.offsetMax = new Vector2(-10f, 0f);

            GameObject handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            RectTransform handleRT = handleGO.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(16f, 16f);
            Image handleImg = handleGO.AddComponent<Image>();
            handleImg.color = Color.white;
            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;

            // Wire up navigation
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private Toggle CreateToggle(Transform parent, string labelText,
            Vector2 anchoredPos, Vector2 size, Color activeColor)
        {
            GameObject go = new GameObject("Toggle_" + labelText);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Toggle toggle = go.AddComponent<Toggle>();

            // Background box
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            RectTransform bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.5f);
            bgRT.anchorMax = new Vector2(0f, 0.5f);
            bgRT.pivot = new Vector2(0f, 0.5f);
            bgRT.anchoredPosition = new Vector2(4f, 0f);
            bgRT.sizeDelta = new Vector2(16f, 16f);
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(1f, 1f, 1f, 0.15f);
            toggle.targetGraphic = bgImg;

            // Checkmark
            GameObject checkGO = new GameObject("Checkmark");
            checkGO.transform.SetParent(bgGO.transform, false);
            RectTransform checkRT = checkGO.AddComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.1f, 0.1f);
            checkRT.anchorMax = new Vector2(0.9f, 0.9f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;
            Image checkImg = checkGO.AddComponent<Image>();
            checkImg.color = activeColor;
            toggle.graphic = checkImg;

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 1f);
            labelRT.offsetMin = new Vector2(24f, 0f);
            labelRT.offsetMax = Vector2.zero;
            Text labelT = labelGO.AddComponent<Text>();
            labelT.text = labelText;
            labelT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelT.fontSize = 12;
            labelT.color = TEXT_COLOR;
            labelT.alignment = TextAnchor.MiddleLeft;

            return toggle;
        }

        private InputField CreateInputField(Transform parent, string name,
            Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.08f);

            InputField field = go.AddComponent<InputField>();
            field.contentType = InputField.ContentType.DecimalNumber;

            // Text component inside
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(4f, 2f);
            textRT.offsetMax = new Vector2(-4f, -2f);
            Text textComp = textGO.AddComponent<Text>();
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = 13;
            textComp.color = TEXT_COLOR;
            textComp.alignment = TextAnchor.MiddleCenter;
            field.textComponent = textComp;
            field.targetGraphic = bg;

            // Placeholder
            GameObject phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(go.transform, false);
            RectTransform phRT = phGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(4f, 2f);
            phRT.offsetMax = new Vector2(-4f, -2f);
            Text phText = phGO.AddComponent<Text>();
            phText.text = "m/s";
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phText.fontSize = 11;
            phText.fontStyle = FontStyle.Italic;
            phText.color = DIM_COLOR;
            phText.alignment = TextAnchor.MiddleCenter;
            field.placeholder = phText;

            return field;
        }

        private Button CreateButton(Transform parent, string labelText,
            Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject("Button_" + labelText);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.40f, 0.85f, 1.00f, 0.20f);

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;

            // Hover colour
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.40f, 0.85f, 1.00f, 0.20f);
            cb.highlightedColor = new Color(0.40f, 0.85f, 1.00f, 0.45f);
            cb.pressedColor = new Color(0.40f, 0.85f, 1.00f, 0.70f);
            btn.colors = cb;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.40f, 0.85f, 1.00f, 0.60f);

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            RectTransform lRT = labelGO.AddComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero;
            lRT.anchorMax = Vector2.one;
            lRT.offsetMin = Vector2.zero;
            lRT.offsetMax = Vector2.zero;
            Text lText = labelGO.AddComponent<Text>();
            lText.text = labelText;
            lText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lText.fontSize = 14;
            lText.fontStyle = FontStyle.Bold;
            lText.color = ACCENT_COLOR;
            lText.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        private GameObject CreateRow(Transform parent, string name,
            Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return go;
        }

        private void CreateLine(Transform parent, float yPos)
        {
            GameObject go = new GameObject("Separator");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(PANEL_W - 30f, 1f);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.10f);
        }
    }
}
