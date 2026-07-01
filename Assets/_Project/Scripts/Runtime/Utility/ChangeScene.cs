using UnityEngine;
using UnityEngine.SceneManagement;

public enum ScenesInGame
{
    MainMenu = 0,
    Gameplay = 1
}

public class ChangeScene : MonoBehaviour
{
    [SerializeField] private ScenesInGame _currentScene;


    public void LoadScene(ScenesInGame scenesInGame)
    {
        SceneManager.LoadScene((int)scenesInGame);

        Scene currentScene = SceneManager.GetActiveScene();
        _currentScene = (ScenesInGame)currentScene.buildIndex;
    }
}
