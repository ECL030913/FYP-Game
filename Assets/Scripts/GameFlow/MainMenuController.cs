using UnityEngine;
using UnityEngine.UI;
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
        public int currentStageIndex;
        public int currentRoundIndex;
        public int currentStageType;
        public float savedPlayerHealth;
        public float maxHealthBonus;
    }

    [SerializeField] private Button BtnNewGame;
    [SerializeField] private Button BtnContinue;
    [SerializeField] private TMP_Text SaveSlotText;
    [SerializeField] private Button BtnQuit;
    private bool isLoadingStage;

    private string SavePath => Path.Combine(Application.persistentDataPath, "module1_run_save.json");

    private void Awake()
    {
        BtnNewGame.onClick.AddListener(StartNewGame);
        BtnContinue.onClick.AddListener(ContinueGame);
        BtnQuit.onClick.AddListener(QuitGame);
        PixelUiTheme.ApplyMainMenu(BtnNewGame, BtnContinue, BtnQuit, SaveSlotText);
    }

    private void Start()
    {
        UpdateSavePreview();
    }

    private void StartNewGame()
    {
        if (isLoadingStage)
        {
            return;
        }

        if (!StageSceneRouter.CanLoadStage(StageType.Combat))
        {
            SaveSlotText.text = "Combat scene is unavailable";
            return;
        }

        isLoadingStage = true;
        SetMenuButtonsInteractable(false);
        RunManager.EnsureInstance().BeginNewRun();
        if (StageSceneRouter.LoadStageAsync(StageType.Combat) == null)
        {
            isLoadingStage = false;
            SetMenuButtonsInteractable(true);
            SaveSlotText.text = "Could not load Combat";
        }
    }

    private void ContinueGame()
    {
        if (isLoadingStage)
        {
            return;
        }

        RunManager runManager = RunManager.EnsureInstance();
        runManager.ContinueRun();
        StageType stageType = runManager.Data.currentStageType;
        if (!StageSceneRouter.CanLoadStage(stageType))
        {
            SaveSlotText.text = $"{stageType} scene is unavailable";
            return;
        }

        isLoadingStage = true;
        SetMenuButtonsInteractable(false);
        if (StageSceneRouter.LoadStageAsync(stageType) == null)
        {
            isLoadingStage = false;
            SetMenuButtonsInteractable(true);
            SaveSlotText.text = $"Could not load {stageType}";
        }
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

            int stageIndex = Mathf.Clamp(savePreview.currentStageIndex, 1, StageManager.MaxStageCount);
            SaveSlotText.text = $"Stage {stageIndex} / {StageManager.MaxStageCount} \u00b7 {stageTypeString}";
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
            case 3:
                return "Boss";
            default:
                return "Combat";
        }
    }

    private void SetMenuButtonsInteractable(bool interactable)
    {
        BtnNewGame.interactable = interactable;
        BtnContinue.interactable = interactable && RunManager.EnsureInstance().HasSavedRun;
        BtnQuit.interactable = interactable;
    }
}
