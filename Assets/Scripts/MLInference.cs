using UnityEngine;
using Unity.Barracuda;

public class MLInference : MonoBehaviour {
    [Header("Fixed LSTM ONNX")]
    public NNModel movementModelAsset;

    private Model model;
    private IWorker worker;
    private bool ready = false;

    private const int SEQ_LEN = 20;
    private const int FEAT_DIM = 5;

    void Awake() {
        if (movementModelAsset == null) {
            Debug.LogError("MLInference: Movement model not assigned!");
            return;
        }

        model = ModelLoader.Load(movementModelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, model);
        ready = true;

        Debug.Log("MLInference: Fixed LSTM model loaded.");
    }

    /// <summary>
    /// Input : float[20,5] → [px, py, pvx, pvy, aim]
    /// Tensor: (1, 20, 5, 1)
    /// Output: Vector2 (future px, future py)
    /// </summary>
    public Vector2 PredictMovement(float[,] seq) {
        if (!ready || seq == null)
            return Vector2.zero;

        // SAFETY CHECK
        if (seq.GetLength(0) != SEQ_LEN || seq.GetLength(1) != FEAT_DIM) {
            Debug.LogError("MLInference: Input sequence shape mismatch.");
            return Vector2.zero;
        }

        using (Tensor input = new Tensor(1, SEQ_LEN, FEAT_DIM, 1)) {
            for (int t = 0; t < SEQ_LEN; t++) {
                for (int f = 0; f < FEAT_DIM; f++) {
                    input[0, t, f, 0] = seq[t, f];
                }
            }

            worker.Execute(input);

            using (Tensor output = worker.PeekOutput()) {
                // Output: [1,2]
                float x = output[0];
                float y = output[1];
                return new Vector2(x, y);
            }
        }
    }

    void OnDestroy() {
        worker?.Dispose();
    }
}
