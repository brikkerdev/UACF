using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEditor;

namespace UACF.Core
{
    public static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        private static bool _initialized;

        static MainThreadDispatcher()
        {
            EnsureInitialized();
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            while (_queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        public static Task<T> Enqueue<T>(Func<T> action)
        {
            var tcs = new TaskCompletionSource<T>();
            _queue.Enqueue(() =>
            {
                try
                {
                    var result = action();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }

        public static Task Enqueue(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            _queue.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
