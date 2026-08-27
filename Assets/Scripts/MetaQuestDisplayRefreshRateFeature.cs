using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

#if UNITY_EDITOR
[OpenXRFeature(
    UiName = "Meta Quest 72 Hz",
    Desc = "Requests a 72 Hz headset display refresh rate through XR_FB_display_refresh_rate on Android OpenXR.",
    Company = "Tyche",
    Version = "1.0.0",
    FeatureId = FeatureId,
    OpenxrExtensionStrings = ExtensionName,
    BuildTargetGroups = new[] { BuildTargetGroup.Android })]
#endif
public sealed class MetaQuestDisplayRefreshRateFeature : OpenXRFeature
{
    public const string FeatureId = "com.tyche.openxr.feature.metaquest.display-refresh-rate";
    public const string ExtensionName = "XR_FB_display_refresh_rate";

    private const float SupportedRateTolerance = 0.1f;

    [SerializeField, Min(1f)]
    private float m_TargetRefreshRate = 72f;

    private XrEnumerateDisplayRefreshRatesFBDelegate m_EnumerateDisplayRefreshRates;
    private XrGetDisplayRefreshRateFBDelegate m_GetDisplayRefreshRate;
    private XrRequestDisplayRefreshRateFBDelegate m_RequestDisplayRefreshRate;
    private ulong m_Session;
    private bool m_HasRequestedForCurrentSession;

    public float TargetRefreshRate => m_TargetRefreshRate;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int XrGetInstanceProcAddrDelegate(
        ulong instance,
        [MarshalAs(UnmanagedType.LPStr)] string functionName,
        out IntPtr function);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int XrEnumerateDisplayRefreshRatesFBDelegate(
        ulong session,
        uint capacityInput,
        out uint countOutput,
        IntPtr displayRefreshRates);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int XrGetDisplayRefreshRateFBDelegate(ulong session, out float displayRefreshRate);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int XrRequestDisplayRefreshRateFBDelegate(ulong session, float displayRefreshRate);

    protected override bool OnInstanceCreate(ulong xrInstance)
    {
        if (!OpenXRRuntime.IsExtensionEnabled(ExtensionName))
        {
            Debug.LogError(
                $"[Meta Quest Refresh Rate] Required OpenXR extension '{ExtensionName}' is not enabled. " +
                $"The requested {m_TargetRefreshRate:0.##} Hz rate cannot be applied.");
            return false;
        }

        if (xrGetInstanceProcAddr == IntPtr.Zero)
        {
            Debug.LogError("[Meta Quest Refresh Rate] xrGetInstanceProcAddr is unavailable.");
            return false;
        }

        var getInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<XrGetInstanceProcAddrDelegate>(
            xrGetInstanceProcAddr);

        return TryLoadFunction(getInstanceProcAddr, xrInstance, "xrEnumerateDisplayRefreshRatesFB",
                   out m_EnumerateDisplayRefreshRates)
               && TryLoadFunction(getInstanceProcAddr, xrInstance, "xrGetDisplayRefreshRateFB",
                   out m_GetDisplayRefreshRate)
               && TryLoadFunction(getInstanceProcAddr, xrInstance, "xrRequestDisplayRefreshRateFB",
                   out m_RequestDisplayRefreshRate);
    }

    protected override void OnSessionCreate(ulong xrSession)
    {
        m_Session = xrSession;
        m_HasRequestedForCurrentSession = false;
    }

    protected override void OnSessionBegin(ulong xrSession)
    {
        m_Session = xrSession;
        Application.targetFrameRate = Mathf.RoundToInt(m_TargetRefreshRate);
        RequestConfiguredRefreshRate();
    }

    protected override void OnSessionDestroy(ulong xrSession)
    {
        m_Session = 0;
        m_HasRequestedForCurrentSession = false;
    }

    protected override void OnInstanceDestroy(ulong xrInstance)
    {
        m_EnumerateDisplayRefreshRates = null;
        m_GetDisplayRefreshRate = null;
        m_RequestDisplayRefreshRate = null;
        m_Session = 0;
        m_HasRequestedForCurrentSession = false;
    }

    private void RequestConfiguredRefreshRate()
    {
        if (m_HasRequestedForCurrentSession)
            return;

        m_HasRequestedForCurrentSession = true;

        if (m_Session == 0 || m_EnumerateDisplayRefreshRates == null || m_RequestDisplayRefreshRate == null)
        {
            Debug.LogError("[Meta Quest Refresh Rate] OpenXR session or refresh-rate functions are unavailable.");
            return;
        }

        int result = m_EnumerateDisplayRefreshRates(m_Session, 0, out uint rateCount, IntPtr.Zero);
        if (result != 0 || rateCount == 0)
        {
            Debug.LogError(
                $"[Meta Quest Refresh Rate] Could not enumerate supported display refresh rates " +
                $"(XrResult={result}, count={rateCount}).");
            return;
        }

        IntPtr ratesBuffer = IntPtr.Zero;
        try
        {
            ratesBuffer = Marshal.AllocHGlobal(checked((int)rateCount * sizeof(float)));
            result = m_EnumerateDisplayRefreshRates(m_Session, rateCount, out uint returnedRateCount, ratesBuffer);
            if (result != 0 || returnedRateCount == 0 || returnedRateCount > rateCount)
            {
                Debug.LogError(
                    $"[Meta Quest Refresh Rate] Could not read supported display refresh rates " +
                    $"(XrResult={result}, count={returnedRateCount}).");
                return;
            }

            var supportedRates = new float[returnedRateCount];
            Marshal.Copy(ratesBuffer, supportedRates, 0, (int)returnedRateCount);

            bool supportsTargetRate = false;
            for (int index = 0; index < supportedRates.Length; index++)
            {
                if (Mathf.Abs(supportedRates[index] - m_TargetRefreshRate) <= SupportedRateTolerance)
                {
                    supportsTargetRate = true;
                    break;
                }
            }

            if (!supportsTargetRate)
            {
                Debug.LogError(
                    $"[Meta Quest Refresh Rate] The runtime does not support the requested " +
                    $"{m_TargetRefreshRate:0.##} Hz rate. Supported rates: {string.Join(", ", supportedRates)}. " +
                    "No fallback rate was selected.");
                return;
            }
        }
        finally
        {
            if (ratesBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(ratesBuffer);
        }

        result = m_RequestDisplayRefreshRate(m_Session, m_TargetRefreshRate);
        if (result != 0)
        {
            Debug.LogError(
                $"[Meta Quest Refresh Rate] Requesting {m_TargetRefreshRate:0.##} Hz failed " +
                $"(XrResult={result}).");
            return;
        }

        if (m_GetDisplayRefreshRate != null && m_GetDisplayRefreshRate(m_Session, out float currentRate) == 0)
        {
            Debug.Log(
                $"[Meta Quest Refresh Rate] Requested {m_TargetRefreshRate:0.##} Hz. " +
                $"Runtime reported {currentRate:0.##} Hz immediately after the request.");
        }
        else
        {
            Debug.Log($"[Meta Quest Refresh Rate] Requested {m_TargetRefreshRate:0.##} Hz.");
        }
    }

    private static bool TryLoadFunction<TDelegate>(
        XrGetInstanceProcAddrDelegate getInstanceProcAddr,
        ulong xrInstance,
        string functionName,
        out TDelegate function)
        where TDelegate : Delegate
    {
        int result = getInstanceProcAddr(xrInstance, functionName, out IntPtr functionPointer);
        if (result != 0 || functionPointer == IntPtr.Zero)
        {
            Debug.LogError(
                $"[Meta Quest Refresh Rate] Could not load {functionName} " +
                $"(XrResult={result}).");
            function = null;
            return false;
        }

        function = Marshal.GetDelegateForFunctionPointer<TDelegate>(functionPointer);
        return true;
    }
}
