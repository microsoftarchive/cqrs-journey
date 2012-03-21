using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common;
using Registration.ReadModel;

namespace Conference.Web.Public
{
    public class OrmViewRepositoryProxy : IViewRepository
    {
        public T Find<T>(Guid id) where T : class
        {
            using (var repo = new OrmViewRepository())
                return repo.Find<T>(id);
        }

        public IQueryable<T> Query<T>() where T : class
        {
            using (var repo = new OrmViewRepository())
                return repo.Query<T>();
        }
    }
}