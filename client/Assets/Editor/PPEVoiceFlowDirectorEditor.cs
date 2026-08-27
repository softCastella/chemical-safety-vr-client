using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PPEVoiceFlowDirector))]
public sealed class PPEVoiceFlowDirectorEditor : Editor
{
    readonly List<AudioClip> previewClips = new();
    int previewIndex;
    double nextPreviewTime;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(property);
            }
            else if (property.name == "m_VoiceSteps"
                || property.name == "m_ControllerEduVoiceSteps"
                || property.name == "m_ControllerSimpVoiceSteps")
            {
                DrawVoiceSteps(property, property.displayName);
            }
            else
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawVoiceSteps(SerializedProperty steps, string label)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        if (steps.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Voice Steps가 비어 있습니다.", MessageType.Warning);
            return;
        }

        EditorGUI.indentLevel++;
        for (int index = 0; index < steps.arraySize; index++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(index);
            SerializedProperty stepId = step.FindPropertyRelative("stepId");
            SerializedProperty state = step.FindPropertyRelative("state");

            string id = string.IsNullOrWhiteSpace(stepId.stringValue)
                ? $"Step {index + 1}"
                : stepId.stringValue;
            string stateName = state.enumDisplayNames[state.enumValueIndex];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"{index + 1}. {id} ({stateName})",
                EditorStyles.boldLabel);

            if (GUILayout.Button("▶ Play", GUILayout.Width(70f)))
                PlayStep(step);
            if (GUILayout.Button("Stop", GUILayout.Width(48f)))
                StopPreview();
            EditorGUILayout.EndHorizontal();

            DrawStepChildren(step);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }
        EditorGUI.indentLevel--;
    }

    static void DrawStepChildren(SerializedProperty step)
    {
        SerializedProperty child = step.Copy();
        SerializedProperty end = child.GetEndProperty();
        bool enterChildren = true;

        EditorGUI.indentLevel++;
        while (child.NextVisible(enterChildren)
            && !SerializedProperty.EqualContents(child, end))
        {
            EditorGUILayout.PropertyField(child, true);
            enterChildren = false;
        }
        EditorGUI.indentLevel--;
    }

    void PlayStep(SerializedProperty step)
    {
        StopPreview();

        SerializedProperty enabled = step.FindPropertyRelative("enabled");
        if (enabled != null && !enabled.boolValue)
        {
            EditorUtility.DisplayDialog(
                "Voice Step",
                "이 Voice Step은 비활성화되어 있습니다. 활성화한 뒤 재생하세요.",
                "확인");
            return;
        }

        SerializedProperty clips = step.FindPropertyRelative("clips");
        for (int index = 0; index < clips.arraySize; index++)
        {
            if (clips.GetArrayElementAtIndex(index).objectReferenceValue is AudioClip clip)
                previewClips.Add(clip);
        }

        if (previewClips.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Voice Step",
                "이 Step에 연결된 AudioClip이 없습니다.",
                "확인");
            return;
        }

        previewIndex = 0;
        PlayNextPreviewClip();
    }

    void PlayNextPreviewClip()
    {
        if (previewIndex >= previewClips.Count)
        {
            StopPreview();
            return;
        }

        AudioClip clip = previewClips[previewIndex];
        PlayPreviewClip(clip);
        nextPreviewTime = EditorApplication.timeSinceStartup + Mathf.Max(0.05f, clip.length);
        EditorApplication.update -= UpdatePreview;
        EditorApplication.update += UpdatePreview;
    }

    void UpdatePreview()
    {
        if (EditorApplication.timeSinceStartup < nextPreviewTime)
            return;

        previewIndex++;
        PlayNextPreviewClip();
    }

    void StopPreview()
    {
        EditorApplication.update -= UpdatePreview;
        previewClips.Clear();
        previewIndex = 0;

        try
        {
            FindAudioUtilMethod("StopAllPreviewClips")?.Invoke(null, null);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Voice preview stop failed: {exception.Message}");
        }
    }

    static void PlayPreviewClip(AudioClip clip)
    {
        MethodInfo playPreviewClipMethod = FindAudioUtilMethod("PlayPreviewClip");
        if (playPreviewClipMethod == null)
        {
            Debug.LogWarning("Unity Editor AudioUtil.PlayPreviewClip을 찾지 못했습니다.");
            return;
        }

        try
        {
            ParameterInfo[] parameters = playPreviewClipMethod.GetParameters();
            object[] arguments = new object[parameters.Length];
            arguments[0] = clip;
            for (int index = 1; index < arguments.Length; index++)
            {
                Type parameterType = parameters[index].ParameterType;
                arguments[index] = parameterType == typeof(bool)
                    ? false
                    : parameterType == typeof(int)
                        ? 0
                        : parameterType == typeof(float)
                            ? 1f
                            : parameters[index].HasDefaultValue
                                ? parameters[index].DefaultValue
                                : null;
            }

            playPreviewClipMethod.Invoke(null, arguments);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Voice preview failed for '{clip.name}': {exception.Message}");
        }
    }

    static MethodInfo FindAudioUtilMethod(string methodName)
    {
        Type audioUtil = FindAudioUtilType();
        if (audioUtil == null)
            return null;

        foreach (MethodInfo method in audioUtil.GetMethods(
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.Name != methodName)
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (methodName == "PlayPreviewClip"
                && (parameters.Length == 0 || parameters[0].ParameterType != typeof(AudioClip)))
                continue;

            return method;
        }

        return null;
    }

    static Type FindAudioUtilType()
    {
        // Unity 6000 loads UnityEditor.AudioUtil from a different editor
        // assembly than UnityEditor.Editor. Search loaded editor assemblies so
        // the Inspector preview buttons resolve the actual API.
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type audioUtil = assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil != null)
                return audioUtil;
        }

        return null;
    }

    void OnDisable()
    {
        StopPreview();
    }
}
