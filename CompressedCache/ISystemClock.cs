using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressedCache
{
    using System;

    /// <summary>
    /// The system clock interface.
    /// </summary>
    public interface ISystemClock
    {
        /// <summary>
        /// Gets the UTC now.
        /// </summary>
        /// <value>
        /// The UTC now.
        /// </value>
        DateTime UtcNow { get; }

        /// <summary>
        /// Gets the minimum value.
        /// </summary>
        /// <value>
        /// The minimum value.
        /// </value>
        DateTime MinValue { get; }

        /// <summary>
        /// Gets the maximum value.
        /// </summary>
        /// <value>
        /// The maximum value.
        /// </value>
        DateTime MaxValue { get; }
    }
}
