#if TEXT_MESH_PRO_PRESENT || (UGUI_2_0_PRESENT && UNITY_6000_0_OR_NEWER)
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

namespace Prototype.Tyche.UI.Keyboard
{
    /// <summary>
    /// Connects the project-owned Hangul composer to Unity's XRI Spatial Keyboard sample without
    /// modifying the imported sample scripts or prefabs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HangulKeyboardController : MonoBehaviour
    {
        public enum InputLanguage
        {
            Hangul,
            English,
        }

        [Header("XRI Keyboard References")]
        [SerializeField] XRKeyboard m_Keyboard;
        [SerializeField] XRKeyboardLayout m_KeyboardLayout;

        [Header("Serialized Layout Assets")]
        [SerializeField] XRKeyboardConfig m_HangulLayout;
        [SerializeField] XRKeyboardConfig m_EnglishLayout;
        [SerializeField] XRKeyboardConfig m_SymbolLayout;

        [Header("Behavior")]
        [SerializeField] bool m_ResetToHangulOnOpen = true;

        [Header("Submit Completion")]
        [Tooltip("Optional modal canvas to disable after the keyboard submits with Enter. The keyboard's authored text and other scene UI are left untouched.")]
        [SerializeField] GameObject m_CanvasToDisableOnSubmit;
        [SerializeField] bool m_DisableCanvasOnSubmit;

        [Header("Diagnostics")]
        [SerializeField] bool m_LogKeyPressesToConsole = true;

        readonly HangulComposer m_Composer = new HangulComposer();
        HangulShiftLegend[] m_ShiftLegends = Array.Empty<HangulShiftLegend>();
        string m_TrackedRenderedRun = string.Empty;
        InputLanguage m_Language = InputLanguage.Hangul;
        bool m_SymbolMode;
        bool m_RestoreCloseOnSubmit;
        bool m_AuthoredCloseOnSubmit = true;

        public event Action<string> TextSubmitted;

        public XRKeyboard keyboard => m_Keyboard;
        public InputLanguage language => m_Language;
        public bool isHangulMode => !m_SymbolMode && m_Language == InputLanguage.Hangul;
        public bool isSymbolMode => m_SymbolMode;

        void Reset()
        {
            m_Keyboard = GetComponent<XRKeyboard>();
            m_KeyboardLayout = GetComponentInChildren<XRKeyboardLayout>(true);
        }

        void Awake()
        {
            if (m_Keyboard == null)
                m_Keyboard = GetComponent<XRKeyboard>();
            if (m_KeyboardLayout == null)
                m_KeyboardLayout = GetComponentInChildren<XRKeyboardLayout>(true);

            m_ShiftLegends = GetComponentsInChildren<HangulShiftLegend>(true);
            InferModeFromActiveLayout();
        }

        void OnEnable()
        {
            if (m_Keyboard == null)
                return;

            m_Keyboard.onOpened.AddListener(OnKeyboardOpened);
            m_Keyboard.onClosed.AddListener(OnKeyboardClosed);
            m_Keyboard.onTextSubmitted.AddListener(OnTextSubmitted);
            m_Keyboard.onShifted.AddListener(OnKeyboardShifted);
            m_Keyboard.onKeyPressed.AddListener(OnKeyPressed);
            RefreshShiftLegends();
        }

        void OnDisable()
        {
            if (m_Keyboard == null)
                return;

            m_Keyboard.onOpened.RemoveListener(OnKeyboardOpened);
            m_Keyboard.onClosed.RemoveListener(OnKeyboardClosed);
            m_Keyboard.onTextSubmitted.RemoveListener(OnTextSubmitted);
            m_Keyboard.onShifted.RemoveListener(OnKeyboardShifted);
            m_Keyboard.onKeyPressed.RemoveListener(OnKeyPressed);
            CommitComposition();
        }

        /// <summary>Processes a key whose effective character is a physical two-set QWERTY key.</summary>
        public void ProcessCharacter(XRKeyboardKey key)
        {
            if (key == null)
                return;

            ProcessCharacter(key.GetEffectiveCharacter());
        }

        /// <summary>Processes text from the Hangul layout, composing supported QWERTY letters.</summary>
        public void ProcessCharacter(string value)
        {
            if (m_Keyboard == null || string.IsNullOrEmpty(value))
                return;

            var isHangulKey = isHangulMode && value.Length == 1 && HangulComposer.IsSupportedKey(value[0]);
            if (!isHangulKey)
            {
                CommitComposition();
                m_Keyboard.UpdateText(value);
                return;
            }

            EnsureTrackedRunIsValid();

            var previousRenderedRun = m_TrackedRenderedRun;
            if (!m_Composer.TryAppend(value[0]))
                return;

            var nextRenderedRun = m_Composer.renderedText;
            if (TryReplaceTrackedRun(previousRenderedRun, nextRenderedRun))
            {
                m_TrackedRenderedRun = nextRenderedRun;
                return;
            }

            // Character-limit rejection restores the previous text and composition state.
            m_Composer.TryBackspace(out _);
            m_TrackedRenderedRun = previousRenderedRun;
        }

        /// <summary>Backspaces one composition step, then falls back to the XRI keyboard.</summary>
        public void ProcessBackspace()
        {
            if (m_Keyboard == null)
                return;

            EnsureTrackedRunIsValid();
            if (!isHangulMode || !m_Composer.TryBackspace(out var removedKey))
            {
                CommitComposition();
                m_Keyboard.Backspace();
                return;
            }

            var previousRenderedRun = m_TrackedRenderedRun;
            var nextRenderedRun = m_Composer.renderedText;
            if (TryReplaceTrackedRun(previousRenderedRun, nextRenderedRun))
            {
                m_TrackedRenderedRun = nextRenderedRun;
                return;
            }

            m_Composer.TryAppend(removedKey);
            m_TrackedRenderedRun = previousRenderedRun;
        }

        /// <summary>Toggles between serialized Hangul and English configurations.</summary>
        public void ToggleLanguage()
        {
            CommitComposition();
            m_SymbolMode = false;
            m_Language = m_Language == InputLanguage.Hangul
                ? InputLanguage.English
                : InputLanguage.Hangul;
            ApplyCurrentLanguageLayout();
        }

        /// <summary>Toggles the serialized symbol layout and returns to the previous input language.</summary>
        public void ToggleSymbols()
        {
            CommitComposition();
            m_SymbolMode = !m_SymbolMode;

            if (m_KeyboardLayout != null)
            {
                m_KeyboardLayout.activeKeyMapping = m_SymbolMode
                    ? m_SymbolLayout
                    : GetCurrentLanguageLayout();
            }

            ResetModifiersAndRefresh();
        }

        /// <summary>Clears only the decomposable history; the visible XRI text remains committed.</summary>
        public void CommitComposition()
        {
            m_Composer.Clear();
            m_TrackedRenderedRun = string.Empty;
        }

#if UNITY_EDITOR
        /// <summary>Editor-builder entry point. References remain serialized in the generated prefab.</summary>
        public void Configure(
            XRKeyboard keyboardReference,
            XRKeyboardLayout layoutReference,
            XRKeyboardConfig hangulLayout,
            XRKeyboardConfig englishLayout,
            XRKeyboardConfig symbolLayout,
            bool resetToHangulOnOpen,
            bool logKeyPressesToConsole)
        {
            m_Keyboard = keyboardReference;
            m_KeyboardLayout = layoutReference;
            m_HangulLayout = hangulLayout;
            m_EnglishLayout = englishLayout;
            m_SymbolLayout = symbolLayout;
            m_ResetToHangulOnOpen = resetToHangulOnOpen;
            m_LogKeyPressesToConsole = logKeyPressesToConsole;
        }
#endif

        void OnKeyboardOpened(KeyboardTextEventArgs _)
        {
            CommitComposition();
            if (m_ResetToHangulOnOpen)
            {
                m_Language = InputLanguage.Hangul;
                m_SymbolMode = false;
                ApplyCurrentLanguageLayout();
            }
        }

        void OnKeyboardClosed(KeyboardTextEventArgs _)
        {
            CommitComposition();
        }

        void OnTextSubmitted(KeyboardTextEventArgs _)
        {
            CommitComposition();

            string submittedText = m_Keyboard != null ? m_Keyboard.text : string.Empty;
            if (string.IsNullOrWhiteSpace(submittedText))
            {
                PreventCloseAfterRejectedSubmit();
                return;
            }

            TextSubmitted?.Invoke(submittedText);

            Debug.Log(
                $"[Hangul Keyboard] Text submitted; disabling target=" +
                (m_CanvasToDisableOnSubmit != null ? m_CanvasToDisableOnSubmit.name : "<none>"),
                this);

            // Enter is delivered through the shared XR UI submit path as well. Block
            // the quit action briefly so closing this modal cannot also quit Play Mode.
            HangulKeyboardSubmitGuard.SuppressForFrames(2);

            if (m_DisableCanvasOnSubmit && m_CanvasToDisableOnSubmit != null)
                m_CanvasToDisableOnSubmit.SetActive(false);
        }

        void PreventCloseAfterRejectedSubmit()
        {
            if (m_Keyboard == null)
                return;

            m_AuthoredCloseOnSubmit = m_Keyboard.closeOnSubmit;
            m_Keyboard.closeOnSubmit = false;
            m_RestoreCloseOnSubmit = true;
            if (isActiveAndEnabled)
                StartCoroutine(RestoreCloseOnSubmitAfterRejectedSubmit());
        }

        IEnumerator RestoreCloseOnSubmitAfterRejectedSubmit()
        {
            yield return null;
            if (!m_RestoreCloseOnSubmit || m_Keyboard == null)
                yield break;

            m_Keyboard.closeOnSubmit = m_AuthoredCloseOnSubmit;
            m_RestoreCloseOnSubmit = false;
        }

        void OnKeyboardShifted(KeyboardModifiersEventArgs _)
        {
            RefreshShiftLegends();
        }

        void OnKeyPressed(KeyboardKeyEventArgs args)
        {
            if (!m_LogKeyPressesToConsole || args?.key == null)
                return;

            var key = args.key;
            var label = key.textComponent != null ? key.textComponent.text : string.Empty;
            var mode = m_SymbolMode ? "Symbols" : m_Language.ToString();
            Debug.Log(
                $"[Hangul Keyboard] Key pressed | label='{EscapeForLog(label)}' " +
                $"| value='{EscapeForLog(key.GetEffectiveCharacter())}' " +
                $"| base='{EscapeForLog(key.character)}' | shift='{EscapeForLog(key.shiftCharacter)}' " +
                $"| keyCode={key.keyCode} | mode={mode} " +
                $"| text='{EscapeForLog(m_Keyboard != null ? m_Keyboard.text : string.Empty)}'",
                key);
        }

        void InferModeFromActiveLayout()
        {
            if (m_KeyboardLayout == null)
                return;

            if (ReferenceEquals(m_KeyboardLayout.activeKeyMapping, m_EnglishLayout))
                m_Language = InputLanguage.English;
            else
                m_Language = InputLanguage.Hangul;

            m_SymbolMode = ReferenceEquals(m_KeyboardLayout.activeKeyMapping, m_SymbolLayout);
        }

        void ApplyCurrentLanguageLayout()
        {
            if (m_KeyboardLayout != null)
                m_KeyboardLayout.activeKeyMapping = GetCurrentLanguageLayout();

            ResetModifiersAndRefresh();
        }

        XRKeyboardConfig GetCurrentLanguageLayout()
        {
            return m_Language == InputLanguage.Hangul ? m_HangulLayout : m_EnglishLayout;
        }

        void ResetModifiersAndRefresh()
        {
            if (m_Keyboard != null)
                m_Keyboard.CapsLock(false);

            RefreshShiftLegends();
        }

        void RefreshShiftLegends()
        {
            var shifted = m_Keyboard != null && m_Keyboard.shifted;
            for (var index = 0; index < m_ShiftLegends.Length; ++index)
                m_ShiftLegends[index].SetState(isHangulMode, shifted);
        }

        void EnsureTrackedRunIsValid()
        {
            if (m_Keyboard == null || string.IsNullOrEmpty(m_TrackedRenderedRun))
                return;

            var start = m_Keyboard.caretPosition - m_TrackedRenderedRun.Length;
            var isValid = start >= 0
                && start + m_TrackedRenderedRun.Length <= m_Keyboard.text.Length
                && string.CompareOrdinal(
                    m_Keyboard.text,
                    start,
                    m_TrackedRenderedRun,
                    0,
                    m_TrackedRenderedRun.Length) == 0;

            if (!isValid)
                CommitComposition();
        }

        bool TryReplaceTrackedRun(string previousRun, string nextRun)
        {
            var previousText = m_Keyboard.text;
            var previousCaret = m_Keyboard.caretPosition;
            var replaceStart = previousCaret - previousRun.Length;
            if (replaceStart < 0)
                return false;

            for (var index = 0; index < previousRun.Length; ++index)
                m_Keyboard.Backspace();

            if (!string.IsNullOrEmpty(nextRun))
                m_Keyboard.UpdateText(nextRun);

            var expectedText = previousText.Remove(replaceStart, previousRun.Length)
                .Insert(replaceStart, nextRun);
            var expectedCaret = replaceStart + nextRun.Length;
            if (m_Keyboard.text == expectedText && m_Keyboard.caretPosition == expectedCaret)
                return true;

            // Restore the last valid visible state if XRI rejected the replacement.
            while (m_Keyboard.caretPosition > replaceStart)
                m_Keyboard.Backspace();
            if (!string.IsNullOrEmpty(previousRun))
                m_Keyboard.UpdateText(previousRun);
            return false;
        }

        static string EscapeForLog(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }
    }
}
#endif
