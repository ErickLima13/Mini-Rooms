using UnityEngine;
using UnityEngine.SceneManagement;

public enum ScenesInGame
{
    MainMenu = 0,
    Gameplay = 1,
    Level1,
    Level2,
    Level3,
    Level4

}

public class ChangeScene : IChangeScene
{
    [SerializeField] private ScenesInGame _currentScene;


    public void LoadScene(ScenesInGame scenesInGame)
    {
        //SceneManager.LoadScene((int)scenesInGame);

        //Scene currentScene = SceneManager.GetActiveScene();
        //_currentScene = (ScenesInGame)currentScene.buildIndex;

        Debug.Log("chamei " + scenesInGame);
    }
}
