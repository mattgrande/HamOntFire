using System;
using Microsoft.AspNet.SignalR.Hubs;

namespace HamOntFire.Web.Models
{
    [HubName("eventManager")]
    public class EventManagerHub : Hub
    {
        private readonly EventManager _eventManager;

        public EventManagerHub() : this(EventManager.Instance) { }

#region ctor/dtor/Dispose

        public EventManagerHub(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        ~EventManagerHub()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_eventManager != null && !_eventManager.IsDisposed)
                    _eventManager.Dispose();
            }
        }

#endregion

        public void Start()
        {
            _eventManager.Start();
        }
    }
}