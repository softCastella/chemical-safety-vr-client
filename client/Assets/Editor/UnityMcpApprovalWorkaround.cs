using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Workaround for Unity AI Assistant 2.13.x losing its pending MCP approval TCS.
/// Remove this file after Unity fixes "No pending approval found for identity".
/// </summary>
[InitializeOnLoad]
internal static class UnityMcpApprovalWorkaround
{
    private static double s_DisconnectAt;

    static UnityMcpApprovalWorkaround()
    {
        EditorApplication.delayCall += Apply;
    }

    private static void Apply()
    {
        try
        {
            Type managerType = Type.GetType(
                "Unity.AI.MCP.Editor.Settings.MCPSettingsManager, Unity.AI.MCP.Editor");
            if (managerType == null)
            {
                Debug.LogWarning("Unity MCP workaround: MCPSettingsManager is unavailable.");
                return;
            }

            PropertyInfo settingsProperty = managerType.GetProperty(
                "Settings", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            object settings = settingsProperty?.GetValue(null);
            FieldInfo processValidation = settings?.GetType().GetField(
                "processValidationEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object policies = GetField(settings, "connectionPolicies");
            object direct = GetField(policies, "direct");
            FieldInfo requiresApproval = direct?.GetType().GetField(
                "requiresApproval", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (processValidation == null || requiresApproval == null)
                throw new MissingFieldException("Unity MCP validation settings were not found.");

            processValidation.SetValue(settings, false);
            requiresApproval.SetValue(direct, false);
            managerType.GetMethod(
                "SaveSettings", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, null);

            s_DisconnectAt = EditorApplication.timeSinceStartup + 3.0;
            EditorApplication.update -= DisconnectStaleTransports;
            EditorApplication.update += DisconnectStaleTransports;

            Debug.Log("Unity MCP workaround active: broken process validation and pending-approval queue bypassed.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Unity MCP workaround failed: {exception}");
        }
    }

    private static void DisconnectStaleTransports()
    {
        if (EditorApplication.timeSinceStartup < s_DisconnectAt)
            return;

        EditorApplication.update -= DisconnectStaleTransports;
        Type bridgeType = Type.GetType(
            "Unity.AI.MCP.Editor.UnityMCPBridge, Unity.AI.MCP.Editor");
        PropertyInfo enabled = bridgeType?.GetProperty(
            "Enabled", BindingFlags.Static | BindingFlags.Public);
        enabled?.SetValue(null, false);
        enabled?.SetValue(null, true);
        Debug.Log("Unity MCP workaround: Bridge recreated with validation bypass active.");
    }

    private static object GetField(object instance, string name)
    {
        return instance?.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(instance);
    }
}
