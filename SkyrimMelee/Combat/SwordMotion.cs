namespace SkyrimMelee.Combat;

/// <summary>
/// Plays one sword clip at a time and cross-fades the pose over 100 ms.
/// This is the only animated transform in the game.
/// </summary>
public sealed class SwordMotion
{
	public const float CrossfadeDuration = 0.1f;

	private SwordClip? _clip;
	private float _time;
	private SwordPose _pose = SwordPose.Identity;
	private SwordPose _blendFrom = SwordPose.Identity;
	private float _blendRemaining;

	public SwordClip? Clip => _clip;

	public float Time => _time;

	public float NormalizedTime => _clip is null
		? 0f
		: Math.Clamp(_time / _clip.Duration, 0f, 1f);

	public bool IsFinished => _clip is not null && !_clip.Loop && _time >= _clip.Duration;

	public SwordPose CurrentPose => _pose;

	public void Play(SwordClip clip, bool restart = false)
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

	public void Update(float elapsedSeconds)
	{
		if (_clip is null || elapsedSeconds <= 0f)
		{
			return;
		}

		_time += elapsedSeconds;
		if (_clip.Loop)
		{
			_time %= _clip.Duration;
		}
		else if (_time > _clip.Duration)
		{
			_time = _clip.Duration;
		}

		var target = _clip.Sample(_time);

		if (_blendRemaining > 0f)
		{
			_blendRemaining = MathF.Max(0f, _blendRemaining - elapsedSeconds);
			var t = Math.Clamp(1f - _blendRemaining / CrossfadeDuration, 0f, 1f);
			t = SwordClip.ApplyEasing(Easing.SmoothStep, t);
			_pose = SwordPose.Lerp(_blendFrom, target, t);
		}
		else
		{
			_pose = target;
		}
	}
}
