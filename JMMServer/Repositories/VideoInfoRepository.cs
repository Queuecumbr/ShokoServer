﻿using JMMServer.Entities;
using NHibernate.Criterion;

namespace JMMServer.Repositories
{
    public class VideoInfoRepository
    {
        public void Save(VideoInfo obj)
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                // populate the database
                using (var transaction = session.BeginTransaction())
                {
                    session.SaveOrUpdate(obj);
                    transaction.Commit();
                }
            }
        }

        public VideoInfo GetByID(int id)
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                return session.Get<VideoInfo>(id);
            }
        }

        public VideoInfo GetByHash(string hash)
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var obj = session
                    .CreateCriteria(typeof(VideoInfo))
                    .Add(Restrictions.Eq("Hash", hash))
                    .UniqueResult<VideoInfo>();

                return obj;
            }
        }


        public void Delete(int id)
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                // populate the database
                using (var transaction = session.BeginTransaction())
                {
                    var cr = GetByID(id);
                    if (cr != null)
                    {
                        session.Delete(cr);
                        transaction.Commit();
                    }
                }
            }
        }
    }
}