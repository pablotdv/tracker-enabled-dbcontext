using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrackerEnabledDbContext.Common;
using TrackerEnabledDbContext.Common.Configuration;
using TrackerEnabledDbContext.Common.EventArgs;
using TrackerEnabledDbContext.Common.Interfaces;
using TrackerEnabledDbContext.Common.Models;

namespace TrackerEnabledDbContext.Core
{
    public class TrackerContext : DbContext, ITrackerContext
    {
        private readonly CoreTracker _coreTracker;

        private Func<string> _usernameFactory;
        private string _defaultUsername;
        private Action<dynamic> _metadataConfiguration;


        private bool _trackingEnabled = true;

        public bool TrackingEnabled
        {
            get
            {
                return GlobalTrackingConfig.Enabled && _trackingEnabled;
            }
            set
            {
                _trackingEnabled = value;
            }
        }

        public virtual void ConfigureUsername(Func<string> usernameFactory)
        {
            _usernameFactory = usernameFactory;
        }

        public virtual void ConfigureUsername(string defaultUsername)
        {
            _defaultUsername = defaultUsername;
        }

        public virtual void ConfigureMetadata(Action<dynamic> metadataConfiguration)
        {
            _metadataConfiguration = metadataConfiguration;
        }

        public TrackerContext([NotNullAttribute] DbContextOptions options) : base(options)
        {
            _coreTracker = new CoreTracker(this);
        }

        protected TrackerContext()
        {
        }

        public virtual event EventHandler<AuditLogGeneratedEventArgs> OnAuditLogGenerated
        {
            add { _coreTracker.OnAuditLogGenerated += value; }
            remove { _coreTracker.OnAuditLogGenerated -= value; }
        }

        public virtual DbSet<AuditLog> AuditLog { get; set; }

        public virtual DbSet<AuditLogDetail> LogDetails { get; set; }

        /// <summary>
        ///     This method saves the model changes to the database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table.
        ///     Always use this method instead of SaveChanges() whenever possible.
        /// </summary>
        /// <param name="userName">Username of the logged in identity</param>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public virtual int SaveChanges(object userName)
        {
            if (!TrackingEnabled) return base.SaveChanges();

            dynamic metaData = new ExpandoObject();
            _metadataConfiguration?.Invoke(metaData);

            _coreTracker.AuditChanges(userName, metaData);

            IEnumerable<EntityEntry> addedEntries = _coreTracker.GetAdditions();
            // Call the original SaveChanges(), which will save both the changes made and the audit records...Note that added entry auditing is still remaining.
            int result = base.SaveChanges();
            //By now., we have got the primary keys of added entries of added entiries because of the call to savechanges.

            _coreTracker.AuditAdditions(userName, addedEntries, metaData);

            //save changes to audit of added entries
            base.SaveChanges();
            return result;
        }

        /// <summary>
        ///     This method saves the model changes to the database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table.
        /// </summary>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public override int SaveChanges()
        {
            if (!TrackingEnabled) return base.SaveChanges();

            return SaveChanges(_usernameFactory?.Invoke() ?? _defaultUsername);
        }

        /// <summary>
        ///     Get all logs for the given model type
        /// </summary>
        /// <typeparam name="TEntity">Type of domain model</typeparam>
        /// <returns></returns>
        public virtual IQueryable<AuditLog> GetLogs<TEntity>()
        {
            return _coreTracker.GetLogs<TEntity>();
        }

        /// <summary>
        ///     Get all logs for the given entity name
        /// </summary>
        /// <param name="entityFullName">full name of entity</param>
        /// <returns></returns>
        public virtual IQueryable<AuditLog> GetLogs(string entityFullName)
        {
            return _coreTracker.GetLogs(entityFullName);
        }

        /// <summary>
        ///     Get all logs for the given model type for a specific record
        /// </summary>
        /// <typeparam name="TEntity">Type of domain model</typeparam>
        /// <param name="primaryKey">primary key of record</param>
        /// <returns></returns>
        public virtual IQueryable<AuditLog> GetLogs<TEntity>(object primaryKey)
        {
            return _coreTracker.GetLogs<TEntity>(primaryKey);
        }

        /// <summary>
        ///     Get all logs for the given entity name for a specific record
        /// </summary>
        /// <param name="entityFullName">full name of entity</param>
        /// <param name="primaryKey">primary key of record</param>
        /// <returns></returns>
        public virtual IQueryable<AuditLog> GetLogs(string entityFullName, object primaryKey)
        {
            return _coreTracker.GetLogs(entityFullName, primaryKey);
        }

        #region -- Async --

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table.
        /// </summary>
        /// <param name="userName">Username of the logged in identity</param>
        /// <param name="cancellationToken">
        ///     A System.Threading.CancellationToken to observe while waiting for the task
        ///     to complete.
        /// </param>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public virtual async Task<int> SaveChangesAsync(object userName, CancellationToken cancellationToken)
        {
            if (!TrackingEnabled) return await base.SaveChangesAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            dynamic metadata = new ExpandoObject();
            _metadataConfiguration?.Invoke(metadata);

            _coreTracker.AuditChanges(userName, metadata);

            IEnumerable<EntityEntry> addedEntries = _coreTracker.GetAdditions();

            // Call the original SaveChanges(), which will save both the changes made and the audit records...Note that added entry auditing is still remaining.
            int result = await base.SaveChangesAsync(cancellationToken);

            //By now., we have got the primary keys of added entries of added entiries because of the call to savechanges.
            _coreTracker.AuditAdditions(userName, addedEntries, metadata);

            //save changes to audit of added entries
            await base.SaveChangesAsync(cancellationToken);

            return result;
        }

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table.
        ///     Always use this method instead of SaveChangesAsync() whenever possible.
        /// </summary>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public virtual async Task<int> SaveChangesAsync(int userId)
        {
            if (!TrackingEnabled) return await base.SaveChangesAsync(CancellationToken.None);

            return await SaveChangesAsync(userId, CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table.
        ///     Always use this method instead of SaveChangesAsync() whenever possible.
        /// </summary>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public virtual async Task<int> SaveChangesAsync(string userName)
        {
            if (!TrackingEnabled) return await base.SaveChangesAsync(CancellationToken.None);

            return await SaveChangesAsync(userName, CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously saves all changes made in this context to the underlying database.
        ///     If the tracker for an entity is active, it will also put the old values in tracking table with null UserName.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A System.Threading.CancellationToken to observe while waiting for the task
        ///     to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous save operation.  The task result
        ///     contains the number of objects written to the underlying database.
        /// </returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!TrackingEnabled) return await base.SaveChangesAsync(cancellationToken);

            return await SaveChangesAsync(_usernameFactory?.Invoke() ?? _defaultUsername, cancellationToken);
        }

        #endregion
    }
}