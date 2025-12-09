using System.IO;
using System.Text;
using UnityEngine;

public class DataLogger : MonoBehaviour {
    [Header("References")]
    [SerializeField] private PlayerScript player;   // Drag player here (or auto-detected)

    [Header("Logging Settings")]
    [SerializeField] private float logFps = 30f;    // Sampling rate

    private StreamWriter writer;
    private float logInterval;
    private float timer = 0f;
    private int frame = 0;

    private void Awake() {
        if (player == null)
            player = FindObjectOfType<PlayerScript>();

        logInterval = 1f / logFps;
    }

    private void Start() {
        string fileName = "session_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
        string path = Path.Combine(Application.persistentDataPath, fileName);

        writer = new StreamWriter(path, false, Encoding.UTF8);

        // CSV header — ML ONLY needs this
        writer.WriteLine("timestamp,frame,px,py,pvx,pvy,a_attack,a_dash,a_idle,aim");

        Debug.Log("ML Data Logging to: " + path);
    }

    private void Update() {
        if (writer == null || player == null) return;

        timer += Time.deltaTime;

        while (timer >= logInterval) {
            timer -= logInterval;
            LogFrame();
        }
    }

    private void LogFrame() {
        float ts = Time.time;

        Vector2 pos = player.Position;
        Vector2 vel = player.Velocity;

        int a_attack = player.IsAttacking ? 1 : 0;
        int a_dash = player.IsDashing ? 1 : 0;
        int a_idle = player.IsIdle ? 1 : 0;

        float aim = player.AimAngle;

        writer.WriteLine(
            $"{ts:F3},{frame++}," +
            $"{pos.x:F4},{pos.y:F4}," +
            $"{vel.x:F4},{vel.y:F4}," +
            $"{a_attack},{a_dash},{a_idle}," +
            $"{aim:F4}"
        );
    }

    private void OnApplicationQuit() {
        if (writer != null) {
            writer.Flush();
            writer.Close();
            writer = null;
        }
    }

    private void OnDestroy() {
        if (writer != null) {
            writer.Flush();
            writer.Close();
            writer = null;
        }
    }
}
