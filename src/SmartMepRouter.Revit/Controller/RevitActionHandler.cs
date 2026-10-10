using System;
using System.Collections.Concurrent;
using Autodesk.Revit.UI;

namespace SmartMepRouter.Revit.Controller
{
    /// <summary>
    /// Single <see cref="IExternalEventHandler"/> that drains a queue of actions on the Revit API thread (R-A2).
    /// The modeless window enqueues work and calls <see cref="ExternalEvent.Raise"/>.
    /// </summary>
    public sealed class RevitActionHandler : IExternalEventHandler
    {
        private readonly ConcurrentQueue<Action<UIApplication>> _queue = new ConcurrentQueue<Action<UIApplication>>();

        /// <summary>Raised with the exception when a queued action throws; the host decides how to surface it.</summary>
        public event Action<Exception>? ActionFailed;

        public void Enqueue(Action<UIApplication> action) => _queue.Enqueue(action);

        public void Execute(UIApplication app)
        {
            // Raise() coalesces calls, so always drain everything queued.
            while (_queue.TryDequeue(out Action<UIApplication>? action))
            {
                try
                {
                    action(app);
                }
                catch (Exception ex)
                {
                    ActionFailed?.Invoke(ex);
                }
            }
        }

        public string GetName() => "SmartMepRouter.RevitActionHandler";
    }
}
