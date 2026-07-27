using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.IO;

/// <summary>
/// Controls the main-menu actions and presents the available run save.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Serializable]
    private class SavePreview
    {
        public int currentRoundIndex;
        public int currentStageType;
        public float savedPlayerHealth;
        public float maxHealthBonus;
    }

    [SerializeField] private Button BtnNewGame;
    [SerializeField] private Button BtnContinue;
    [SerializeField] private TMP_Text SaveSlotText;
    [SerializeField] private Button BtnQuit;

    private string SavePath => Path.Combine(Application.persistentDataPath, "module1_run_save.json");

    private void Awake()
    {
        BtnNewGame.onClick.AddListener(StartNewGame);
        BtnContinue.onClick.AddListener(ContinueGame);
        BtnQuit.onClick.AddListener(QuitGame);
    }

    private void Start()
    {
        UpdateSavePreview();
    }

    private void StartNewGame()
    {
        RunManager.EnsureInstance().BeginNewRun();
        SceneManager.LoadScene("Game");
    }

    private void ContinueGame()
    {
        RunManager.EnsureInstance().ContinueRun();
        SceneManager.LoadScene("Game");
    }

    private void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void UpdateSavePreview()
    {
        if (!File.Exists(SavePath))
        {
            BtnContinue.interactable = false;
            SaveSlotText.text = "No save found";
            return;
        }

        try
        {
            SavePreview savePreview = JsonUtility.FromJson<SavePreview>(File.ReadAllText(SavePath));
            string stageTypeString = GetStageTypeString(savePreview.currentStageType);

            SaveSlotText.text = $"Round {savePreview.currentRoundIndex} \u00b7 {stageTypeString}";
            BtnContinue.interactable = true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"MainMenuController could not read the saved run: {exception.Message}");
            BtnContinue.interactable = false;
            SaveSlotText.text = "No save found";
        }
    }

    private static string GetStageTypeString(int stageType)
    {
        switch (stageType)
        {
            case 0:
                return "Combat";
            case 1:
                return "Elite";
            case 2:
                return "Shop";
            default:
                return "Combat";
        }
    }
}
