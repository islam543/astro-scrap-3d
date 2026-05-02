using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public bool pauseOnGameOver = true;
    public bool unlockCursorOnGameOver = true;

    private bool isGameOver = false;

    public bool IsGameOver => isGameOver;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (!isGameOver) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            RestartScene();
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("[GameManager] Game Over.");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (unlockCursorOnGameOver)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (pauseOnGameOver)
            Time.timeScale = 0f;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
