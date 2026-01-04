using UnityEngine;

public class PlayerMLBuffer : MonoBehaviour {
    [Header("Reference")]
    public PlayerScript player;

    [Header("LSTM Settings")]
    public int sequenceLength = 20;   // MUST be 20
    public int featureDim = 5;        // MUST be 5

    private float[,] buffer;
    private int index = 0;
    private bool isFull = false;

    private void Awake() {
        buffer = new float[sequenceLength, featureDim];

        if (player == null)
            player = FindObjectOfType<PlayerScript>();
    }

    private void Update() {
        if (player == null) return;

        // EXACT order used in training
        float[] frame =
        {
            player.Position.x,   // px
            player.Position.y,   // py
            player.Velocity.x,   // pvx
            player.Velocity.y,   // pvy
            player.AimAngle      // aim
        };

        for (int i = 0; i < featureDim; i++)
            buffer[index, i] = frame[i];

        index = (index + 1) % sequenceLength;

        if (index == 0)
            isFull = true;
    }

    public bool IsReady() {
        return isFull;
    }

    // Returns [20,5] oldest → newest
    public float[,] GetSequence() {
        if (!isFull) return null;

        float[,] seq = new float[sequenceLength, featureDim];

        for (int t = 0; t < sequenceLength; t++) {
            int src = (index + t) % sequenceLength;
            for (int f = 0; f < featureDim; f++) {
                seq[t, f] = buffer[src, f];
            }
        }

        return seq;
    }
}
