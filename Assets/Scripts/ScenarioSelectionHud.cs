using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class ScenarioSelectionHud : MonoBehaviour
{
    [Serializable]
    public sealed class ScenarioOption
    {
        [SerializeField] private string title;
        [SerializeField] private Button[] selectionButtons;
        [SerializeField] private UnityEvent onSelected;

        public string Title => title;
        public Button[] SelectionButtons => selectionButtons;
        public UnityEvent OnSelected => onSelected;
    }

    [SerializeField] private ScenarioOption[] options;
    private readonly List<(Button button, UnityAction action)> listeners = new();

    private void OnEnable()
    {
        SetListeners(true);
    }

    private void OnDisable()
    {
        SetListeners(false);
    }

    private void SetListeners(bool add)
    {
        if (!add)
        {
            foreach ((Button button, UnityAction action) in listeners)
                if (button != null)
                    button.onClick.RemoveListener(action);
            listeners.Clear();
            return;
        }

        if (options == null)
            return;

        for (int optionIndex = 0; optionIndex < options.Length; optionIndex++)
        {
            ScenarioOption option = options[optionIndex];
            if (option?.SelectionButtons == null)
                continue;

            int capturedIndex = optionIndex;
            foreach (Button button in option.SelectionButtons)
            {
                if (button == null)
                    continue;

                UnityAction listener = () => SelectScenario(capturedIndex);
                button.onClick.AddListener(listener);
                listeners.Add((button, listener));
            }
        }
    }

    private void SelectScenario(int index)
    {
        if (options == null || index < 0 || index >= options.Length || options[index] == null)
            return;

        Debug.Log($"Selected {options[index].Title}", this);
        options[index].OnSelected?.Invoke();
    }
}
