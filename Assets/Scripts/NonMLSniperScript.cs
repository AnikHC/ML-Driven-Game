using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NonMLSniperScript : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject bulletSpawnPoint;
    [SerializeField] private GameObject gun;

    [Header("Enemy Settings")]
    [SerializeField] private float shootingDelay = 1f;
    [SerializeField] private int health = 1;
    private float shootingDelayDynamic;
    private GameObject player;
    
    private void Awake() {
        player = GameObject.FindWithTag("Player");
    }

    private void Start() {
        shootingDelayDynamic=shootingDelay;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, (player.transform.position - transform.position).normalized);
    }

    private void Update() {
        if (player != null) {
            lookAtPlayer();
            ShootAtPlayer();
        }
    }

    private void lookAtPlayer() { 
        Vector2 playerDirection = (player.transform.position - transform.position);
        float lookAngle = Mathf.Atan2(playerDirection.x,playerDirection.y)*Mathf.Rad2Deg;
        gun.transform.rotation = Quaternion.Euler(0, 0, -lookAngle-90);
    }

    private void ShootAtPlayer() { 
        shootingDelayDynamic -= Time.deltaTime;
        if (shootingDelayDynamic <= 0f) {
            Instantiate(bulletPrefab,bulletSpawnPoint.transform.position,bulletSpawnPoint.transform.rotation);
            shootingDelayDynamic = shootingDelay;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Bullet")) {
            health--;
            if (health <= 0) { 
                Destroy(gameObject);
            }
        }
    }
}
