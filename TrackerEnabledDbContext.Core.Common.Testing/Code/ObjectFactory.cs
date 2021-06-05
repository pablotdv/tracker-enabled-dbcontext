using System;

namespace TrackerEnabledDbContext.Common.Testing.Code
{
    public class ObjectFactory<TContext>
        where TContext : class, ITestDbContext
    {
        readonly ObjectFiller _filler = new ObjectFiller();

        private readonly TContext _db;

        public ObjectFactory(TContext db)
        {
            _filler.IgnorePropertiesWhen(propName =>
                propName.EndsWith("Id") ||
                propName == "IsDeleted");
            _db = db;
        }

        public TEntity Create<TEntity>
            (bool fill = true, bool save = false, ITestDbContext testDbContext = null)
            where TEntity : class
        {
            var instance = Activator.CreateInstance<TEntity>();

            if (fill)
            {
                _filler.Fill(instance);
            }

            if (save)
            {
                if (testDbContext == null)
                {
                    _db.Set<TEntity>().Add(instance);
                    _db.SaveChanges();
                }
                else
                {
                    testDbContext.Set<TEntity>().Add(instance);
                    testDbContext.SaveChanges();
                }
            }

            return instance;
        }
    }
}