using UnityEngine;
using Unity.MLAgents;
using Unity.Barracuda;
using Unity.MLAgents.Policies;

public class RandomOpponentModel : MonoBehaviour
{
    public enum AgentSide
    {
        Runner,
        Chaser
    }

    [Header("Enable Random Opponent")]
    public bool enableRandomOpponent = true;


    [Header("This Agent Side")]
    public AgentSide agentSide;

    [Header("Model Pool")]
    public NNModel[] modelPool;

    [Header("Behavior Name")]
    public string behaviorName;

    [Header("Inference Device")]
    public InferenceDevice inferenceDevice = InferenceDevice.Default;

    [Header("Debug")]
    public bool showDebugLog = true;

    private Agent agent;
    private BehaviorParameters behaviorParameters;

    private void Awake()
    {
        agent = GetComponent<Agent>();
        behaviorParameters = GetComponent<BehaviorParameters>();
    }

    public void ApplyModeAndRandomModel()
    {
        if (!enableRandomOpponent)
        {
            return;
        }
        if (TrainingModeManager.Instance == null)
        {
            Debug.LogError("No TrainingModeManager found in scene.");
            return;
        }

        bool trainRunner = TrainingModeManager.Instance.IsTrainingRunner();
        bool trainChaser = TrainingModeManager.Instance.IsTrainingChaser();

        bool thisAgentShouldTrain =
            (agentSide == AgentSide.Runner && trainRunner) ||
            (agentSide == AgentSide.Chaser && trainChaser);

        if (thisAgentShouldTrain)
        {
            SetAsTrainingAgent();
        }
        else
        {
            SetAsFrozenRandomOpponent();
        }
    }

    private void SetAsTrainingAgent()
    {
        if (behaviorParameters != null)
        {
            behaviorParameters.BehaviorType = BehaviorType.Default;
        }

        if (showDebugLog)
        {
            Debug.Log($"{gameObject.name} is TRAINING with behavior name: {behaviorName}");
        }
    }

    private void SetAsFrozenRandomOpponent()
    {
        if (modelPool == null || modelPool.Length == 0)
        {
            Debug.LogError($"{gameObject.name}: modelPool is empty. Frozen opponent needs at least one model.");
            return;
        }

        int index = Random.Range(0, modelPool.Length);
        NNModel selectedModel = modelPool[index];

        agent.SetModel(behaviorName, selectedModel, inferenceDevice);

        if (behaviorParameters != null)
        {
            behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;
        }

        if (showDebugLog)
        {
            Debug.Log($"{gameObject.name} is FROZEN opponent. Selected model: {selectedModel.name}");
        }
    }
}