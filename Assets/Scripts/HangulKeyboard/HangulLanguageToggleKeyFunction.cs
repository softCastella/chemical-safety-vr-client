#if TEXT_MESH_PRO_PRESENT || (UGUI_2_0_PRESENT && UNITY_6000_0_OR_NEWER)
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

namespace Prototype.Tyche.UI.Keyboard
{
    [CreateAssetMenu(
        fileName = "Hangul Language Toggle Key Function",
        menuName = "XR/Spatial Keyboard/Hangul Language Toggle Key Function")]
    public sealed class HangulLanguageToggleKeyFunction : KeyFunction
    {
        public override void ProcessKey(XRKeyboard keyboardContext, XRKeyboardKey key)
        {
            if (keyboardContext != null
                && keyboardContext.TryGetComponent<HangulKeyboardController>(out var controller))
            {
                controller.ToggleLanguage();
            }
        }
    }
}
#endif
