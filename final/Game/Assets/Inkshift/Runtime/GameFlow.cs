using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Inkshift
{
    [RequireComponent(typeof(GameHud), typeof(FeedbackAudio))]
    public sealed class GameFlow : MonoBehaviour
    {
        enum Mode { Arriving, Title, StageSelect, Running, Paused, Respawning, Finishing, Cleared, Loading }

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
        StageZone[] checkpoints;

        void Start()
        {
            Time.timeScale = 1f;
            hud = GetComponent<GameHud>();
            audioFeedback = GetComponent<FeedbackAudio>();
            hud.Bind(this);
            if (stageIndex >= 0)
            {
                player.Feedback += audioFeedback.Play;
                PlaceAt(startPoint, startAsBall);
                StageZone[] zones = FindObjectsByType<StageZone>(FindObjectsSortMode.None);
                checkpoints = Array.FindAll(zones, zone => zone.Kind == ZoneKind.Checkpoint);
                hud.ShowStage(stageIndex, campaign.Count, campaign[stageIndex], checkpoints.Length);
                hud.UpdateMeters(player.IsBall, player.JumpsRemaining, view.Heading, elapsed, retries);
            }
            else
            {
                hud.ShowTitle(campaign);
                hud.SetMenuInput(false);
            }
            StartCoroutine(Arrive());
        }

        IEnumerator Arrive()
        {
            hud.SetBlackout(1f);
            yield return null;
            yield return Fade(0f, 0.34f);
            if (stageIndex < 0)
            {
                mode = Mode.Title;
                hud.SetMenuInput(true);
                hud.FocusTitle();
            }
            else
            {
                mode = Mode.Running;
                player.AllowInput(true);
                Hint("ROUTE BRIEF", campaign[stageIndex].Brief, 7f);
            }
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
            {
                mode = Mode.Respawning;
                player.AllowInput(false);
                StartCoroutine(Respawn());
                return;
            }
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
            hud.FocusTitle();
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
                    int reached = 0;
                    foreach (StageZone marker in checkpoints)
                        if (marker.Order <= zone.Order)
                            reached++;
                    hud.SetCheckpoint(reached, checkpoints.Length);
                    Hint("CHECKPOINT SET", zone.Message, 5.5f);
                    break;
                case ZoneKind.Finish:
                    mode = Mode.Finishing;
                    StartCoroutine(ClearStage(zone.ExitDoor));
                    break;
            }
        }

        void Hint(string title, string message, float seconds)
        {
            hintUntil = elapsed + seconds;
            hud.ShowHint(title, message);
        }

        IEnumerator ClearStage(LabExitDoor door)
        {
            player.AllowInput(false);
            hud.HideHint();
            if (door)
            {
                hud.SetStageHudVisible(false);
                Vector3 from = player.BeginExit();
                view.BeginExitFrame();
                Vector3 staging = door.StagingPosition;
                Vector3 direction = staging - from;
                int approachSteps = Mathf.RoundToInt(Vector3.ProjectOnPlane(direction, Vector3.up).magnitude);
                yield return Animate(Mathf.Clamp(direction.magnitude / 4.8f, 0.32f, 0.7f), t =>
                {
                    player.MoveThroughExit(Vector3.Lerp(from, staging, t), direction, approachSteps * t);
                    view.FrameExit(door.transform.position, door.Forward, t);
                });
                player.SettleExitRoll();
                audioFeedback.Play(SoundCue.Shutter);
                yield return Animate(0.72f, door.SetOpenAmount);
                Vector3 inside = door.InteriorPosition;
                int entrySteps = Mathf.RoundToInt(Vector3.Distance(staging, inside));
                yield return Animate(1.15f, t =>
                {
                    player.MoveThroughExit(Vector3.Lerp(staging, inside, t), door.Forward, entrySteps * t);
                    view.FrameExit(door.transform.position, door.Forward, 1f, t);
                });
            }

            bool personalBest = campaign.RecordClear(stageIndex, elapsed);
            audioFeedback.Play(SoundCue.Clear);
            mode = Mode.Cleared;
            Time.timeScale = 0f;
            hud.ShowResult(campaign, stageIndex, elapsed, retries, personalBest);
        }

        IEnumerator Respawn()
        {
            retries++;
            audioFeedback.Play(SoundCue.Fall);
            yield return Fade(1f, 0.09f);
            Transform spawn = checkpoint ? checkpoint.Spawn : startPoint;
            PlaceAt(spawn, player.IsBall);
            hud.UpdateMeters(player.IsBall, player.JumpsRemaining, view.Heading, elapsed, retries);
            yield return Fade(0f, 0.17f);
            mode = Mode.Running;
            player.AllowInput(true);
            Hint(checkpoint ? "CHECKPOINT RESTORED" : "ROUTE RESTARTED",
                checkpoint ? "Hold forward to continue." : campaign[stageIndex].Brief, 3.6f);
        }

        void PlaceAt(Transform spawn, bool ball)
        {
            int heading = Mathf.RoundToInt(spawn.eulerAngles.y / 90f) % 4;
            player.Place(spawn.position, heading, ball, 0f);
        }

        void Load(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is missing from the build profile.", this);
                hud.ShowLoadError();
                return;
            }

            mode = Mode.Loading;
            hud.SetMenuInput(false);
            if (player)
                player.AllowInput(false);
            StartCoroutine(LoadScene(sceneName));
        }

        IEnumerator LoadScene(string sceneName)
        {
            yield return Fade(1f, 0.3f);
            yield return null;
            yield return SceneManager.LoadSceneAsync(sceneName);
        }

        IEnumerator Fade(float target, float duration)
        {
            float from = hud.Blackout;
            yield return Animate(duration, t => hud.SetBlackout(Mathf.Lerp(from, target, t)));
        }

        static IEnumerator Animate(float duration, Action<float> draw)
        {
            draw(0f);
            for (float time = 0f; time < duration;)
            {
                yield return null;
                time += Time.unscaledDeltaTime;
                draw(Mathf.SmoothStep(0f, 1f, time / duration));
            }
        }

        void OnDestroy()
        {
            if (player && audioFeedback)
                player.Feedback -= audioFeedback.Play;
            if (mode is Mode.Paused or Mode.Cleared or Mode.Loading)
                Time.timeScale = 1f;
        }
    }
}
