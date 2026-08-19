using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GISTech.TerrainStreaming
{
    public static class TaskCancellationExtension
    {
        public static Task WhenCanceled(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// add cancellation functionality to Task T 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="swallowCancellationException">If True the <see cref="OperationCanceledException"/> will be swallowed</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        public static Task<T> CancelWith<T>(
            this Task<T> task, CancellationToken cancellationToken, bool swallowCancellationException = false)
        {
            return TaskCancellationInternals.CancelWithInternal(task, cancellationToken, swallowCancellationException);
        }


        /// <summary>
        /// add cancellation functionality to Task T with exception message 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="swallowCancellationException">If True the <see cref="OperationCanceledException"/> will be swallowed</param>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        public static Task<T> CancelWith<T>(
            this Task<T> task, CancellationToken cancellationToken, string message,
            bool swallowCancellationException = false)
        {
            return TaskCancellationInternals.CancelWithInternal(task, cancellationToken, message,
                swallowCancellationException);
        }


        /// <summary>
        /// add cancellation functionality to Tasks 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="swallowCancellationException">If True the <see cref="OperationCanceledException"/> will be swallowed</param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        public static Task CancelWith(
            this Task task, CancellationToken cancellationToken, bool swallowCancellationException = false)
        {
            return TaskCancellationInternals.CancelWithInternal(task, cancellationToken, swallowCancellationException);
        }


    }
    internal static class TaskCancellationInternals
    {
        public static async Task<T> CancelWithInternal<T>(Task<T> task, CancellationToken cancellationToken,bool swallowCancellationException = true)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                if (task != await Task.WhenAny(task, tcs.Task))
                    if (!swallowCancellationException)
                        throw new OperationCanceledException(cancellationToken);
                    else return default;
            return await task;
        }

        public static async Task<T> CancelWithInternal<T>(Task<T> task, CancellationToken cancellationToken,
            string message, bool swallowCancellationException = false)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                if (task != await Task.WhenAny(task, tcs.Task))
                    if (!swallowCancellationException)
                        throw new OperationCanceledException(message, cancellationToken);
                    else return default;
            return await task;
        }


        public static async Task CancelWithInternal(Task task, CancellationToken cancellationToken,
            bool swallowCancellationException = false)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                if (task != await Task.WhenAny(task, tcs.Task))
                    if (swallowCancellationException)
                        throw new OperationCanceledException(cancellationToken);
                    else return;
            await task;

        }


        public static async Task CancelWithInternal(
            Task task, CancellationToken cancellationToken, string message, bool swallowCancellationException = false)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                if (task != await Task.WhenAny(task, tcs.Task))
                    if (!swallowCancellationException)
                        throw new OperationCanceledException(message, cancellationToken);
                    else return;
            await task;
        }
    }
}