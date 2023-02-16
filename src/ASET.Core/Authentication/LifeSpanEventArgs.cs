using System;
using System.Collections.Generic;
using System.Text;

namespace ASET.Core.Authentication
{
    /// <summary>
    /// Provides data for token lifespan event.
    /// </summary>
    public class LifeSpanEventArgs
    {
        /// <summary>
        /// Gets the Total lifespan of the token.
        /// </summary>
        public int LifeSpan { get; set; }

        /// <summary>
        /// Gets the remaining lifespan of the token.
        /// </summary>
        public int LifeSpanRemaining { get; set; }
    }
}
