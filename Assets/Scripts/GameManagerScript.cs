using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManagerScript : MonoBehaviour
{
    [SerializeField] private Text playerHealth;
    [SerializeField] private Text playerStamina;
    private PlayerScript playerScript;
    private void Awake() {
        Application.targetFrameRate = 30;
        QualitySettings.vSyncCount = 0;
        playerScript = GameObject.FindWithTag("Player").GetComponent<PlayerScript>();
    }
    private void Update() {
        DevBuildButtons();
        GetPlayerVitals();
    }
    private void GetPlayerVitals() {
        playerHealth.text = (playerScript.health).ToString();
        playerStamina.text = playerScript.stamina.ToString();
    }
    private void DevBuildButtons() {
        if (Input.GetKeyDown(KeyCode.R)) {
            SceneManager.LoadScene(0);
        }
        if (Input.GetKeyDown(KeyCode.Q)) { 
            Application.Quit();
        }
    }
}
