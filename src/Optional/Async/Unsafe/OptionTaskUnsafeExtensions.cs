#if !NET35

using System;
using System.Threading.Tasks;

namespace Optional.Async.Unsafe
{
    public static class OptionTaskUnsafeExtensions
    {
        /// <summary>
        /// Returns value or throws exception.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <typeparam name="ERR">Error type.</typeparam>
        /// <typeparam name="EX">Thrown exception type.</typeparam>
        /// <param name="optionTask">Awaitable option.</param>
        /// <param name="exceptionFactory">Produces exception to be thrown from inside the extension when option has no value.</param>
        /// <returns>Value when is present.</returns>
        /// <exception cref="{EX}" />
        public static async Task<T> ValueOrFailureAsync<T, ERR, EX>(this Task<Option<T, ERR>> optionTask, Func<ERR, EX> exceptionFactory) where EX : Exception
        {
            var option = await optionTask.ConfigureAwait(continueOnCapturedContext: false);

            T value = default;

            option.Match(
                some: x => value = x,
                none: e => throw exceptionFactory(e));

            return value;
        }

        /// <summary>
        /// Returns value or throws exception.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <typeparam name="EX">Thrown exception type.</typeparam>
        /// <param name="optionTask">Awaitable option.</param>
        /// <param name="exceptionFactory">Produces exception to be thrown from inside the extension when option has no value.</param>
        /// <returns>Value when is present.</returns>
        /// <exception cref="{EX}" />
        public static async Task<T> ValueOrFailureAsync<T, EX>(this Task<Option<T>> optionTask, Func<EX> exceptionFactory) where EX : Exception
        {
            var option = await optionTask.ConfigureAwait(continueOnCapturedContext: false);

            T value = default;

            option.Match(
                some: x => value = x,
                none: () => throw exceptionFactory());

            return value;
        }
    }
}

#endif