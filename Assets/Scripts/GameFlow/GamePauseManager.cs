using System.Collections.Generic;
using UnityEngine;

public class GamePauseManager : MonoBehaviour
{
    private readonly HashSet<string> pauseReasons = new HashSet<string>();
    private int suppressEscapeThroughFrame = -1;

    public static GamePauseManager Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.pauseReasons.Count > 0;

    public static GamePauseManager EnsureForScene()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("Game Pause Manager");
        return managerObject.AddComponent<GamePauseManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        // Another UI may consume Escape earlier in the same frame (for
        // example cancelling a Shop purchase). Script Update order is not
        // deterministic, so do not let that same key press open PauseMenu.
        if (Time.frameCount <= suppressEscapeThroughFrame)
        {
            return;
        }

        // Death and victory already own the screen and terminate the run; do
        // not layer an ESC menu over either terminal menu.
        if (RunManager.Instance != null && !RunManager.Instance.IsRunReady)
        {
            return;
        }

        Module1Ui ui = Module1Ui.EnsureForScene();
        if (ui.IsGuideVisible)
        {
            // The guide owns Escape while visible. This also prevents the
            // PauseMenu reason from being removed before the guide can return
            // to that menu in the same frame.
            return;
        }

        if (pauseReasons.Contains("PauseMenu"))
        {
            ResumeFromPauseMenu();
            return;
        }

        // Level-up and Shop decisions cannot be dismissed with Escape.
        if (pauseReasons.Count > 0)
        {
            return;
        }

        Pause("PauseMenu");
        ui.ShowPauseMenu();
    }

    public void Pause(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        pauseReasons.Add(reason);
        Time.timeScale = 0f;
    }

    public void Resume(string reason)
    {
        pauseReasons.Remove(reason);
        if (pauseReasons.Count == 0)
        {
            Time.timeScale = 1f;
        }
    }

    public void ResumeFromPauseMenu()
    {
        Module1Ui.EnsureForScene().HidePauseMenu();
        Resume("PauseMenu");
    }

    public void SuppressEscapeForCurrentFrame()
    {
        suppressEscapeThroughFrame = Mathf.Max(
            suppressEscapeThroughFrame,
            Time.frameCount);
    }

    public void ResumeAll()
    {
        pauseReasons.Clear();
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }
}
