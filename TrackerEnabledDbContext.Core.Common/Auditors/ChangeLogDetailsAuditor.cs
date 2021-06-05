﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using TrackerEnabledDbContext.Common.Auditors.Comparators;
using TrackerEnabledDbContext.Common.Auditors.Helpers;
using TrackerEnabledDbContext.Common.Configuration;
using TrackerEnabledDbContext.Common.Extensions;
using TrackerEnabledDbContext.Common.Interfaces;
using TrackerEnabledDbContext.Common.Models;

namespace TrackerEnabledDbContext.Common.Auditors
{
    public class ChangeLogDetailsAuditor : ILogDetailsAuditor
    {
        protected readonly EntityEntry DbEntry;
        private readonly AuditLog _log;
        private readonly DbEntryValuesWrapper _dbEntryValuesWrapper;

        public ChangeLogDetailsAuditor(EntityEntry dbEntry, AuditLog log)
        {
            DbEntry = dbEntry;
            _log = log;
            _dbEntryValuesWrapper = new DbEntryValuesWrapper(dbEntry);
        }

        public IEnumerable<AuditLogDetail> CreateLogDetails()
        {
            Type entityType = DbEntry.Entity.GetType().GetEntityType();

            foreach (string propertyName in PropertyNamesOfEntity())
            {
                if (PropertyTrackingConfiguration.IsTrackingEnabled(
                    new PropertyConfiguerationKey(propertyName, entityType.FullName), entityType)
                    && IsValueChanged(propertyName))
                {
                    if (IsComplexType(propertyName))
                    {
                        foreach (var auditLogDetail in CreateComplexTypeLogDetails(propertyName))
                        {
                            yield return auditLogDetail;
                        }
                    }
                    else
                    {
                        yield return new AuditLogDetail
                        {
                            PropertyName = propertyName,
                            OriginalValue = OriginalValue(propertyName)?.ToString(),
                            NewValue = CurrentValue(propertyName)?.ToString(),
                            Log = _log
                        };
                    }
                }
            }
        }

        protected internal virtual EntityState StateOfEntity()
        {
            return DbEntry.State;
        }

        private IEnumerable<string> PropertyNamesOfEntity()
        {
            var propertyValues = (StateOfEntity() == EntityState.Added)
                ? DbEntry.CurrentValues
                : DbEntry.OriginalValues;
            return propertyValues.Properties.Where(x => x.PropertyInfo != null).Select(x => x.PropertyInfo.Name);
        }

        protected virtual bool IsValueChanged(string propertyName)
        {
            if (IsComplexType(propertyName))
            {
                var prop = DbEntry.Reference(propertyName);
                var propertyType = DbEntry.Entity.GetType().GetProperty(propertyName).PropertyType;

                object originalValue = OriginalValue(propertyName);

                Comparator comparator = ComparatorFactory.GetComparator(propertyType);

                var changed = (StateOfEntity() == EntityState.Modified
                    && prop.IsModified && !comparator.AreEqual(CurrentValue(propertyName), originalValue));
                return changed;
            }
            else
            {
                var prop = DbEntry.Property(propertyName);
                var propertyType = DbEntry.Entity.GetType().GetProperty(propertyName).PropertyType;

                object originalValue = OriginalValue(propertyName);

                Comparator comparator = ComparatorFactory.GetComparator(propertyType);

                var changed = (StateOfEntity() == EntityState.Modified
                    && prop.IsModified && !comparator.AreEqual(CurrentValue(propertyName), originalValue));
                return changed;
            }
        }

        protected virtual object OriginalValue(string propertyName)
        {
            return _dbEntryValuesWrapper.OriginalValue(propertyName);
        }

        protected virtual object CurrentValue(string propertyName)
        {
            var value = DbEntry.Property(propertyName).CurrentValue;
            return value;
        }

        private bool IsComplexType(string propertyName)
        {
            var entryMember = DbEntry.Member(propertyName) as ReferenceEntry;

            return entryMember != null;
        }

        private IEnumerable<AuditLogDetail> CreateComplexTypeLogDetails(string propertyName)
        {
            var entryMember = DbEntry.Member(propertyName) as ReferenceEntry;

            if (entryMember != null)
            {
                var complexTypeObj = entryMember.CurrentValue.GetType();

                foreach (var pi in complexTypeObj.GetProperties())
                {
                    var complexTypePropertyName = $"{propertyName}_{pi.Name}";
                    var complexTypeOrigValue = OriginalValue(propertyName);
                    var complexTypeNewValue = CurrentValue(propertyName);

                    var origValue = complexTypeOrigValue == null ? null : pi.GetValue(complexTypeOrigValue);
                    var newValue = complexTypeNewValue == null ? null : pi.GetValue(complexTypeNewValue);

                    Comparator comparator = ComparatorFactory.GetComparator(complexTypeObj);

                    if (!comparator.AreEqual(newValue, origValue))
                    {
                        yield return new AuditLogDetail
                        {
                            PropertyName = complexTypePropertyName,
                            OriginalValue = origValue?.ToString(),
                            NewValue = newValue?.ToString(),
                            Log = _log
                        };
                    }
                }
            }
        }

    }
}