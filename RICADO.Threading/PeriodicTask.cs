using System;
using System.Threading;
using System.Threading.Tasks;
using RICADO.Logging;

namespace RICADO.Threading
{
    public sealed class PeriodicTask : IPeriodic, IDisposable
    {
        #region Private Properties

#if !NETSTANDARD
        private PeriodicTimer _timer;
#endif

        private Task _task;

        private readonly Func<CancellationToken, Task> _action;

        private CancellationTokenSource _stoppingCts;

        private int _interval = Timeout.Infinite;

        private int _startDelay = Timeout.Infinite;

        private bool _running = false;
        private readonly object _runningLock = new object();

        #endregion


        #region Public Properties

        /// <inheritdoc/>
        public int Interval
        {
            get
            {
                return _interval;
            }
            set
            {
                _interval = value;
            }
        }

        /// <inheritdoc/>
        public int StartDelay
        {
            get
            {
                return _startDelay;
            }
            set
            {
                _startDelay = value;
            }
        }

        #endregion


        #region Constructor

        /// <summary>
        /// Create a new <see cref="PeriodicTask"/>
        /// </summary>
        /// <param name="action">The Method to be periodically called</param>
        /// <param name="interval">The Interval between Method calls in Milliseconds</param>
        /// <param name="startDelay">An Optional Delay when Starting in Milliseconds (Defaults to 0ms)</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public PeriodicTask(Func<CancellationToken, Task> action, int interval, int startDelay = 0)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));

            if (interval < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "The Interval Value cannot be Negative");
            }

            _interval = interval;

            if (startDelay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startDelay), startDelay, "The Start Delay Value cannot be Negative");
            }

            _startDelay = startDelay;

#if !NETSTANDARD
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_interval));
#endif

            _stoppingCts = new CancellationTokenSource();
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Start the <see cref="PeriodicTask"/>
        /// </summary>
        public Task Start()
        {
            lock (_runningLock)
            {
                if (_running == true)
                {
                    return Task.CompletedTask;
                }

                _running = true;
            }

            if (_task != null)
            {
                return Task.CompletedTask;
            }

#if !NETSTANDARD
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_interval));
#endif

            _stoppingCts = new CancellationTokenSource();

            try
            {
                _task = Task.Run(taskRunner, _stoppingCts.Token);
            }
            catch
            {
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop the <see cref="PeriodicTask"/>
        /// </summary>
        public async Task Stop()
        {
            lock (_runningLock)
            {
                if (_running == false)
                {
                    return;
                }

                _running = false;
            }

            _stoppingCts.Cancel();

#if !NETSTANDARD
            _timer.Dispose();
#endif

            if (_task == null)
            {
                return;
            }

            try
            {
                await _task;
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Release all resources used by the current instance of <see cref="PeriodicTask"/>
        /// </summary>
        public void Dispose()
        {
            lock (_runningLock)
            {
                _running = false;
            }

            _stoppingCts.Dispose();

#if !NETSTANDARD
            _timer.Dispose();
#endif
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// The Task Runner Method
        /// </summary>
        private async Task taskRunner()
        {
            if(_startDelay > 0)
            {
                try
                {
                    await Task.Delay(_startDelay, _stoppingCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (_stoppingCts.IsCancellationRequested)
                    {
                        throw;
                    }
                }
                catch
                {
                }
            }

            bool firstRun = true;

#if NETSTANDARD
            while(_stoppingCts.Token.IsCancellationRequested == false)
#else
            while(firstRun == true || await _timer.WaitForNextTickAsync(_stoppingCts.Token) == true)
#endif
            {
                lock(_runningLock)
                {
                    if(_running == false)
                    {
                        return;
                    }
                }

                if(_stoppingCts.IsCancellationRequested == true)
                {
                    return;
                }
                
                try
                {
                    await _action(_stoppingCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (_stoppingCts.IsCancellationRequested)
                    {
                        throw;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogCritical(e, "Unhandled Exception on the Periodic Task Action Method");
                }
                finally
                {
                    firstRun = false;
                }

#if NETSTANDARD
                try
                {
                    await Task.Delay(_interval, _stoppingCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if(_stoppingCts.IsCancellationRequested)
                    {
                        throw;
                    }
                }
                catch
                {
                }
#endif
            }
        }

        #endregion
    }
}
