using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainScene"); // <-- cambia el nombre si el tuyo es distinto
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
