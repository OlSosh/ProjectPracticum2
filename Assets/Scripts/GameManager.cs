using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using SimpleFileBrowser;

public class GameManager : MonoBehaviour
{

    public Slider progressbar;
    public GameObject PromptCanvas;
    public GameObject winCanvas;

    public AudioSource audioSource;

    public ScoreManager scoreManager;


    [HideInInspector]
    public bool playing = false;

    float GetSongRatio()
	{
		return audioSource.time / audioSource.clip.length;
	}
    float SongRatioToSeconds(float ratio)
	{
		return ratio * audioSource.clip.length;
	}
    float SongSecondsToRatio(float s)
	{
		return s / audioSource.clip.length;
	}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void OpenFilePrompt()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Audio Files", ".mp3", ".wav", ".ogg"));
        FileBrowser.SetDefaultFilter(".mp3");
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    private IEnumerator ShowLoadDialogCoroutine()
    {
        // Open the native file selection dialog
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Select Audio", "Load");

        if (FileBrowser.Success)
        {
            // Get the absolute local path of the selected file
            string filePath = FileBrowser.Result[0];
            StartCoroutine(LoadAudioClip(filePath));
        }
    }

    private IEnumerator LoadAudioClip(string path)
    {
        // Format path as a local URL for UnityWebRequest
        string url = "file://" + path;
        
        // Determine audio type automatically from extension
        AudioType type = GetAudioType(path);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, type))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"File loading failed: {www.error}");
            }
            else
            {
                // Extract the clip, assign it, and play
                AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = myClip;
                StartPlaying();
            }
        }
    }

    private AudioType GetAudioType(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".wav" => AudioType.WAV,
            ".ogg" => AudioType.OGGVORBIS,
            ".mp3" => AudioType.MPEG,
            _ => AudioType.UNKNOWN,
        };
    }


    void StartPlaying()
	{
        PromptCanvas.SetActive(false);
		playing = true;
        audioSource.Play();
	}

    public void StopPlaying()
	{
		playing = false;
        audioSource.Stop();
        winCanvas.SetActive(true);
	}

    // Update is called once per frame
    void Update()
    {
        progressbar.value = GetSongRatio();
    }
}
