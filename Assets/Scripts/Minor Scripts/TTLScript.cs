using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TTLScript : MonoBehaviour {
    [SerializeField] float TTL = 2f;
    private void Update() {
        TTL -= Time.deltaTime;
        if(TTL<=0f) Destroy(gameObject);
    }
}
