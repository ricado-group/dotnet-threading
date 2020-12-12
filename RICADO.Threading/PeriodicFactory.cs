using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RICADO.Threading
{
    public class PeriodicFactory
    {
        #region Private Properties

        private ConcurrentDictionary<string, IPeriodic> _periodicItems = new ConcurrentDictionary<string, IPeriodic>();

        #endregion


        #region Constructor

        /// <summary>
        /// Create a new <see cref="PeriodicFactory"/> Instance
        /// </summary>
        public PeriodicFactory()
        {
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Create a new <see cref="PeriodicTimer"/>
        /// </summary>
        /// <param name="name">A Name for the Timer</param>
        /// <param name="action">The Method to be periodically called</param>
        /// <param name="interval">The Interval between Method calls in Milliseconds</param>
        /// <param name="startDelay">An Optional Delay when Starting in Milliseconds (Defaults to 0ms)</param>
        /// <returns>The Name of the new Timer</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        public string CreateNew(string name, Action action, int interval, int startDelay = 0)
        {
            if(name == null)
            {
                name = Guid.NewGuid().ToString();
            }

            if(action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_periodicItems.ContainsKey(name))
            {
                throw new ArgumentException("The Specified Name has already been used by another Periodic Item", nameof(name));
            }

            _periodicItems.TryAdd(name, new PeriodicTimer(action, interval, startDelay));

            return name;
        }

        /// <summary>
        /// Create a new <see cref="PeriodicTimer"/>
        /// </summary>
        /// <param name="action">The Method to be periodically called</param>
        /// <param name="interval">The Interval between Method calls in Milliseconds</param>
        /// <param name="startDelay">An Optional Delay when Starting in Milliseconds (Defaults to 0ms)</param>
        /// <returns>The Auto-Generated Name of the new Timer</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        public string CreateNew(Action action, int interval, int startDelay = 0)
        {
            return CreateNew(null, action, interval, startDelay);
        }

        /// <summary>
        /// Create a new <see cref="PeriodicTask"/>
        /// </summary>
        /// <param name="name">A Name for the Task</param>
        /// <param name="action">The Method to be periodically called</param>
        /// <param name="interval">The Interval between Method calls in Milliseconds</param>
        /// <param name="startDelay">An Optional Delay when Starting in Milliseconds (Defaults to 0ms)</param>
        /// <returns>The Name of the new Task</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        public string CreateNew(string name, Func<CancellationToken, Task> action, int interval, int startDelay = 0)
        {
            if (name == null)
            {
                name = Guid.NewGuid().ToString();
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_periodicItems.ContainsKey(name))
            {
                throw new ArgumentException("The Specified Name has already been used by another Periodic Item", nameof(name));
            }

            _periodicItems.TryAdd(name, new PeriodicTask(action, interval, startDelay));

            return name;
        }

        /// <summary>
        /// Create a new <see cref="PeriodicTask"/>
        /// </summary>
        /// <param name="action">The Method to be periodically called</param>
        /// <param name="interval">The Interval between Method calls in Milliseconds</param>
        /// <param name="startDelay">An Optional Delay when Starting in Milliseconds (Defaults to 0ms)</param>
        /// <returns>The Auto-Generated Name of the new Task</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        public string CreateNew(Func<CancellationToken, Task> action, int interval, int startDelay = 0)
        {
            return CreateNew(null, action, interval, startDelay);
        }

        /// <summary>
        /// Starts a Single Periodic Item managed by this <see cref="PeriodicFactory"/>
        /// </summary>
        /// <param name="name">The Name of a Periodic Item to Start</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Task Start(string name)
        {
            if(name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_periodicItems.ContainsKey(name))
            {
                return _periodicItems[name].Start();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Starts all Periodic Items managed by this <see cref="PeriodicFactory"/>
        /// </summary>
        public async Task StartAll(CancellationToken cancellationToken)
        {
            foreach (IPeriodic item in _periodicItems.Values)
            {
                if (cancellationToken.IsCancellationRequested == false)
                {
                    await item.Start();
                }
            }
        }

        /// <summary>
        /// Stops a Single Periodic Item managed by this <see cref="PeriodicFactory"/>
        /// </summary>
        /// <param name="name">The Name of a Periodic Item to Stop</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Task Stop(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if(_periodicItems.ContainsKey(name))
            {
                return _periodicItems[name].Stop();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops all Periodic Items managed by this <see cref="PeriodicFactory"/>
        /// </summary>
        public async Task StopAll(CancellationToken cancellationToken)
        {
            List<Task> tasks = new List<Task>();
            
            foreach(IPeriodic item in _periodicItems.Values)
            {
                tasks.Add(item.Stop());
            }

            await Task.WhenAll(tasks);
        }

        #endregion
    }
}
