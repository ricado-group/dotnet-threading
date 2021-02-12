using System;
using System.Threading;
using System.Threading.Tasks;
using RICADO.Logging;

namespace RICADO.Threading
{
    public sealed class PeriodicTask : IPeriodic, IDisposable
    {
        #region Private Properties

        private Task _task;

        private Func<CancellationToken, Task> _action;

        private CancellationTokenSource _stoppingCts;

        private int _interval = Timeout.Infinite;

        private int _startDelay = Timeout.Infinite;

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
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _action = action;

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
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Start the <see cref="PeriodicTask"/>
        /// </summary>
        public Task Start()
        {
            if (_task != null)
            {
                return Task.CompletedTask;
            }

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
        public Task Stop()
        {
            if(_stoppingCts != null)
            {
                _stoppingCts.Cancel();
            }

            if(_task == null)
            {
                return Task.CompletedTask;
            }

            return _task;
        }

        /// <summary>
        /// Release all resources used by the current instance of <see cref="PeriodicTask"/>
        /// </summary>
        public void Dispose()
        {
            _stoppingCts?.Dispose();
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

            while(_stoppingCts.Token.IsCancellationRequested == false)
            {
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

                try
                {
                    await Task.Delay(_interval, _stoppingCts.Token).ConfigureAwait(false);
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
        }

        #endregion
    }
}
