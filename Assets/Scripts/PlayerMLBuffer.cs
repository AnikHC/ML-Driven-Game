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

        float[] frame =
        {
            player.Position.x,
            player.Position.y,
            player.Velocity.x,
            player.Velocity.y,
            player.AimAngle
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

    // LSTM input: [20,5]
    public float[,] GetSequence() {
        if (!isFull) return null;

        float[,] seq = new float[sequenceLength, featureDim];

        for (int t = 0; t < sequenceLength; t++) {
            int src = (index + t) % sequenceLength;
            for (int f = 0; f < featureDim; f++)
                seq[t, f] = buffer[src, f];
        }

        return seq;
    }

    // Action classifier input: [5]
    public float[] GetLatestFrame() {
        if (!isFull && index == 0) return null;

        int latestIndex = index - 1;
        if (latestIndex < 0)
            latestIndex = sequenceLength - 1;

        float[] frame = new float[featureDim];

        for (int i = 0; i < featureDim; i++)
            frame[i] = buffer[latestIndex, i];

        return frame;
    }
}
