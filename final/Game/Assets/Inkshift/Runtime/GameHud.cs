using UnityEngine;
using UnityEngine.UIElements;

namespace Inkshift
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameHud : MonoBehaviour
    {
        static readonly string[] Headings = { "N", "E", "S", "W" };
        static readonly string[] JumpPips = { "○  ○", "●  ○", "●  ●" };

        VisualElement root;
        GameFlow flow;
        FeedbackAudio feedback;
        bool? lastBall;
        int lastJumps = -1;
        int lastHeading = -1;
        int lastTenths = -1;
        int lastRetries = -1;
        Label form;
        Label jumps;
        Label compass;
        Label timer;
        Label retryCount;

        public void Bind(GameFlow gameFlow)
        {
            flow = gameFlow;
            feedback = GetComponent<FeedbackAudio>();
            root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
            form = Get<Label>("form");
            jumps = Get<Label>("jumpPips");
            compass = Get<Label>("compass");
            timer = Get<Label>("timer");
            retryCount = Get<Label>("retryCount");
            BindClick(Get<Button>("playButton"), flow.Play);
            BindClick(Get<Button>("stagesButton"), flow.ShowStages);
            BindClick(Get<Button>("backButton"), flow.ShowTitle);
            Button pause = Get<Button>("pauseButton");
            pause.focusable = false;
            BindClick(pause, flow.Pause);
            BindClick(Get<Button>("quitButton"), flow.Quit);
            Visible("quitButton", !Application.isEditor);
        }

        public void ShowTitle(Campaign campaign)
        {
            Screen("titleScreen");
            Reveal(Get<VisualElement>("titleScreen").Q(className: "title-panel"));
            int cleared = campaign.ClearedCount;
            Get<Label>("titleProgress").text = $"{cleared:D2} / {campaign.Count:D2} ROUTES CLEARED";
            Get<Button>("playButton").text = cleared == 0 ? "START RUN" : cleared == campaign.Count ? "RUN AGAIN" : "CONTINUE RUN";
            Focus(Get<Button>("playButton"));
        }

        public void ShowStages(Campaign campaign)
        {
            Screen("stageScreen");
            Reveal(Get<VisualElement>("stageScreen").Q(className: "card"));
            VisualElement list = Get<VisualElement>("stageList");
            list.Clear();
            Button first = null;
            for (int i = 0; i < campaign.Count; i++)
            {
                int index = i;
                StageInfo info = campaign[i];
                bool unlocked = campaign.IsUnlocked(i);
                float best = campaign.BestTime(i);

                var row = new Button();
                BindClick(row, () => flow.SelectStage(index));
                row.AddToClassList("stage-row");
                var number = new Label($"{i + 1:D2}");
                number.AddToClassList("stage-number");
                row.Add(number);

                var route = new VisualElement();
                route.AddToClassList("stage-route");
                var title = new Label(info.Title);
                title.AddToClassList("stage-name");
                var focus = new Label(info.Focus);
                focus.AddToClassList("caption");
                route.Add(title);
                route.Add(focus);
                row.Add(route);

                var state = new Label(best > 0f ? FormatTime(best) : unlocked ? "READY" : "LOCKED");
                state.AddToClassList("stage-state");
                row.Add(state);
                row.SetEnabled(unlocked);
                list.Add(row);
                if (unlocked && first == null)
                    first = row;
            }
            Focus(first ?? Get<Button>("backButton"));
        }

        public void ShowStage(int index, int count, StageInfo stage)
        {
            Screen("stageHud");
            Get<Label>("routeId").text = $"ROUTE {index + 1:D2} / {count:D2}";
            Get<Label>("routeName").text = stage.Title;
            SetCheckpoint(false);
        }

        public void UpdateMeters(bool ball, int jumpsLeft, int heading, float elapsed, int retries)
        {
            if (lastBall != ball)
            {
                form.text = ball ? "BALL" : "CUBE";
                form.EnableInClassList("ball", ball);
                if (lastBall.HasValue)
                    Pulse(form);
                lastBall = ball;
            }
            if (lastJumps != jumpsLeft)
            {
                jumps.text = JumpPips[jumpsLeft];
                if (lastJumps >= 0)
                    Pulse(jumps);
                lastJumps = jumpsLeft;
            }
            if (lastHeading != heading)
            {
                compass.text = Headings[heading];
                if (lastHeading >= 0)
                    Pulse(compass);
                lastHeading = heading;
            }
            int tenths = Mathf.FloorToInt(elapsed * 10f);
            if (lastTenths != tenths)
            {
                timer.text = FormatTime(elapsed);
                lastTenths = tenths;
            }
            if (lastRetries != retries)
            {
                retryCount.text = $"RETRIES  {retries:D2}";
                lastRetries = retries;
            }
        }

        public void SetCheckpoint(bool reached)
        {
            Label badge = Get<Label>("checkpoint");
            badge.text = reached ? "CHECKPOINT SET" : "R  /  RETRY";
            badge.EnableInClassList("reached", reached);
            if (reached)
                Pulse(badge);
        }

        public void ShowHint(string title, string message)
        {
            Get<Label>("hintTitle").text = title;
            Get<Label>("hintBody").text = message;
            Visible("hint", true);
            Reveal(Get<VisualElement>("hint"));
        }

        public void HideHint() => Visible("hint", false);

        public void ShowPause(string stageName)
        {
            ShowDialog("FLIGHT ON HOLD", "Paused", stageName, "RESUME", flow.Resume);
        }

        public void ShowResult(Campaign campaign, int index, float elapsed, int retries, bool best)
        {
            bool last = index == campaign.Count - 1;
            bool complete = last && campaign.ClearedCount == campaign.Count;
            string status = complete ? "ALL ROUTES CLEARED" : $"ROUTE {index + 1:D2} CLEARED";
            string result = $"{FormatTime(elapsed)}   /   {retries:D2} RETRIES";
            if (best)
                result += "   /   NEW BEST";
            ShowDialog(status, complete ? "Sky painted." : campaign[index].Title, result,
                last ? "BACK TO TITLE" : "NEXT ROUTE", flow.Continue, !last);
        }

        void ShowDialog(string eyebrow, string title, string text, string action, System.Action callback, bool showTitleAction = true)
        {
            Get<Label>("dialogEyebrow").text = eyebrow;
            Get<Label>("dialogTitle").text = title;
            Get<Label>("dialogText").text = text;
            VisualElement actions = Get<VisualElement>("dialogActions");
            actions.Clear();
            Button primary = MakeButton(action, callback, true);
            actions.Add(primary);
            actions.Add(MakeButton("RESTART ROUTE", flow.RestartStage));
            if (showTitleAction)
                actions.Add(MakeButton("TITLE MENU", flow.LeaveStage));
            Visible("dialog", true);
            Reveal(Get<VisualElement>("dialog").Q(className: "card"));
            Focus(primary);
        }

        Button MakeButton(string title, System.Action action, bool primary = false)
        {
            var button = new Button { text = title };
            BindClick(button, action);
            button.AddToClassList("action");
            if (primary)
                button.AddToClassList("primary");
            return button;
        }

        void BindClick(Button button, System.Action action)
        {
            button.clicked += () =>
            {
                feedback.Play(SoundCue.Flip);
                action();
            };
        }

        static void Reveal(VisualElement element)
        {
            element.AddToClassList("enter-from");
            element.schedule.Execute(() => element.RemoveFromClassList("enter-from")).StartingIn(35);
        }

        static void Pulse(VisualElement element)
        {
            element.AddToClassList("feedback-pulse");
            element.schedule.Execute(() => element.RemoveFromClassList("feedback-pulse")).StartingIn(140);
        }

        public void CloseDialog() => Visible("dialog", false);
        public void ShowLoading() => Screen("loading");

        void Screen(string active)
        {
            Visible("titleScreen", active == "titleScreen");
            Visible("stageScreen", active == "stageScreen");
            Visible("stageHud", active == "stageHud");
            Visible("loading", active == "loading");
            CloseDialog();
        }

        T Get<T>(string name) where T : VisualElement => root.Q<T>(name);
        void Visible(string name, bool visible) => Get<VisualElement>(name).style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        static void Focus(Button button) => button.schedule.Execute(() => button.Focus());

        static string FormatTime(float seconds)
        {
            int tenths = Mathf.FloorToInt(seconds * 10f);
            return $"{tenths / 600:D2}:{tenths / 10 % 60:D2}.{tenths % 10}";
        }
    }
}
