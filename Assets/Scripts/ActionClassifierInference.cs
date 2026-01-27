using UnityEngine;
using Unity.Barracuda;

public class ActionClassifierInference : MonoBehaviour {
    [Header("Model")]
    public NNModel actionModelAsset;

    private Model model;
    private IWorker worker;

    [Header("References")]
    public PlayerMLBuffer playerBuffer;

    // Probabilities (softmax outputs)
    public float IdleProbability { get; private set; }
    public float AttackProbability { get; private set; }
    public float DashProbability { get; private set; }
    public float MoveProbability { get; private set; }

    void Awake() {
        model = ModelLoader.Load(actionModelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, model);

        if (playerBuffer == null)
            playerBuffer = FindObjectOfType<PlayerMLBuffer>();
    }

    void OnDestroy() {
        worker?.Dispose();
    }

    void Update() {
        RunInference();
    }

    void RunInference() {
        if (playerBuffer == null || !playerBuffer.IsReady())
            return;

        float[] input = playerBuffer.GetLatestFrame();

        // Shape: (1, 5)
        Tensor inputTensor = new Tensor(1, input.Length);
        for (int i = 0; i < input.Length; i++)
            inputTensor[0, i] = input[i];

        worker.Execute(inputTensor);

        Tensor output = worker.PeekOutput();

        // Softmax outputs
        IdleProbability = output[0];
        AttackProbability = output[1];
        DashProbability = output[2];
        MoveProbability = output[3];

        inputTensor.Dispose();
        output.Dispose();
    }

    public int PredictAction() {
        float max = IdleProbability;
        int index = 0;

        if (AttackProbability > max) { max = AttackProbability; index = 1; }
        if (DashProbability > max) { max = DashProbability; index = 2; }
        if (MoveProbability > max) { index = 3; }

        return index;
    }
}
