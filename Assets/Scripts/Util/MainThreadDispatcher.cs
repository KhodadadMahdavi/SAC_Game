using System;
using System.Collections.Generic;
using UnityEngine;

namespace TTT.Util
{
    /// <summary>
    /// Run actions on Unity main thread if you need to marshal background callbacks.
    /// </summary>
    [AddComponentMenu("TTT/Util/Main Thread Dispatcher")]
    public class MainThreadDispatcher : MonoBehaviour
    {
        public static MainThreadDispatcher Instance { get; private set; }
        private readonly Queue<Action> _queue = new Queue<Action>();

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Enqueue(Action a)
        {
            if (Instance == null) return;
            lock (Instance._queue) Instance._queue.Enqueue(a);
        }

        void Update()
        {
            Action a = null;
            while (true)
            {
                lock (_queue)
                {
                    if (_queue.Count == 0) break;
                    a = _queue.Dequeue();
                }
                try { a?.Invoke(); } catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}
