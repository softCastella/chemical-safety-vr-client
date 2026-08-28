using System;
using UnityEngine;

public enum PPEQuizTopic
{
    ProtectiveSuit,
    ChemicalBoots,
    TacticalHarness,
    SuppliedAirMask,
    SafetyHelmet,
    ChemicalGloves,
    SafetyGoggles,
    FaceShield,
}

[Serializable]
public sealed class PPEQuizQuestionPool
{
    [SerializeField] ScenarioDetailModal.PpeWorkPlan workPlan;
    [SerializeField] ScenarioDetailModal.PpeLearningMode learningMode;
    [SerializeField] PPEQuizQuestion[] questions;

    public ScenarioDetailModal.PpeWorkPlan WorkPlan => workPlan;
    public ScenarioDetailModal.PpeLearningMode LearningMode => learningMode;
    public PPEQuizQuestion[] Questions => questions;
}

[CreateAssetMenu(
    fileName = "PPEQuizQuestionCatalog",
    menuName = "Chemical Safety VR/PPE Quiz Question Catalog")]
public sealed class PPEQuizQuestionCatalog : ScriptableObject
{
    [SerializeField] PPEQuizQuestionPool[] pools;

    public PPEQuizQuestionPool[] Pools => pools;

    public bool TryGetQuestions(
        ScenarioDetailModal.PpeWorkPlan workPlan,
        ScenarioDetailModal.PpeLearningMode learningMode,
        out PPEQuizQuestion[] questions)
    {
        if (pools != null)
        {
            foreach (PPEQuizQuestionPool pool in pools)
            {
                if (pool != null &&
                    pool.WorkPlan == workPlan &&
                    pool.LearningMode == learningMode)
                {
                    questions = pool.Questions;
                    return questions != null;
                }
            }
        }

        questions = null;
        return false;
    }
}
