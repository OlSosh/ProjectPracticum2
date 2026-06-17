using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Название игровой сцены")]
    [SerializeField] private string gameSceneName = "GameScene";
    
    [Header("Кнопки (назначить в инспекторе)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    void Start()
    {
        Debug.Log("MainMenu: Start");
        
        // Привязываем кнопки через код (если они назначены)
        if (playButton != null)
        {
            playButton.onClick.AddListener(PlayGame);
            Debug.Log("MainMenu: Кнопка Play привязана");
        }
        else
        {
            Debug.LogError("MainMenu: Кнопка Play не назначена в инспекторе!");
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
            Debug.Log("MainMenu: Кнопка Quit привязана");
        }
        else
        {
            Debug.LogError("MainMenu: Кнопка Quit не назначена в инспекторе!");
        }
        
        // Проверяем, есть ли сцена в билде
        bool sceneInBuild = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (scenePath.Contains(gameSceneName))
            {
                sceneInBuild = true;
                break;
            }
        }
        
        if (!sceneInBuild)
        {
            Debug.LogError($"MainMenu: Сцена '{gameSceneName}' не найдена в Build Settings! Добавьте её в File → Build Settings");
        }
    }

    public void PlayGame()
    {
        Debug.Log($"MainMenu: Загружаю сцену '{gameSceneName}'");
        
        // Проверяем, существует ли сцена
        if (Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError($"MainMenu: Не могу загрузить сцену '{gameSceneName}'. Проверьте:");
            Debug.LogError("1. Сцена добавлена в File → Build Settings?");
            Debug.LogError("2. Имя сцены написано правильно?");
            
            // Показываем все сцены в билде для отладки
            Debug.Log("Сцены в Build Settings:");
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                Debug.Log($"  [{i}] {SceneUtility.GetScenePathByBuildIndex(i)}");
            }
        }
    }

    public void QuitGame()
    {
        Debug.Log("MainMenu: Выход из игры");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(PlayGame);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }
}