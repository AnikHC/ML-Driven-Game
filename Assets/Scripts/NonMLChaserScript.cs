using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonMLChaserScript : MonoBehaviour {
    [Header("Enemy settings")]
    [SerializeField] private float enemySpeed=2.5f;
    [SerializeField] private int enemyHealth = 2;
    [SerializeField] private float enemyAttackRange = 1f;
    [SerializeField] private float delayBeforeAttack = 0.15f;
    [SerializeField] private float attackDistance = 2f;
    [SerializeField] private float attackDuration=0.1f;
    [SerializeField] private float enemyTurnSpeed;
    private bool isAttacking=false;
    private GameObject player;
    private Rigidbody2D rb;

    private void Awake() {
        player = GameObject.FindWithTag("Player");
        rb = GetComponent<Rigidbody2D>();
    }
    private void Start() {
    Time.fixedDeltaTime = 1 / 30f;
    }
    private void Update() {
        PhaseManager();
    }
    private void Movement() {
        if (player != null) {
            Vector2 playerDirection = (player.transform.position - transform.position).normalized;
            rb.velocity = playerDirection * enemySpeed*Time.deltaTime;
            EnemyTurn(playerDirection);
        }

    }
    private void EnemyTurn(Vector2 movDir) {
            Quaternion toRotate = Quaternion.LookRotation(Vector3.forward, movDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotate, enemyTurnSpeed * Time.deltaTime);
    }
    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Bullet")) {
            enemyHealth--;
            if (enemyHealth <= 0) Destroy(gameObject);
        }
    }
    private void PhaseManager() {
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > enemyAttackRange) Movement();
        else if (!isAttacking) { 
            ChargeAttack(); 
            rb.velocity = Vector2.zero;
        }
    }
    private void ChargeAttack() { 
        Vector2 attackDirection = (player.transform.position - transform.position).normalized;
        StartCoroutine(DoAttack(attackDirection));
    }
    private IEnumerator DoAttack(Vector2 attackDirection) {
        //animation for charging towards player
        isAttacking = true;
        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(attackDirection * attackDistance);
        yield return new WaitForSeconds(delayBeforeAttack);

        float elapsed = 0f;
        while (elapsed < attackDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / attackDuration;
            transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.position = end;
        yield return new WaitForSeconds(2f);
        isAttacking = false;
    }
}
