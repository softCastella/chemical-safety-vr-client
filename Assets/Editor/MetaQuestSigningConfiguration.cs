using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MetaQuestSigningConfiguration
{
    private const string LocalConfigurationFileName = ".meta-quest-signing.local";
    private const string ExampleConfigurationFileName = ".meta-quest-signing.example";
    private const string KeystorePathVariable = "CHEMICAL_SAFETY_VR_KEYSTORE_PATH";
    private const string KeystorePasswordVariable = "CHEMICAL_SAFETY_VR_KEYSTORE_PASSWORD";
    private const string KeyAliasNameVariable = "CHEMICAL_SAFETY_VR_KEYALIAS_NAME";
    private const string KeyAliasPasswordVariable = "CHEMICAL_SAFETY_VR_KEYALIAS_PASSWORD";

    [MenuItem("Tools/XR/Validate Local Meta Quest Signing")]
    public static void ValidateLocalConfiguration()
    {
        LoadLocalConfiguration();
        Debug.Log(
            "[Meta Quest Signing] PASS: local keystore path, alias, and passwords are available. " +
            "Secret values were not logged.");
    }

    [MenuItem("Tools/XR/Validate Meta Quest Signing Isolation")]
    public static void ValidateProjectIsolation()
    {
        if (PlayerSettings.Android.useCustomKeystore ||
            !string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName) ||
            !string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName))
        {
            throw new InvalidOperationException(
                "[Meta Quest Signing] ProjectSettings must not contain a local Android signing configuration. " +
                $"Keep it in {LocalConfigurationFileName} or the documented environment variables.");
        }

        string projectRoot = GetProjectRoot();
        string gitIgnorePath = Path.Combine(projectRoot, ".gitignore");
        string examplePath = Path.Combine(projectRoot, ExampleConfigurationFileName);
        if (!File.Exists(gitIgnorePath) ||
            !File.ReadAllText(gitIgnorePath).Contains("/" + LocalConfigurationFileName))
        {
            throw new InvalidOperationException(
                $"[Meta Quest Signing] {LocalConfigurationFileName} is not protected by .gitignore.");
        }

        if (!File.Exists(examplePath))
        {
            throw new InvalidOperationException(
                $"[Meta Quest Signing] Missing configuration template: {ExampleConfigurationFileName}.");
        }

        Debug.Log(
            "[Meta Quest Signing] PASS: tracked ProjectSettings contain no machine-specific signing values, " +
            "and the ignored local configuration contract is present.");
    }

    public static IDisposable ApplyForBuild()
    {
        LocalSigningConfiguration configuration = LoadLocalConfiguration();
        SigningScope scope = new SigningScope();
        try
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = configuration.KeystorePath;
            PlayerSettings.Android.keyaliasName = configuration.KeyAliasName;
            PlayerSettings.Android.keystorePass = configuration.KeystorePassword;
            PlayerSettings.Android.keyaliasPass = configuration.KeyAliasPassword;
            return scope;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    private static LocalSigningConfiguration LoadLocalConfiguration()
    {
        string projectRoot = GetProjectRoot();
        string localConfigurationPath = Path.Combine(projectRoot, LocalConfigurationFileName);
        Dictionary<string, string> localValues = ReadLocalValues(localConfigurationPath);

        string keystorePath = RequireValue(KeystorePathVariable, localValues, localConfigurationPath);
        string keystorePassword = RequireValue(KeystorePasswordVariable, localValues, localConfigurationPath);
        string keyAliasName = RequireValue(KeyAliasNameVariable, localValues, localConfigurationPath);
        string keyAliasPassword = RequireValue(KeyAliasPasswordVariable, localValues, localConfigurationPath);

        keystorePath = Environment.ExpandEnvironmentVariables(keystorePath);
        if (!Path.IsPathRooted(keystorePath))
            keystorePath = Path.Combine(projectRoot, keystorePath);
        keystorePath = Path.GetFullPath(keystorePath);

        if (!File.Exists(keystorePath))
        {
            throw new InvalidOperationException(
                $"[Meta Quest Signing] Keystore file does not exist. Check {KeystorePathVariable} " +
                $"in {LocalConfigurationFileName} or the process environment.");
        }

        return new LocalSigningConfiguration(
            keystorePath,
            keystorePassword,
            keyAliasName,
            keyAliasPassword);
    }

    private static Dictionary<string, string> ReadLocalValues(string filePath)
    {
        Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(filePath))
            return values;

        string[] lines = File.ReadAllLines(filePath);
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                throw new InvalidOperationException(
                    $"[Meta Quest Signing] Invalid local configuration at line {index + 1}. " +
                    "Expected NAME=value.");
            }

            string name = line.Substring(0, separatorIndex).Trim();
            string value = line.Substring(separatorIndex + 1).Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
            {
                value = value.Substring(1, value.Length - 2);
            }

            values[name] = value;
        }

        return values;
    }

    private static string RequireValue(
        string variableName,
        IReadOnlyDictionary<string, string> localValues,
        string localConfigurationPath)
    {
        string environmentValue = Environment.GetEnvironmentVariable(variableName);
        if (!string.IsNullOrWhiteSpace(environmentValue))
            return environmentValue.Trim();

        if (localValues.TryGetValue(variableName, out string localValue) &&
            !string.IsNullOrWhiteSpace(localValue))
        {
            return localValue;
        }

        throw new InvalidOperationException(
            $"[Meta Quest Signing] Missing {variableName}. Copy {ExampleConfigurationFileName} to " +
            $"{Path.GetFileName(localConfigurationPath)} and fill it locally, or set the process environment variable.");
    }

    private static string GetProjectRoot()
    {
        return Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("[Meta Quest Signing] Project root could not be resolved.");
    }

    private sealed class LocalSigningConfiguration
    {
        public readonly string KeystorePath;
        public readonly string KeystorePassword;
        public readonly string KeyAliasName;
        public readonly string KeyAliasPassword;

        public LocalSigningConfiguration(
            string keystorePath,
            string keystorePassword,
            string keyAliasName,
            string keyAliasPassword)
        {
            KeystorePath = keystorePath;
            KeystorePassword = keystorePassword;
            KeyAliasName = keyAliasName;
            KeyAliasPassword = keyAliasPassword;
        }
    }

    private sealed class SigningScope : IDisposable
    {
        private readonly bool previousUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        private readonly string previousKeystoreName = PlayerSettings.Android.keystoreName;
        private readonly string previousKeyAliasName = PlayerSettings.Android.keyaliasName;
        private readonly string previousKeystorePassword = PlayerSettings.Android.keystorePass;
        private readonly string previousKeyAliasPassword = PlayerSettings.Android.keyaliasPass;
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;

            PlayerSettings.Android.useCustomKeystore = previousUseCustomKeystore;
            PlayerSettings.Android.keystoreName = previousKeystoreName;
            PlayerSettings.Android.keyaliasName = previousKeyAliasName;
            PlayerSettings.Android.keystorePass = previousKeystorePassword;
            PlayerSettings.Android.keyaliasPass = previousKeyAliasPassword;
            disposed = true;
        }
    }
}
