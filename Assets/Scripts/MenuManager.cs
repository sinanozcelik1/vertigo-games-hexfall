using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        // Sahne adına göre geçişler
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        // Uygulamayı tamamen sonlandırma
        Application.Quit();
    }
}