#if UNITY_EDITOR
using System;
using Prototype.Tyche.UI.Keyboard;
using UnityEditor;
using UnityEngine;

/// <summary>Deterministic validation for the project-owned two-set Hangul composer.</summary>
public static class HangulComposerValidationHarness
{
    [MenuItem("Tools/XR/Hangul Keyboard/Validate Composer")]
    public static void Validate()
    {
        AssertComposes("gksrmf", "한글");
        AssertComposes("dkssudgktpdy", "안녕하세요");
        AssertComposes("dP", "예");
        AssertComposes("rP", "계");
        AssertComposes("dO", "얘");
        AssertComposes("Rk", "까");
        AssertComposes("dhk", "와");
        AssertComposes("dnj", "워");
        AssertComposes("rkrk", "가가");
        AssertComposes("rkqt", "값");
        AssertComposes("rkqtk", "갑사");

        var composer = new HangulComposer();
        AppendAll(composer, "gks");
        AssertEqual("한", composer.renderedText, "Backspace setup");
        AssertBackspace(composer, "하");
        AssertBackspace(composer, "ㅎ");
        AssertBackspace(composer, string.Empty);

        Debug.Log("[Hangul Keyboard] Composer validation passed: 11 composition cases and staged Backspace.");
    }

    public static void ValidateBatch()
    {
        try
        {
            Validate();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    static void AssertComposes(string physicalKeys, string expected)
    {
        AssertEqual(expected, HangulComposer.ComposeKeys(physicalKeys), physicalKeys);
    }

    static void AppendAll(HangulComposer composer, string physicalKeys)
    {
        foreach (var key in physicalKeys)
        {
            if (!composer.TryAppend(key))
                throw new InvalidOperationException($"Unsupported validation key: '{key}'.");
        }
    }

    static void AssertBackspace(HangulComposer composer, string expected)
    {
        if (!composer.TryBackspace(out _))
            throw new InvalidOperationException("Backspace unexpectedly had no composition key to remove.");

        AssertEqual(expected, composer.renderedText, "Backspace");
    }

    static void AssertEqual(string expected, string actual, string context)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Hangul composition failed for {context}. Expected '{expected}', actual '{actual}'.");
        }
    }
}
#endif
