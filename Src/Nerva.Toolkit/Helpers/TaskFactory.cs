using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngryWasp.Logger;

namespace Nerva.Toolkit.Helpers
{
    public class TaskContainer
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Task Task { get; set; }
    }

    public delegate void CancellableTask(CancellationToken token);

    public class AsyncTaskContainer
    {

        private Task t;

        private CancellationTokenSource ct;

        private bool isRunning = false;

        public bool IsRunning => isRunning;

        public void Start(Func<CancellationToken, Task> function)
        {
            ct = new CancellationTokenSource();
            ct.Token.Register(OnStopped);
            t = function(ct.Token);
            isRunning = true;
        }

        public void Stop()
        {
            if (!isRunning)
                return;
                
            isRunning = false;
            ct.Cancel();
        }

        private void OnStopped()
        {
            isRunning = false;
        }
    }

    public class TaskFactory
    {
        private ConcurrentDictionary<string, TaskContainer> containers = new ConcurrentDictionary<string, TaskContainer>();

        private static TaskFactory instance = new TaskFactory();

        public static TaskFactory Instance => instance;

        public TaskContainer this[string name]
        {
            get
            {
                TaskContainer c = null;
                if (containers.TryGetValue(name, out c))
                    return c;

                return null;
            }
        }

        public int Count => containers.Count;

        public Task RunTask(string name, string description, Action action)
        {
            //Basically, we try to add the task to the list, if that works, we start the task,
            //otherwise we write a message in the log and return
            if (containers.TryAdd(name, new TaskContainer{
                Name = name,
                Description = description}))
            {
                containers[name].Task = Task.Run(action);
                return containers[name].Task;
            }
            else
            {
                Log.Instance.Write(Log_Severity.Error, "Task with name {0} already running");
                return null;
            }
        }

        public void Prune()
        {
            foreach(var c in containers)
            {
                string toRemove = null;
                
                if (c.Value.Task.IsCanceled || c.Value.Task.IsFaulted || c.Value.Task.IsCompleted)
                    toRemove = c.Key;

                if (toRemove != null)
                {
                    TaskContainer tc = null;
                    if (!containers.TryRemove(c.Key, out tc))
                        Log.Instance.Write(Log_Severity.Error, "Failed to remove task {0} from TaskFactory");
                }
            }
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode() ^ Count;
        }

        public override string ToString()
        {
            StringBuilder sb =new StringBuilder();
            foreach (var c in containers)
                sb.AppendLine(c.Value.Description);

            return sb.ToString();
        }
    }
}