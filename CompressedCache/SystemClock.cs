namespace CompressedCache
{
    using System;

    /// <summary>
    /// The system clock wrapper.
    /// </summary>
    public class SystemClock : ISystemClock
    {
        /// <summary>
        /// Gets the UTC now.
        /// </summary>
        /// <value>
        /// The UTC now.
        /// </value>
        public DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// Gets the minimum value.
        /// </summary>
        /// <value>
        /// The minimum value.
        /// </value>
        public DateTime MinValue => DateTime.MinValue;

        /// <summary>
        /// Gets the maximum value.
        /// </summary>
        /// <value>
        /// The maximum value.
        /// </value>
        public DateTime MaxValue => DateTime.MaxValue;
    }
}
