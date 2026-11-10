using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Inkshift
{
    [RequireComponent(typeof(GameHud), typeof(FeedbackAudio))]
    public sealed class GameFlow : MonoBehaviour
    {
        enum Mode { Title, StageSelect, Running, Paused, Cleared, Loading }

        [SerializeField] Campaign campaign;
        [SerializeField] int stageIndex = -1;
        [SerializeField] ShapeMotor player;
        [SerializeField] QuarterCamera view;
        [SerializeField] Transform startPoint;
        [SerializeField] bool startAsBall;
        [SerializeField] float deathHeight = -12f;

        GameHud hud;
        FeedbackAudio audioFeedback;
        StageZone checkpoint;
        Mode mode;
        float elapsed;
        float hintUntil;
        int retries;

        void Start()
        {
            Time.timeScale = 1f;
            hud = GetComponent<GameHud>();
            audioFeedback = GetComponent<FeedbackAudio>();
            hud.Bind(this);
            if (stageIndex < 0)
            {
                ShowTitle();
                return;
            }

            player.Feedback += audioFeedback.Play;
            mode = Mode.Running;
            player.AllowInput(true);
            PlaceAt(startPoint, startAsBall, inputDelay: 0.18f);
            hud.ShowStage(stageIndex, campaign.Count, campaign[stageIndex]);
            Hint("FIELD NOTES", campaign[stageIndex].Brief, 7f);
        }

        void Update()
        {
            Keyboard keys = Keyboard.current;
            if (keys != null && keys.escapeKey.wasPressedThisFrame)
            {
                if (mode == Mode.Running)
                    Pause();
                else if (mode == Mode.Paused)
                    Resume();
                else if (mode == Mode.StageSelect)
                    ShowTitle();
                return;
            }

            if (mode != Mode.Running)
                return;

            elapsed += Time.deltaTime;
            if (player.transform.position.y < deathHeight || (keys != null && keys.rKey.wasPressedThisFrame))
                Respawn();
            if (hintUntil > 0f && elapsed >= hintUntil)
            {
                hud.HideHint();
                hintUntil = 0f;
            }
            hud.UpdateMeters(player.IsBall, player.JumpsRemaining, view.Heading, elapsed, retries);
        }

        public void Play()
        {
            if (mode == Mode.Title)
                Load(campaign[campaign.NextUncleared].Scene);
        }

        public void ShowStages()
        {
            if (mode != Mode.Title)
                return;
            mode = Mode.StageSelect;
            hud.ShowStages(campaign);
        }

        public void ShowTitle()
        {
            if (mode == Mode.Loading || stageIndex >= 0)
                return;
            mode = Mode.Title;
            hud.ShowTitle(campaign);
        }

        public void SelectStage(int index)
        {
            if (mode == Mode.StageSelect && campaign.IsUnlocked(index))
                Load(campaign[index].Scene);
        }

        public void Pause()
        {
            if (mode != Mode.Running)
                return;
            mode = Mode.Paused;
            player.AllowInput(false);
            Time.timeScale = 0f;
            hud.ShowPause(campaign[stageIndex].Title);
        }

        public void Resume()
        {
            if (mode != Mode.Paused)
                return;
            mode = Mode.Running;
            Time.timeScale = 1f;
            player.AllowInput(true);
            hud.CloseDialog();
        }

        public void RestartStage()
        {
            if (mode is Mode.Paused or Mode.Cleared)
                Load(campaign[stageIndex].Scene);
        }

        public void LeaveStage()
        {
            if (mode is Mode.Paused or Mode.Cleared)
                Load(campaign.MenuScene);
        }

        public void Continue()
        {
            if (mode != Mode.Cleared)
                return;
            int next = stageIndex + 1;
            Load(next < campaign.Count ? campaign[next].Scene : campaign.MenuScene);
        }

        public void Quit()
        {
            if (mode == Mode.Title)
                Application.Quit();
        }

        public void EnterZone(StageZone zone, ShapeMotor motor)
        {
            if (mode != Mode.Running || motor != player)
                return;

            switch (zone.Kind)
            {
                case ZoneKind.Hint:
                    Hint(zone.Title, zone.Message, 6f);
                    break;
                case ZoneKind.Checkpoint when !checkpoint || zone.Order > checkpoint.Order:
                    checkpoint = zone;
                    zone.Activate();
                    audioFeedback.Play(SoundCue.Checkpoint);
                    hud.SetCheckpoint(true);
                    Hint("CHECKPOINT SET", zone.Message, 5.5f);
                    break;
                case ZoneKind.Finish:
                    ClearStage();
                    break;
            }
        }

        void Hint(string title, string message, float seconds)
        {
            hintUntil = elapsed + seconds;
            hud.ShowHint(title, message);
        }

        void ClearStage()
        {
            mode = Mode.Cleared;
            player.AllowInput(false);
            bool personalBest = campaign.RecordClear(stageIndex, elapsed);
            audioFeedback.Play(SoundCue.Clear);
            Time.timeScale = 0f;
            hud.ShowResult(campaign, stageIndex, elapsed, retries, personalBest);
        }

        void Respawn()
        {
            retries++;
            audioFeedback.Play(SoundCue.Fall);
            Transform spawn = checkpoint ? checkpoint.Spawn : startPoint;
            PlaceAt(spawn, player.IsBall);
            Hint("BACK ON THE LINE", checkpoint ? "Checkpoint restored. Hold forward to continue." : campaign[stageIndex].Brief, 3.6f);
        }

        void PlaceAt(Transform spawn, bool ball, float inputDelay = 0f)
        {
            int heading = Mathf.RoundToInt(spawn.eulerAngles.y / 90f) % 4;
            player.Place(spawn.position, heading, ball, inputDelay);
        }

        void Load(string sceneName)
        {
            mode = Mode.Loading;
            if (player)
                player.AllowInput(false);
            Time.timeScale = 1f;
            hud.ShowLoading();
            StartCoroutine(LoadScene(sceneName));
        }

        static IEnumerator LoadScene(string sceneName)
        {
            yield return null;
            yield return SceneManager.LoadSceneAsync(sceneName);
        }

        void OnDestroy()
        {
            if (player && audioFeedback)
                player.Feedback -= audioFeedback.Play;
            if (mode is Mode.Paused or Mode.Cleared)
                Time.timeScale = 1f;
        }
    }
}
