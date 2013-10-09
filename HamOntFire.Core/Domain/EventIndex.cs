using System;
using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace HamOntFire.Core.Domain
{
    public class Events_ByCreatedAtSortByCreatedAt : AbstractIndexCreationTask<Event>
    {
        public Events_ByCreatedAtSortByCreatedAt()
        {
            Map = events => from doc in events
                            select new {doc.CreatedAt};
        }
    }

    public class Events_ByUpdatedAtSortByUpdatedAt : AbstractIndexCreationTask<Event>
    {
        public Events_ByUpdatedAtSortByUpdatedAt()
        {
            Map = events => from doc in events
                            select new { doc.UpdatedAt };
        }
    }

    public class Events_Count : AbstractIndexCreationTask<Event, Events_Count.ReduceResult>
    {
        public class ReduceResult
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }

        public Events_Count()
        {
            Map = events => from doc in events
                            select new {Name = doc.Type, Count = 1};
            Reduce = results => from eventCount in results
                                group eventCount by eventCount.Name
                                into g
                                select new {Name = g.Key, Count = g.Sum(x => x.Count)};
            Sort(result => result.Count, SortOptions.Int);
        }
    }

    public class Events_UnitsPerType : AbstractIndexCreationTask<Event, Events_UnitsPerType.ReduceResult>
    {
        public class ReduceResult
        {
            public string Name { get; set; }
            public int Units { get; set; }
            public decimal UnitsPerType { get; set; }
        }

        public Events_UnitsPerType()
        {
            Map = events => from doc in events
                            select new { Name = doc.Type, doc.Units, UnitsPerType = 0};
            Reduce = results => from eventCount in results
                                group eventCount by eventCount.Name
                                into g
                                select new { Name = g.Key, Units = g.Sum(x => x.Units), UnitsPerType = g.Average(x => x.Units) };
            Sort(result => result.UnitsPerType, SortOptions.Double);
        }
    }

    public class Events_DistinctTypes : AbstractIndexCreationTask<Event, Events_DistinctTypes.ReduceResult>
    {
        public class ReduceResult
        {
            public string Type { get; set; }
        }

        public Events_DistinctTypes()
        {
            Map = events => from doc in events
                            select new { Type = doc.Type };
            Reduce = results => from eventCount in results
                                group eventCount by eventCount.Type
                                into g
                                select new { Type = g.Key };
            Sort(result => result.Type, SortOptions.String);
        }
    }

    public class Events_ByTweetIdSortByTweetId : AbstractIndexCreationTask<Event>
    {
        public Events_ByTweetIdSortByTweetId()
        {
            Map = events => from doc in events
                            select new {TweetId = doc.TweetId};
            Sort(x => x.TweetId, SortOptions.Long);
        }
    }
}
