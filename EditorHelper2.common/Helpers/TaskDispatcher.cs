using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EditorHelper2.common.Helpers
{
    public class TaskDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> QueuedActions = new ConcurrentQueue<Action>();

        public static void QueueOnMainThread(Action action)
        {
            QueuedActions.Enqueue(action);
        }

        public static Task QueueOnMainThreadAsync(Action action, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            // https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
            var tcs = new TaskCompletionSource<object?>(TaskContinuationOptions.RunContinuationsAsynchronously);

            CancellationTokenRegistration ctr = default;
            if (cancellationToken.CanBeCanceled)
            {
                ctr = cancellationToken.Register(() =>
                {
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            QueuedActions.Enqueue(() =>
            {
                if (tcs.Task.IsCompleted) return; // Cancelled

                try
                {
                    action();
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    ctr.Dispose();
                }
            });

            return tcs.Task;
        }

        private void FixedUpdate()
        {
            while (QueuedActions.TryDequeue(out Action action))
            {
                action();
            }
        }
    }
}
