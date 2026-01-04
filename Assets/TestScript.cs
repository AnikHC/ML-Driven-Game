using UnityEngine;

public class MLQuickTest : MonoBehaviour {
    public MLInference ml;
    public PlayerMLBuffer buffer;

    void Update() {
        if (buffer.IsReady()) {
            Vector2 p = ml.PredictMovement(buffer.GetSequence());
            Debug.Log("Predicted future position: " + p);
            enabled = false;
        }
    }
}
