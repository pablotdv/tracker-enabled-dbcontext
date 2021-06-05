using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackerEnabledDbContext.Common.Configuration
{
    //https://stackoverflow.com/questions/2958921/entity-framework-4-how-to-find-the-primary-key
    internal static class DbContextExtensions
    {
        public static IEnumerable<PropertyConfiguerationKey> GetKeyNames<TEntity>(this DbContext context)
            where TEntity : class
        {
            return context.GetKeyNames(typeof(TEntity));
        }

        public static IEnumerable<PropertyConfiguerationKey> GetKeyNames(this DbContext context, Type entity)
        {
            var entityType = context.Model.FindEntityType(entity);
            return entityType
                .GetKeys()
                .SelectMany(x=>x.Properties)
                .Select(x => new PropertyConfiguerationKey(x.Name, entity.FullName));
        }
    }
}
