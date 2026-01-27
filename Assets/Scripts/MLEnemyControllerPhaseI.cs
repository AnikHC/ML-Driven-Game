using System.Collections;
using UnityEngine;

public class MLEnemyControllerPhaseI : MonoBehaviour {

    [Header("References")]
    public PlayerMLBuffer playerBuffer;   // Rolling 20-frame buffer
    public MLInference ml;                // LSTM inference
    private Transform player;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 360f;

    [Header("ML Influence")]
    [Range(0f, 1f)]
    public float predictionWeight = 0.7f; // ML bias strength

    [Header("Direction Control")]
    public float directionUpdateInterval = 0.25f; // How often ML is queried
    public float directionSmoothSpeed = 8f;       // Smoothing speed

    [Header("Direction Commitment")]
    public float directionChangeThreshold = 25f;  // Degrees needed to re-commit

    [Header("Engagement Distances")]
    public float interceptRange = 4f;   // start cutting
    public float attackRange = 1.8f;     // stop thinking, commit

    [Header("Final Polish")]
    public float minTurnSpeedFactor = 0.3f;

    [Header("Dash Attack")]
    public float dashRange = 1.8f;
    public float dashSpeed = 8f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 2f;
    public float dashWindup = 0.15f;

    [Header("Dash Damage")]
    public float dashDamage = 20f;
    public float dashHitRadius = 0.6f;

    private bool attackCommitted = false;
    private Vector2 committedAttackDir;
    private Vector2 desiredDirection;     // Where enemy wants to go
    private Vector2 smoothedDirection;    // Where enemy actually moves
    private float directionTimer;
    private bool isDashing = false;
    private bool canDash = true;
    private bool dashHasHit = false;

    void Start() {
        player = playerBuffer.player.transform;

        // Initial commitment: go straight to player
        desiredDirection = (player.position - transform.position).normalized;
        smoothedDirection = desiredDirection;
    }

    void Update() {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (!isDashing && canDash && distanceToPlayer <= dashRange) {
            StartCoroutine(DashAttack());
            return;
        }

        if (!playerBuffer.IsReady())
            return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // CLOSE RANGE → COMMIT (NO ML)
        if (distToPlayer <= attackRange) {
            if (!attackCommitted) {
                committedAttackDir = (player.position - transform.position).normalized;
                attackCommitted = true;
            }

            MoveInDirection(committedAttackDir);
            return;
        }

        // MID RANGE → INTERCEPT MODE (ML LOCKED)
        if (distToPlayer <= interceptRange) {
            attackCommitted = false;

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

            MoveInDirection(smoothedDirection);
            return;
        }

        // FAR RANGE → PURE CHASE (NO ML)
        attackCommitted = false;
        Vector2 chaseDir = (player.position - transform.position).normalized;
        MoveInDirection(chaseDir);
    }


    /* ===================== AI LOGIC ===================== */

    // Decide intent (ML biases direction, never absolute position)
    Vector2 CalculateDesiredDirection() {
        Vector2 toPlayer = (player.position - transform.position).normalized;

        // If player is idle, ML is ignored
        if (playerBuffer.player.Velocity.magnitude < 0.1f)
            return toPlayer;

        // ML predicted future position
        Vector2 predicted = ml.PredictMovement(playerBuffer.GetSequence());
        Vector2 toPredicted = (predicted - (Vector2)transform.position).normalized;

        // Blend player direction with ML direction
        Vector2 blended = Vector2.Lerp(toPlayer, toPredicted, predictionWeight);
        return blended.normalized;
    }

    /* ===================== MOVEMENT ===================== */

    void MoveInDirection(Vector2 dir) {
        if (dir.sqrMagnitude < 0.01f)
            return;

        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0, 0, angle);

        float speedFactor = Mathf.Clamp01(dir.magnitude);
        float turnFactor = Mathf.Lerp(
            minTurnSpeedFactor,
            1f,
            speedFactor
        );

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * turnFactor * Time.deltaTime
        );

    }
    IEnumerator DashAttack() {
        isDashing = true;
        canDash = false;
        dashHasHit = false;

        // Wind-up (telegraph)
        yield return new WaitForSeconds(dashWindup);

        Vector2 dashDir = (player.position - transform.position).normalized;

        float elapsed = 0f;
        while (elapsed < dashDuration) {
            transform.position += (Vector3)(dashDir * dashSpeed * Time.deltaTime);

            TryDashHit();

            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
    void TryDashHit() {
        if (dashHasHit) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= dashHitRadius) {
            dashHasHit = true;

            // Damage player
            PlayerScript ps = player.GetComponent<PlayerScript>();
            if (ps != null) {
                ps.health -= dashDamage;
            }
        }
    }
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dashHitRadius);
    }


}
