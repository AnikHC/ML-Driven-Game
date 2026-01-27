using UnityEngine;
using System.Collections;

public class MLEnemyController : MonoBehaviour {
    /* ===================== REFERENCES ===================== */
    [Header("References")]
    public PlayerMLBuffer playerBuffer;
    public MLInference ml;
    public ActionClassifierInference actionClassifier;
    private Transform player;

    /* ===================== PHASE CONTROL ===================== */
    [Header("Boss Phase")]
    [Range(1, 2)]
    public int phase = 1;

    /* ===================== MOVEMENT ===================== */
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 360f;

    /* ===================== ML CONTROL ===================== */
    [Header("ML Influence")]
    [Range(0f, 1f)]
    public float predictionWeight = 0.65f;

    public float interceptRange = 4f;
    public float attackRange = 1.8f;

    /* ===================== SMOOTHING ===================== */
    [Header("Smoothing")]
    public float directionUpdateInterval = 0.25f;
    public float directionSmoothSpeed = 6f;
    public float minTurnSpeedFactor = 0.3f;

    /* ===================== DASH ATTACK ===================== */
    [Header("Dash Attack")]
    public float dashRange = 1.8f;
    public float dashSpeed = 8f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 2f;
    public float dashWindup = 0.15f;

    private bool isDashing = false;
    private bool canDash = true;

    /* ===================== PHASE-2 BULLET DODGE ===================== */
    [Header("Phase-2 Bullet Dodge")]
    public float bulletDodgeStrength = 3f;
    public float bulletDodgeDuration = 0.18f;
    public float bulletDodgeCooldown = 1.2f;

    private float bulletDodgeCooldownTimer = 0f;
    private float dodgeTimer = 0f;
    private Vector2 dodgeDir;

    /* ===================== INTERNAL STATE ===================== */
    private Vector2 desiredDirection;
    private Vector2 smoothedDirection;
    private float directionTimer = 0f;

    /* ===================== UNITY ===================== */

    void Start() {
        player = GameObject.FindWithTag("Player").transform;
        playerBuffer = GameObject.Find("MLBrain").GetComponent<PlayerMLBuffer>();
        ml = GameObject.Find("MLBrain").GetComponent<MLInference>();
        actionClassifier = GetComponent<ActionClassifierInference>();

        desiredDirection = (player.position - transform.position).normalized;
        smoothedDirection = desiredDirection;
    }

    void Update() {
        if (player == null || playerBuffer == null || ml == null)
            return;

        if (bulletDodgeCooldownTimer > 0f)
            bulletDodgeCooldownTimer -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.position);

        /* ========== DASH ATTACK (BOTH PHASES) ========== */
        if (!isDashing && canDash && dist <= dashRange) {
            StartCoroutine(DashAttack());
            return;
        }

        /* ========== PHASE-2 BULLET DODGE ========== */
        if (phase == 2 && dodgeTimer <= 0f && bulletDodgeCooldownTimer <= 0f) {
            if (playerBuffer.player.IsAttacking) {
                Vector2 playerVel = playerBuffer.player.Velocity;

                dodgeDir =
                    playerVel.magnitude > 0.1f
                    ? Vector2.Perpendicular(playerVel).normalized
                    : Vector2.Perpendicular(
                        (player.position - transform.position).normalized
                      ).normalized;

                dodgeTimer = bulletDodgeDuration;
                bulletDodgeCooldownTimer = bulletDodgeCooldown;
            }
        }

        if (dodgeTimer > 0f) {
            dodgeTimer -= Time.deltaTime;
            Move(dodgeDir * bulletDodgeStrength);
            return;
        }

        /* ========== WAIT FOR ML BUFFER ========== */
        if (!playerBuffer.IsReady()) {
            Move((player.position - transform.position).normalized);
            return;
        }

        /* ========== CLOSE RANGE (COMMIT) ========== */
        if (dist <= attackRange) {
            Vector2 commitDir = (player.position - transform.position).normalized;
            Move(commitDir);
            return;
        }

        /* ========== INTERCEPT RANGE (ML) ========== */
        if (dist <= interceptRange) {
            directionTimer += Time.deltaTime;
            if (directionTimer >= directionUpdateInterval) {
                directionTimer = 0f;
                desiredDirection = CalculateDesiredDirection();
            }

            smoothedDirection = Vector2.Lerp(
                smoothedDirection,
                desiredDirection,
                directionSmoothSpeed * Time.deltaTime
            );

            Move(smoothedDirection);
            return;
        }

        /* ========== FAR RANGE (CHASE) ========== */
        Move((player.position - transform.position).normalized);
    }

    /* ===================== ML DIRECTION ===================== */

    Vector2 CalculateDesiredDirection() {
        Vector2 toPlayer = (player.position - transform.position).normalized;

        if (playerBuffer.player.Velocity.magnitude < 0.1f)
            return toPlayer;

        Vector2 predicted = ml.PredictMovement(playerBuffer.GetSequence());
        Vector2 toPredicted = (predicted - (Vector2)transform.position).normalized;

        return Vector2.Lerp(toPlayer, toPredicted, predictionWeight).normalized;
    }

    /* ===================== MOVEMENT ===================== */

    void Move(Vector2 dir) {
        if (dir.sqrMagnitude < 0.001f)
            return;

        transform.position += (Vector3)(dir.normalized * moveSpeed * Time.deltaTime);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0, 0, angle);

        float turnFactor = Mathf.Lerp(minTurnSpeedFactor, 1f, dir.magnitude);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * turnFactor * Time.deltaTime
        );
    }

    /* ===================== DASH ATTACK ===================== */

    IEnumerator DashAttack() {
        isDashing = true;
        canDash = false;

        yield return new WaitForSeconds(dashWindup);

        Vector2 dashDir = (player.position - transform.position).normalized;
        float elapsed = 0f;

        while (elapsed < dashDuration) {
            transform.position += (Vector3)(dashDir * dashSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
