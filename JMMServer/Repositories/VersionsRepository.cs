﻿using JMMServer.Entities;
using NHibernate.Criterion;

namespace JMMServer.Repositories
{
    public class VersionsRepository
    {
        public void Save(Versions obj)
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

        public Versions GetByID(int id)
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                return session.Get<Versions>(id);
            }
        }

        public Versions GetByVersionType(string vertype)
        {
            using (var session = JMMService.SessionFactory.OpenSession())
            {
                var cr = session
                    .CreateCriteria(typeof(Versions))
                    .Add(Restrictions.Eq("VersionType", vertype))
                    .UniqueResult<Versions>();
                return cr;
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