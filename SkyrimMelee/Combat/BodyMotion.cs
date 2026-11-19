namespace SkyrimMelee.Combat;

/// <summary>
/// Plays one rigid body reaction at a time and cross-fades over 100 ms.
/// Mirrors <see cref="SwordMotion"/> for the capsule transform.
/// </summary>
public sealed class BodyMotion
{
	public const float CrossfadeDuration = CombatTuning.CrossfadeDuration;

	private BodyClip? _clip;
	private float _time;
	private BodyPose _pose = BodyPose.Identity;
	private BodyPose _blendFrom = BodyPose.Identity;
	private float _blendRemaining;

	public BodyClip? Clip => _clip;

	public float Time => _time;

	public bool IsFinished => _clip is not null && _time >= _clip.Duration;

	public BodyPose CurrentPose => _pose;

	public void Play(BodyClip clip, bool restart = false)
	{
		if (_clip == clip && !restart)
		{
			return;
		}

		if (_clip is not null)
		{
			_blendFrom = _pose;
			_blendRemaining = CrossfadeDuration;
			_clip = clip;
			_time = 0f;
			return;
		}

		_clip = clip;
		_time = 0f;
		_pose = clip.Sample(0f);
	}

	/// <summary>Snaps back to the identity pose without a cross-fade.</summary>
	public void Reset()
	{
		_clip = null;
		_time = 0f;
		_pose = BodyPose.Identity;
		_blendFrom = _pose;
		_blendRemaining = 0f;
	}

	public void Update(float elapsedSeconds)
	{
		if (_clip is null || elapsedSeconds <= 0f)
		{
			return;
		}

		_time += elapsedSeconds;
		if (_time > _clip.Duration)
		{
			_time = _clip.Duration;
		}

		var target = _clip.Sample(_time);

		if (_blendRemaining > 0f)
		{
			_blendRemaining = MathF.Max(0f, _blendRemaining - elapsedSeconds);
			var t = Math.Clamp(1f - _blendRemaining / CrossfadeDuration, 0f, 1f);
			t = SwordClip.ApplyEasing(Easing.SmoothStep, t);
			_pose = BodyPose.Lerp(_blendFrom, target, t);
		}
		else
		{
			_pose = target;
		}
	}
}
