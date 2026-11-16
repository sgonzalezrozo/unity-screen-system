using System.Collections.Generic;

namespace ScreenSystem.Editor.ScreenManagerEditor
{
    public enum BackStepKind
    {
        Alert,
        Modal,
        Screen,
        Root
    }

    /// <summary>One entry of the back stack and what pressing back does while it's on top.</summary>
    public readonly struct BackStep
    {
        public readonly string Screen;
        public readonly BackStepKind Kind;
        public readonly string Action;

        public BackStep(string screen, BackStepKind kind, string action)
        {
            Screen = screen;
            Kind = kind;
            Action = action;
        }
    }

    /// <summary>
    /// Describes the back stack the way GoBack walks it: an open alert blocks back, a modal closes first, then each
    /// screen returns to the one below it, and the root starts the quit flow or raises OnBackAtRoot.
    /// </summary>
    public static class BackStackDescriber
    {
        public const string NothingOpen = "Nothing open: back is left to your own UI";

        /// <param name="historyTopFirst">Fullscreen history, current screen first and root last.</param>
        /// <param name="quitConfirmation">The quit confirmation modal, or null when back at the root quits straight away.</param>
        public static List<BackStep> Describe(IReadOnlyList<string> historyTopFirst, string modal, string alert, bool quitOnBackAtRoot, string quitConfirmation)
        {
            List<BackStep> steps = new();

            if (!string.IsNullOrEmpty(alert))
                steps.Add(new BackStep(alert, BackStepKind.Alert, "Back is blocked: close it with DismissAlert()"));

            if (!string.IsNullOrEmpty(modal))
                steps.Add(new BackStep(modal, BackStepKind.Modal, "Back closes it"));

            int count = historyTopFirst?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                bool isRoot = i == count - 1;
                string action = isRoot ? RootAction(quitOnBackAtRoot, quitConfirmation) : $"Back returns to {historyTopFirst[i + 1]}";
                steps.Add(new BackStep(historyTopFirst[i], isRoot ? BackStepKind.Root : BackStepKind.Screen, action));
            }

            return steps;
        }

        /// <summary>What the next back does.</summary>
        public static string NextBack(List<BackStep> steps)
            => steps.Count > 0 ? $"{steps[0].Screen}: {steps[0].Action}" : NothingOpen;

        private static string RootAction(bool quitOnBackAtRoot, string quitConfirmation)
        {
            if (!quitOnBackAtRoot)
                return "Back raises NavigationEvents.OnBackAtRoot (Quit On Back At Root is off)";

            return string.IsNullOrEmpty(quitConfirmation)
                ? "Back quits the app (no QuitConfirmation modal)"
                : $"Back opens {quitConfirmation}";
        }
    }
}
