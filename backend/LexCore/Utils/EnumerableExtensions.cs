using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LexCore.Utils;

public static class EnumerableExtensions
{
    //borrowed from https://github.com/dotnet/runtime/blob/c81f403737c412942222d13b2753881e62d1e6f7/src/libraries/System.Linq.AsyncEnumerable/src/System/Linq/Chunk.cs#L30C13-L35C46
    //until we upgrade to .Net 10
    public static IAsyncEnumerable<TSource[]> Chunk<TSource>(
            this IAsyncEnumerable<TSource> source,
            int size)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

            return Chunk(source, size, default);

            async static IAsyncEnumerable<TSource[]> Chunk(
                IAsyncEnumerable<TSource> source,
                int size,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    // Before allocating anything, make sure there's at least one element.
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        // Now that we know we have at least one item, allocate an initial storage array. This is not
                        // the array we'll yield.  It starts out small in order to avoid significantly overallocating
                        // when the source has many fewer elements than the chunk size.
                        int arraySize = Math.Min(size, 4);
                        int i;
                        do
                        {
                            var array = new TSource[arraySize];

                            // Store the first item.
                            array[0] = e.Current;
                            i = 1;

                            if (size != array.Length)
                            {
                                // This is the first chunk. As we fill the array, grow it as needed.
                                for (; i < size && await e.MoveNextAsync().ConfigureAwait(false); i++)
                                {
                                    if (i >= array.Length)
                                    {
                                        arraySize = (int)Math.Min((uint)size, 2 * (uint)array.Length);
                                        Array.Resize(ref array, arraySize);
                                    }

                                    array[i] = e.Current;
                                }
                            }
                            else
                            {
                                // For all but the first chunk, the array will already be correctly sized.
                                // We can just store into it until either it's full or MoveNext returns false.
                                TSource[] local = array; // avoid bounds checks by using cached local (`array` is lifted to iterator object as a field)
                                Debug.Assert(local.Length == size);
                                for (; (uint)i < (uint)local.Length && await e.MoveNextAsync().ConfigureAwait(false); i++)
                                {
                                    local[i] = e.Current;
                                }
                            }

                            if (i != array.Length)
                            {
                                Array.Resize(ref array, i);
                            }

                            yield return array;
                        }
                        while (i >= size && await e.MoveNextAsync().ConfigureAwait(false));
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
}
