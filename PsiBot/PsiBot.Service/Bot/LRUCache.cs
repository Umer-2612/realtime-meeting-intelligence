using System;

namespace PsiBot.Services.Bot
{
    /// <summary>
    /// Lightweight fixed-size LRU cache used to track multiview socket allocations.
    /// </summary>
    public class LRUCache
    {
        /// <summary>
        /// Maximum size of the LRU cache.
        /// </summary>
        public const uint Max = 10;

        private uint[] set;

        /// <summary>
        /// Initializes a new instance of the <see cref="LRUCache"/> class with the requested capacity.
        /// </summary>
        /// <param name="size">Number of entries to track.</param>
        /// <exception cref="ArgumentException">size value too large; max value is {Max}</exception>
        public LRUCache(uint size)
        {
            if (size > Max)
            {
                throw new ArgumentException($"size value too large; max value is {Max}");
            }

            this.set = new uint[size];
        }

        /// <summary>
        /// Gets the count of items in the cache.
        /// </summary>
        public uint Count { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "{" + string.Join(", ", this.set) + "}";
        }

        /// <summary>
        /// Inserts an item at the head of the cache, evicting the least-recently-used entry if needed.
        /// </summary>
        /// <param name="k">Item to insert.</param>
        /// <param name="e">Item to evict (optional).</param>
        public void TryInsert(uint k, out uint? e)
        {
            e = null;

            lock (this.set)
            {
                if (this.Count == 0)
                {
                    this.Count = 1;
                    this.set[0] = k;
                }

                else if (this.set[0] != k)
                {
                    uint ik = 0;
                    for (uint i = 1; i < this.Count; i++)
                    {
                        if (this.set[i] == k)
                        {
                            ik = i;
                            break;
                        }
                    }

                    if (ik == 0)
                    {
                        if (this.Count == this.set.Length)
                        {
                            e = this.set[this.Count - 1];
                            ik = this.Count - 1;
                        }
                        else
                        {
                            ik = this.Count++;
                        }
                    }

                    this.ShiftRight(ik);
                    this.set[0] = k;
                }
            }
        }

        /// <summary>
        /// Removes the specified item from the cache if present.
        /// </summary>
        /// <param name="k">Item to remove.</param>
        /// <returns>True if item was removed.</returns>
        public bool TryRemove(uint k)
        {
            lock (this.set)
            {
                for (uint i = 0; i < this.Count; i++)
                {
                    if (this.set[i] == k)
                    {
                        this.ShiftLeft(i);
                        this.Count--;
                        this.set[this.Count] = 0;
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Shifts all entries from index zero through x - 1 one position to the right.
        /// </summary>
        /// <param name="x">Index of last element being shifted.</param>
        private void ShiftRight(uint x)
        {
            while (x > 0)
            {
                this.set[x] = this.set[x - 1];
                x--;
            }
        }

        /// <summary>
        /// Shifts items after the provided index one position to the left.
        /// </summary>
        /// <param name="x">Index of items to shift left after this.</param>
        private void ShiftLeft(uint x)
        {
            while (x < this.set.Length - 1)
            {
                this.set[x] = this.set[x + 1];
                x++;
            }
        }
    }
}
