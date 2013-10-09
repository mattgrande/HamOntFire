using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HamOntFire.Core;
using HamOntFire.Core.Domain;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using log4net;

namespace HamOntFire.Web.Models
{
    public class EventManager : IDisposable
    {
// ReSharper disable InconsistentNaming
        private readonly static Lazy<EventManager>  _instance = new Lazy<EventManager>(() => new EventManager());
        private static readonly ILog _logger = LogManager.GetLogger(typeof (EventManager));
        private readonly static object _eventManagerStateLock = new object();
        private readonly static object _downloadingTweetsLock = new object();
        private readonly ConcurrentDictionary<string, Event> _events = new ConcurrentDictionary<string, Event>();
        private const int _updateInterval = 60000; // 30000ms = 30 seconds (Twitter has a max of 150 reqs/hr, or 2.5 reqs/min)
// ReSharper restore InconsistentNaming
        private Timer _timer;
        private bool _updatingTweets;
        private readonly Lazy<IHubConnectionContext> _clientsInstance = new Lazy<IHubConnectionContext>(() => GlobalHost.ConnectionManager.GetHubContext<EventManagerHub>().Clients);
        private long _greatestTweetId;

        public LoaderState State { get; set; }
        public bool IsDisposed { get; private set; }

        #region ctor/dtor/Dispose

        private EventManager()
        {
            IsDisposed = false;

            using (var session = MvcApplication.Store.OpenSession())
            {
                _logger.Debug("Getting greatest tweet.");
                var tweetManager = new TweetManager(session);
                _greatestTweetId = tweetManager.GetGreatestTweetId();
                _logger.Info("Greatest Tweet: " + _greatestTweetId);
            }
        }

        ~EventManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null)
                    _timer.Dispose();
                IsDisposed = true;
            }
        }

        #endregion

        public static EventManager Instance
        {
            get { return _instance.Value; }
        }

        public IHubConnectionContext Clients
        {
            get { return _clientsInstance.Value; }
        }

        public IEnumerable<Event> GetNewEvents()
        {
            return _events.Values;
        }

        public void Start()
        {
            _logger.Info("EventManager.Start");
            if (State == LoaderState.Waiting)
            {
                lock (_eventManagerStateLock)
                {
                    State = LoaderState.Downloading;
                    _timer = new Timer(DownloadTweets, null, 1000, _updateInterval);
                    State = LoaderState.Downloaded;
                }
            }
        }

        private void DownloadTweets(object state)
        {
            if (_updatingTweets)
                return;

            lock (_downloadingTweetsLock)
            {
                if (!_updatingTweets)
                {
                    _updatingTweets = true;
                    _logger.Debug("DownloadTweets");

                    try
                    {
                        using (var session = MvcApplication.Store.OpenSession())
                        {
                            var tweetManager = new TweetManager(session);
                            var events = tweetManager.DownloadTweets(_greatestTweetId);

                            if (events.Count > 0)
                                _logger.InfoFormat("Updating {0} tweets.", events.Count);

                            foreach (Event @event in events)
                            {
                                BroadcastEvent(@event);
                                if (@event.TweetId > _greatestTweetId)
                                    _greatestTweetId = @event.TweetId;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorFormat("Exception: {0} - {1}", ex.Message, ex.StackTrace);
                        if (_timer != null)
                            _timer.Dispose();
                        _timer = null;
                        State = LoaderState.Waiting;
                        Start();
                    }
                    finally
                    {
                        _updatingTweets = false;
                    }
                }
            }
        }

        private void BroadcastEvent(Event @event)
        {
            Clients.All.updateEvents( @event.ToViewModel() );
        }
    }

    public enum LoaderState
    {
        Waiting,
        Downloading,
        Downloaded,
    }
}