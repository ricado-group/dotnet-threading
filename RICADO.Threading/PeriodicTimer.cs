using System;
using System.Threading;
using System.Threading.Tasks;
using RICADO.Logging;

namespace RICADO.Threading
{
    public sealed class PeriodicTimer : IPeriodic, IDisposable
    {
        #region Private Properties

        private Timer _timer;
        private object _timerLock = new object();

        private Action _action;

        private int _interval = Timeout.Infinite;

        private int _startDelay = Timeout.Infinite;

        private bool _running = false;
        private object _runningLock = new object();

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

        /// <summary>
        /// Whether the <see cref="PeriodicTimer"/> is Running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_runningLock)
                {
                    return _running;
                }
            }
            private set
            {
                lock (_runningLock)
                {
                    _running = value;
                }
            }
        }

        #endregion


        #region Constructor

        /// <summary>
        /// Create a new <see cref="PeriodicTimer"/>
        /// </summary>
        /// <param name="action">The Method to be periodically called</param>
        /// <param name="interval">The Interval between Method calls in Milliseconds</param>
        /// <param name="startDelay">An Optional Delay when Starting in Milliseconds (Defaults to 0ms)</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public PeriodicTimer(Action action, int interval, int startDelay = 0)
        {
            if(action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _action = action;

            if(interval < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "The Interval Value cannot be Negative");
            }
            
            _interval = interval;

            if(startDelay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startDelay), startDelay, "The Start Delay Value cannot be Negative");
            }

            _startDelay = startDelay;

            lock(_timerLock)
            {
                _timer = new Timer(timerCallback);
            }
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Start the <see cref="PeriodicTimer"/>
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

            lock (_timerLock)
            {
                _timer.Change(_startDelay, Timeout.Infinite);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop the <see cref="PeriodicTimer"/>
        /// </summary>
        public Task Stop()
        {
            lock (_runningLock)
            {
                if (_running == false)
                {
                    return Task.CompletedTask;
                }

                _running = false;
            }

            lock (_timerLock)
            {
                if (_timer != null)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Release all resources used by the current instance of <see cref="PeriodicTimer"/>
        /// </summary>
        public void Dispose()
        {
            lock (_runningLock)
            {
                _running = false;
            }

            lock (_timerLock)
            {
                if (_timer != null)
                {
                    _timer.Dispose();

                    _timer = null;
                }
            }
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// The Timer Callback Method
        /// </summary>
        /// <param name="state">The Timer State Object</param>
        private void timerCallback(object state)
        {
            if (IsRunning == false)
            {
                return;
            }

            try
            {
                lock (_timerLock)
                {
                    if (_timer != null)
                    {
                        _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
            catch
            {
            }

            if (IsRunning == false)
            {
                return;
            }

            try
            {
                _action.Invoke();
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, "Unhandled Exception on the Periodic Timer Action Method");
            }

            if (IsRunning == false)
            {
                return;
            }

            try
            {
                lock (_timerLock)
                {
                    if (_timer != null)
                    {
                        _timer.Change(_interval, Timeout.Infinite);
                    }
                }
            }
            catch
            {
            }
        }

        #endregion
    }
}
