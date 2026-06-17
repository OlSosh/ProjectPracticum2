using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class WinCanvas : MonoBehaviour
{

    public TextMeshProUGUI donenessText;
    public ScoreManager scoreManager;

    public string gameSceneName = "MenuScene";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void LoadMenu()
	{
        
		if (Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
	}

    // Update is called once per frame
    void Update()
    {
        int doneness = scoreManager.GetDoneness();

        if (doneness == 0)
		{
			donenessText.text = "Сырой";
		} else if (doneness == 1)
		{
			donenessText.text = "Слабо прожареный";
		} else if (doneness == 2)
		{
			donenessText.text = "Сырой";
		} else if (doneness == 3)
		{
			donenessText.text = "Подгорел";
		} else if (doneness == 4)
		{
			donenessText.text = "Сгорел";
		}
    }
}
