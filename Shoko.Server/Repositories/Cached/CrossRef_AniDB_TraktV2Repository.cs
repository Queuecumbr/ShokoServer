﻿using System.Collections.Generic;
using Shoko.Models.Server;
using NHibernate;
using NHibernate.Criterion;
using Shoko.Server.Databases;
using Shoko.Server.Models;
using System.Linq;
using System;
using Shoko.Commons.Collections;
using NutzCode.InMemoryIndex;

namespace Shoko.Server.Repositories.Cached
{
    public class CrossRef_AniDB_TraktV2Repository : BaseCachedRepository<CrossRef_AniDB_TraktV2, int>
    {
        private CrossRef_AniDB_TraktV2Repository()
        {
        }

        public override void RegenerateDb()
        {
        }

        protected override int SelectKey(CrossRef_AniDB_TraktV2 entity)
        {
            return entity.CrossRef_AniDB_TraktV2ID;
        }

        private PocoIndex<int, CrossRef_AniDB_TraktV2, int> AnimeIDs;
        public override void PopulateIndexes()
        {
            AnimeIDs = new PocoIndex<int, CrossRef_AniDB_TraktV2, int>(Cache, a => a.AnimeID);
        }

        public static CrossRef_AniDB_TraktV2Repository Create()
        {
            return new CrossRef_AniDB_TraktV2Repository();
        }

        public List<CrossRef_AniDB_TraktV2> GetByAnimeID(int id)
        {
            using (var session = DatabaseFactory.SessionFactory.OpenSession())
            {
                return GetByAnimeID(session, id);
            }
        }

        public ILookup<int, CrossRef_AniDB_TraktV2> GetByAnimeIDs(IReadOnlyCollection<int> animeIds)
        {
            if (animeIds == null)
                throw new ArgumentNullException(nameof(animeIds));

            if (animeIds.Count == 0)
            {
                return EmptyLookup<int, CrossRef_AniDB_TraktV2>.Instance;
            }

            lock (Cache)
            {
                return animeIds.SelectMany(id => AnimeIDs.GetMultiple(id))
                    .ToLookup(xref => xref.AnimeID);
            }
        }

        public List<CrossRef_AniDB_TraktV2> GetByAnimeID(ISession session, int id)
        {
            var xrefs = session
                .CreateCriteria(typeof(CrossRef_AniDB_TraktV2))
                .Add(Restrictions.Eq("AnimeID", id))
                .AddOrder(Order.Asc("AniDBStartEpisodeType"))
                .AddOrder(Order.Asc("AniDBStartEpisodeNumber"))
                .List<CrossRef_AniDB_TraktV2>();

            return new List<CrossRef_AniDB_TraktV2>(xrefs);
        }

        public List<CrossRef_AniDB_TraktV2> GetByAnimeIDEpTypeEpNumber(ISession session, int id, int aniEpType,
            int aniEpisodeNumber)
        {
            var xrefs = session
                .CreateCriteria(typeof(CrossRef_AniDB_TraktV2))
                .Add(Restrictions.Eq("AnimeID", id))
                .Add(Restrictions.Eq("AniDBStartEpisodeType", aniEpType))
                .Add(Restrictions.Eq("AniDBStartEpisodeNumber", aniEpisodeNumber))
                .List<CrossRef_AniDB_TraktV2>();

            return new List<CrossRef_AniDB_TraktV2>(xrefs);
        }

        public CrossRef_AniDB_TraktV2 GetByTraktID(ISession session, string id, int season, int episodeNumber,
            int animeID,
            int aniEpType, int aniEpisodeNumber)
        {
            CrossRef_AniDB_TraktV2 cr = session
                .CreateCriteria(typeof(CrossRef_AniDB_TraktV2))
                .Add(Restrictions.Eq("TraktID", id))
                .Add(Restrictions.Eq("TraktSeasonNumber", season))
                .Add(Restrictions.Eq("TraktStartEpisodeNumber", episodeNumber))
                .Add(Restrictions.Eq("AnimeID", animeID))
                .Add(Restrictions.Eq("AniDBStartEpisodeType", aniEpType))
                .Add(Restrictions.Eq("AniDBStartEpisodeNumber", aniEpisodeNumber))
                .UniqueResult<CrossRef_AniDB_TraktV2>();
            return cr;
        }

        public CrossRef_AniDB_TraktV2 GetByTraktID(string id, int season, int episodeNumber, int animeID, int aniEpType,
            int aniEpisodeNumber)
        {
            using (var session = DatabaseFactory.SessionFactory.OpenSession())
            {
                return GetByTraktID(session, id, season, episodeNumber, animeID, aniEpType, aniEpisodeNumber);
            }
        }

        public List<CrossRef_AniDB_TraktV2> GetByTraktIDAndSeason(string traktID, int season)
        {
            using (var session = DatabaseFactory.SessionFactory.OpenSession())
            {
                var xrefs = session
                    .CreateCriteria(typeof(CrossRef_AniDB_TraktV2))
                    .Add(Restrictions.Eq("TraktID", traktID))
                    .Add(Restrictions.Eq("TraktSeasonNumber", season))
                    .List<CrossRef_AniDB_TraktV2>();

                return new List<CrossRef_AniDB_TraktV2>(xrefs);
            }
        }

        public List<CrossRef_AniDB_TraktV2> GetByTraktID(string traktID)
        {
            using (var session = DatabaseFactory.SessionFactory.OpenSession())
            {
                var xrefs = session
                    .CreateCriteria(typeof(CrossRef_AniDB_TraktV2))
                    .Add(Restrictions.Eq("TraktID", traktID))
                    .List<CrossRef_AniDB_TraktV2>();

                return new List<CrossRef_AniDB_TraktV2>(xrefs);
            }
        }
    }
}