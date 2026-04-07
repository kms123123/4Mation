using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OutgameManager : MonoBehaviour
{
    [SerializeField] private Button _gameSceneButton;
    [SerializeField] private Button _gameScene2Button;

    private void Awake() {
        _gameSceneButton.onClick.AddListener(OnGameSceneButtonClicked);
        _gameScene2Button.onClick.AddListener(OnGameScene2ButtonClicked);
    }

    private void OnGameSceneButtonClicked() {
        SceneManager.LoadScene("GameScene");
    }

    private void OnGameScene2ButtonClicked() {
        SceneManager.LoadScene("GameScene2");
    }
}
