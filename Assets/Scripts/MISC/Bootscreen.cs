using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootscreen : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(LoadMainMenu), 0.5f);
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

}
