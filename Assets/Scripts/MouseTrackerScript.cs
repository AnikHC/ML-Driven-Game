using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseTrackerScript : MonoBehaviour
{   
    private Vector3 mousePosition;
    [SerializeField] Transform player;
    public float xThreshold = 1f;
    public float yThreshold = 1f;
    Vector2 targetPos;
    private void Update() {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        targetPos = (player.position + mousePosition)/2;

        targetPos.x = Mathf.Clamp(targetPos.x, -xThreshold + player.position.x, xThreshold + player.position.x );
        targetPos.y = Mathf.Clamp(targetPos.y, -yThreshold + player.position.y, yThreshold + player.position.y );

        gameObject.transform.position = targetPos;
    }
}
