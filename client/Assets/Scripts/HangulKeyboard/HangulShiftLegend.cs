#if TEXT_MESH_PRO_PRESENT || (UGUI_2_0_PRESENT && UNITY_6000_0_OR_NEWER)
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

namespace Prototype.Tyche.UI.Keyboard
{
    /// <summary>Displays the Korean Shift character in the key's primary text position.</summary>
    [DisallowMultipleComponent]
    public sealed class HangulShiftLegend : MonoBehaviour
    {
        [SerializeField] XRKeyboardKey m_Key;
        [SerializeField] TMP_Text m_PrimaryText;
        [SerializeField] TMP_Text m_ShiftLegendText;
        [SerializeField] string m_HangulShiftLegend;

        [Header("Authored State Colors")]
        [SerializeField] Color m_PrimaryNormalColor = Color.white;
        [SerializeField] Color m_PrimaryShiftedColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] Color m_LegendNormalColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] Color m_LegendShiftedColor = Color.white;

        public void SetState(bool hangulMode, bool shifted)
        {
            if (m_ShiftLegendText != null)
            {
                // The alternate label is no longer rendered as a second, smaller glyph.
                m_ShiftLegendText.gameObject.SetActive(false);
            }

            if (m_PrimaryText != null)
            {
                string baseCharacter = m_Key != null && !string.IsNullOrEmpty(m_Key.displayCharacter)
                    ? m_Key.displayCharacter
                    : m_Key != null ? m_Key.character : m_PrimaryText.text;
                string shiftCharacter = m_Key != null && !string.IsNullOrEmpty(m_Key.shiftDisplayCharacter)
                    ? m_Key.shiftDisplayCharacter
                    : m_Key != null && !string.IsNullOrEmpty(m_Key.shiftCharacter)
                        ? m_Key.shiftCharacter
                        : m_HangulShiftLegend;

                if (hangulMode && shifted && !string.IsNullOrEmpty(shiftCharacter) && !shiftCharacter.StartsWith("\\"))
                    m_PrimaryText.text = shiftCharacter;
                else
                    m_PrimaryText.text = baseCharacter;

                m_PrimaryText.color = m_PrimaryNormalColor;
            }
        }

#if UNITY_EDITOR
        public void Configure(
            XRKeyboardKey key,
            TMP_Text primaryText,
            TMP_Text shiftLegendText,
            string hangulShiftLegend,
            Color primaryNormalColor,
            Color primaryShiftedColor,
            Color legendNormalColor,
            Color legendShiftedColor)
        {
            m_Key = key;
            m_PrimaryText = primaryText;
            m_ShiftLegendText = shiftLegendText;
            m_HangulShiftLegend = hangulShiftLegend;
            m_PrimaryNormalColor = primaryNormalColor;
            m_PrimaryShiftedColor = primaryShiftedColor;
            m_LegendNormalColor = legendNormalColor;
            m_LegendShiftedColor = legendShiftedColor;
        }
#endif
    }
}
#endif
