using UnityEngine;

public class TrainingModeManager : MonoBehaviour
{
    public enum TrainingMode
    {
        TrainRunner,
        TrainChaser
    }

    [Header("Current Training Mode")]
    public TrainingMode trainingMode = TrainingMode.TrainRunner;

    public static TrainingModeManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public bool IsTrainingRunner()
    {
        return trainingMode == TrainingMode.TrainRunner;
    }

    public bool IsTrainingChaser()
    {
        return trainingMode == TrainingMode.TrainChaser;
    }
}
