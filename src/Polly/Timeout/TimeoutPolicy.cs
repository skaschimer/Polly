﻿namespace Polly.Timeout;

/// <summary>
/// A timeout policy which can be applied to delegates.
/// </summary>
public class TimeoutPolicy : Policy, ITimeoutPolicy
{
    private readonly TimeoutStrategy _timeoutStrategy;
    private readonly Func<Context, TimeSpan> _timeoutProvider;
    private readonly Action<Context, TimeSpan, Task, Exception> _onTimeout;

    internal TimeoutPolicy(
        Func<Context, TimeSpan> timeoutProvider,
        TimeoutStrategy timeoutStrategy,
        Action<Context, TimeSpan, Task, Exception> onTimeout)
    {
        _timeoutProvider = timeoutProvider;
        _timeoutStrategy = timeoutStrategy;
        _onTimeout = onTimeout;
    }

    /// <inheritdoc/>
    [DebuggerStepThrough]
    protected override TResult Implementation<TResult>(Func<Context, CancellationToken, TResult> action, Context context, CancellationToken cancellationToken)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return TimeoutEngine.Implementation(
            action,
            context,
            _timeoutProvider,
            _timeoutStrategy,
            _onTimeout,
            cancellationToken);
    }
}

/// <summary>
/// A timeout policy which can be applied to delegates returning a value of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
public class TimeoutPolicy<TResult> : Policy<TResult>, ITimeoutPolicy<TResult>
{
    private readonly TimeoutStrategy _timeoutStrategy;
    private readonly Func<Context, TimeSpan> _timeoutProvider;
    private readonly Action<Context, TimeSpan, Task, Exception> _onTimeout;

    internal TimeoutPolicy(
        Func<Context, TimeSpan> timeoutProvider,
        TimeoutStrategy timeoutStrategy,
        Action<Context, TimeSpan, Task, Exception> onTimeout)
    {
        _timeoutProvider = timeoutProvider;
        _timeoutStrategy = timeoutStrategy;
        _onTimeout = onTimeout;
    }

    /// <inheritdoc/>
    protected override TResult Implementation(Func<Context, CancellationToken, TResult> action, Context context, CancellationToken cancellationToken)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return TimeoutEngine.Implementation(
            action,
            context,
            _timeoutProvider,
            _timeoutStrategy,
            _onTimeout,
            cancellationToken);
    }
}
