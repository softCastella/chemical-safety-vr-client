using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Moves between lesson scenes from inside the build.
///
/// Everything comes from the build settings rather than from a list kept here, so adding, removing or
/// reordering lessons needs no change to this component or to the panels already placed in each scene.
/// The index shown counts only enabled scenes, which is what <see cref="SceneManager"/> reports and what
/// the build actually contains.
/// </summary>
public class LessonNavigator : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Shows the current scene name and its position in the build.")]
    TMP_Text m_Label;

    void Start() => Refresh();

    /// <summary>Loads the next lesson, wrapping around at the end.</summary>
    public void Next() => Step(1);

    /// <summary>Loads the previous lesson, wrapping around at the start.</summary>
    public void Previous() => Step(-1);

    void Step(int direction)
    {
        var count = SceneManager.sceneCountInBuildSettings;
        if (count == 0)
        {
            Debug.LogWarning("[Nav] No scenes in the build settings; nothing to navigate to.", this);
            return;
        }

        var current = SceneManager.GetActiveScene().buildIndex;
        if (current < 0)
        {
            // -1 means this scene is not in the build settings - normal when a scene is opened directly in
            // the editor before being registered. Starting from the first lesson is the useful fallback.
            Debug.LogWarning($"[Nav] '{SceneManager.GetActiveScene().name}' is not in the build settings. " +
                "Loading the first lesson instead.", this);
            SceneManager.LoadScene(0);
            return;
        }

        // The extra + count keeps the result positive when stepping back from the first scene; C# gives a
        // negative remainder for negative operands.
        SceneManager.LoadScene(((current + direction) % count + count) % count);
    }

    void Refresh()
    {
        if (m_Label == null)
            return;

        var scene = SceneManager.GetActiveScene();
        var count = SceneManager.sceneCountInBuildSettings;

        m_Label.text = scene.buildIndex >= 0
            ? $"{scene.name}\n<size=60%>{scene.buildIndex + 1} / {count}</size>"
            : $"{scene.name}\n<size=60%>not in build</size>";
    }
}
