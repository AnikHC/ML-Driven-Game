using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{   
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] ParticleSystem collisionParticle;
    private Rigidbody2D rb;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }
    /*void FixedUpdate()
    {
        transform.Translate(new Vector2(0,bulletSpeed));
    }*/
    private void Start() {
        rb.AddRelativeForce(Vector2.up * bulletSpeed*Time.deltaTime, ForceMode2D.Force);
    }
    private void OnCollisionEnter2D(Collision2D collision) {
        Instantiate(collisionParticle,transform.position,collisionParticle.transform.rotation);
        Destroy(gameObject);
    }
}
