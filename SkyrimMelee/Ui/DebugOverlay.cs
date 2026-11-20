using SkyrimMelee.Combat;
using SkyrimMelee.Core;

namespace SkyrimMelee.Ui
{
	partial class DebugOverlay
	{
		private readonly string _modeInfo;

		public DebugOverlay(GameOptions options)
		{
			BuildUI();

			_modeInfo = FormattableString.Invariant(
				$"mode god={(options.GodMode ? "on" : "off")} ui={(options.NoUi ? "off" : "on")}");
		}

		public void Toggle()
		{
			Visible = !Visible;
		}

		public void Update(GameClock clock, CombatStateMachine combat, string clipName,
			float clipNormalizedTime, string vitalsText, string guardText, string tuneText,
			string intentText, string motionText, string aiText, string aiWhy,
			string aiStats, IReadOnlyList<string> aiLog)
		{
			if (!Visible)
			{
				return;
			}

			_frame.Text = FormattableString.Invariant(
				$"frame {clock.FrameIndex}  sim {clock.SimTime:0.000} s  dt {clock.DeltaTime * 1000:0.00} ms");
			_scale.Text = FormattableString.Invariant(
				$"scale {clock.TimeScale:0.00}x  acc {clock.Accumulator * 1000:0.0} ms  dropped {clock.TotalDroppedSteps}  {_modeInfo}");
			_clip.Text = FormattableString.Invariant(
				$"clip {clipName}  t {clipNormalizedTime:0.00}");
			_fsm.Text = FormattableString.Invariant(
				$"fsm {combat.State} {combat.StateTime:0.00}s  active={(combat.IsActive ? "yes" : "no")} block={(combat.Blocks ? "yes" : "no")} lock={(combat.IsFacingLocked ? "yes" : "no")}");
			_vitals.Text = vitalsText;
			_guard.Text = guardText;
			_tune.Text = tuneText;
			_motion.Text = intentText + "  |  " + motionText;
			_ai.Text = aiText;
			_aiWhy.Text = aiWhy + "  |  " + aiStats;
			_aiLog0.Text = LogAt(aiLog, 0);
			_aiLog1.Text = LogAt(aiLog, 1);
		}

		private static string LogAt(IReadOnlyList<string> log, int index) =>
			index < log.Count ? log[index] : string.Empty;
	}
}
