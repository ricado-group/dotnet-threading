using System.Threading.Tasks;

namespace RICADO.Threading
{
    public interface IPeriodic
    {
        /// <summary>
        /// The Interval between Method calls in Milliseconds
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// The Delay when Starting in Milliseconds
        /// </summary>
        public int StartDelay { get; set; }
        
        /// <summary>
        /// Start the Periodic Item
        /// </summary>
        public Task Start();

        /// <summary>
        /// Stop the Periodic Item
        /// </summary>
        public Task Stop();
    }
}
