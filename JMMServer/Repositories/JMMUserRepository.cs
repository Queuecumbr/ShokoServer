﻿using System.Collections.Generic;
using JMMServer.Entities;
using NHibernate;
using NHibernate.Criterion;
using NLog;

namespace JMMServer.Repositories
{
    public class JMMUserRepository
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void Save(JMMUser obj)
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
            logger.Trace("Updating group filter stats by user from JMMUserRepository.Save: {0}", obj.JMMUserID);
            StatsCache.Instance.UpdateGroupFilterUsingUser(obj.JMMUserID);
        }

        public JMMUser GetByID(int id)
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                return GetByID(session, id);
            }
        }

        public JMMUser GetByID(ISession session, int id)
        {
            return session.Get<JMMUser>(id);
        }

        public List<JMMUser> GetAll()
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                return GetAll(session);
            }
        }

        public List<JMMUser> GetAll(ISession session)
        {
            var objs = session
                .CreateCriteria(typeof(JMMUser))
                .List<JMMUser>();

            return new List<JMMUser>(objs);
        }

        public List<JMMUser> GetAniDBUsers()
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var objs = session
                    .CreateCriteria(typeof(JMMUser))
                    .Add(Restrictions.Eq("IsAniDBUser", 1))
                    .List<JMMUser>();

                return new List<JMMUser>(objs);
            }
        }

        public List<JMMUser> GetTraktUsers()
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var objs = session
                    .CreateCriteria(typeof(JMMUser))
                    .Add(Restrictions.Eq("IsTraktUser", 1))
                    .List<JMMUser>();

                return new List<JMMUser>(objs);
            }
        }

        public JMMUser AuthenticateUser(string userName, string password)
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var hashedPassword = Digest.Hash(password);
                var cr = session
                    .CreateCriteria(typeof(JMMUser))
                    .Add(Restrictions.Eq("Username", userName))
                    .Add(Restrictions.Eq("Password", hashedPassword))
                    .UniqueResult<JMMUser>();
                return cr;
            }
        }

        public long GetTotalRecordCount()
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var count = session
                    .CreateCriteria(typeof(JMMUser))
                    .SetProjection(Projections.Count(Projections.Id())
                    )
                    .UniqueResult<int>();

                return count;
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