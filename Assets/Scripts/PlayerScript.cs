using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour {
    public Vector2 Position => transform.position;
    public Vector2 Velocity => rb != null ? rb.velocity : Vector2.zero; // requires rb field
    public float AimAngle { get; private set; } // we'll set this in LookDirection
    public bool IsAttacking { get; private set; }
    public bool IsIdle => rb != null ? rb.velocity.magnitude < 0.01f : true;

    [Header("Bullet Settings")]
    [SerializeField] GameObject bullet;
    [SerializeField] GameObject bulletSpawn;
    [SerializeField] private float bulletShootDelay = 0.5f;
    [SerializeField] private float bulletReloadDelay = 1.5f;
    [SerializeField] private int bulletCount = 2;
    private float remainingBulletDelayTime = 0f;
    private GameObject gun;

    [Header("Movement Settings")]
    [SerializeField] private float movSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.2f;
    [SerializeField] private float rotateSpeed = 1f;
    private Rigidbody2D rb;

    [Header("Dash Settings")]
    [SerializeField] float dashDistance = 3f;
    [SerializeField] float dashDuration = 0.12f;
    [SerializeField] float dashCooldown = 1f;
    private float lastDashTime = -10f;
    public bool IsDashing { get; private set; }

    [Header("SFX Settings")]
    [SerializeField]private AudioClip shootingSound = null;
    [SerializeField]private AudioClip reloadingSound = null;
    [SerializeField]private AudioClip walkingSound = null;
    private AudioSource audioSource;

    [Header("Player Vitals Settings")]
    public float health = 100f;
    public float stamina = 100f;
    [SerializeField] private float staminaRegenDelay=2f;
    [SerializeField] private float staminaRegenSpeed = 1f;
    [SerializeField] private float staminaLostWhileSprint = 1f;
    [SerializeField] private float invulnerabilityFrame = 2f;
    private float invulnerabilityFrameDynamic = 0f;
    private float staminaRegenDelayDynamic;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        Time.fixedDeltaTime = 1/30f;
        gun = transform.Find("Gun").gameObject;
    }
    private void Start() {
        staminaRegenDelayDynamic = staminaRegenDelay;
        invulnerabilityFrameDynamic = invulnerabilityFrame;
    }
    private void Update() {
        Shoot();
        LookDirection();
        StaminaRegen();
        HealthCheck();
    }
    private void FixedUpdate() {
        Movement();
        DashMovement();
    }
    private void LookDirection() { 
        Vector2 lookDirection = Input.mousePosition - Camera.main.WorldToScreenPoint(gameObject.transform.position );//gets the direction for the player
        float lookAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;//uses the vector to get angle and changes to Degree
        gun.transform.rotation = Quaternion.Euler(0,0,lookAngle-90);//rotates the player to the desired direction
        AimAngle = Mathf.Atan2(lookDirection.y, lookDirection.x);//for ML
    }
    private void Shoot() {
        remainingBulletDelayTime = remainingBulletDelayTime - Time.deltaTime;
        if (Input.GetMouseButtonDown(0)&&remainingBulletDelayTime<=0f) {
            Instantiate(bullet, bulletSpawn.transform.position, bulletSpawn.transform.rotation);
            IsAttacking = true; // this will be true only this frame //for ML
            bulletCount--;
            if (bulletCount > 0) remainingBulletDelayTime = bulletShootDelay;
            else {
                remainingBulletDelayTime = bulletReloadDelay;
                bulletCount = 2;
            }
        }
        else { //for ML
            IsAttacking = false; // for ML
        }
    }

    private void Movement() {
        float movDirX = Input.GetAxisRaw("Horizontal");
        float movDirY = Input.GetAxisRaw("Vertical");
        Vector2 moveDir = new Vector2(movDirX,movDirY);
        float actualMovSpeed = movSpeed;

        if (Input.GetKey(KeyCode.LeftShift)) {
            actualMovSpeed = movSpeed * sprintMultiplier;
            stamina -= staminaLostWhileSprint * Time.deltaTime;
            staminaRegenDelayDynamic = staminaRegenDelay;
        }
        rb.velocity = moveDir.normalized * actualMovSpeed;
        playerTurn(moveDir);
        Debug.Log(moveDir);
    }
    private void playerTurn(Vector2 movDir) {
        if (movDir != Vector2.zero) {
            Quaternion toRotate = Quaternion.LookRotation(Vector3.forward, movDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotate, rotateSpeed*Time.deltaTime);
        }
    }
    private void DashMovement() {

        if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastDashTime >= dashCooldown && !IsDashing && stamina>=20f) {
            // 1. Check movement direction
            Vector2 inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

            Vector2 dashDir;

            if (inputDir.sqrMagnitude > 0.01f) {
                // Use WASD direction
                dashDir = inputDir;
            }
            else {
                // Dash backwards from aim direction
                Vector2 aimScreen = (Vector2)Input.mousePosition - (Vector2)Camera.main.WorldToScreenPoint(transform.position);
                Vector2 aimDir = aimScreen.normalized;

                dashDir = -aimDir;  // backward dash
            }
             
            StartCoroutine(DoDash(dashDir));
            lastDashTime = Time.time;
        }
    }

    private IEnumerator DoDash(Vector2 direction) {
        direction = direction.normalized;
        IsDashing = true;
        stamina -= 20;
        staminaRegenDelayDynamic = staminaRegenDelay;

        Vector2 originalVelocity = rb.velocity;
        rb.velocity = Vector2.zero;

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * dashDistance);

        float elapsed = 0f;
        while (elapsed < dashDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;
            transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.position = end;
        rb.velocity = originalVelocity;

        IsDashing = false;
    }
    private void StaminaRegen() { 
        staminaRegenDelayDynamic -= Time.deltaTime;
        if (staminaRegenDelayDynamic <= 0f&&stamina<=100f) {
            stamina += 1f * staminaRegenSpeed * Time.deltaTime;
        }
    }
    private void HealthCheck() { 
        if(invulnerabilityFrameDynamic>0f)  invulnerabilityFrameDynamic -= Time.deltaTime;
        if(health<=0f) Destroy(gameObject);
    }
    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Enemy")&&invulnerabilityFrameDynamic <=0f) {
            health -= 20;
            invulnerabilityFrameDynamic = invulnerabilityFrame;
        }
        if (collision.gameObject.CompareTag("Bullet") && invulnerabilityFrameDynamic <= 0f) {
            health -= 15;
            invulnerabilityFrameDynamic = invulnerabilityFrame;
        }
    }

}

