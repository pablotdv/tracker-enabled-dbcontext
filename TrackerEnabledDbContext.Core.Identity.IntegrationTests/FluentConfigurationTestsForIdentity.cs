﻿using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrackerEnabledDbContext.Common.Configuration;
using TrackerEnabledDbContext.Common.Testing;
using TrackerEnabledDbContext.Common.Testing.Extensions;
using TrackerEnabledDbContext.Common.Testing.Models;

namespace TrackerEnabledDbContext.Identity.IntegrationTests
{
    [TestClass]
    public class FluentConfigurationTestsForIdentity : PersistanceTests<TestTrackerIdentityContext>
    {
        public FluentConfigurationTestsForIdentity()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestTrackerIdentityContext>();
            optionsBuilder.UseInMemoryDatabase("TestTrackerContext");
            var options = optionsBuilder.Options;
            Db = new TestTrackerIdentityContext(options);
            ObjectFactory = new Common.Testing.Code.ObjectFactory<TestTrackerIdentityContext>(Db);
        }

        [TestMethod]
        public void Can_recognise_global_tracking_indicator_when_disabled()
        {
            GlobalTrackingConfig.Enabled = false;
            EntityTracker
                .TrackAllProperties<POCO>();

            POCO model = ObjectFactory.Create<POCO>(false, true, Db);

            model.AssertNoLogs(Db, model.Id);
        }

        [TestMethod]
        public void Can_recognise_global_tracking_indicator_when_enabled()
        {
            EntityTracker.TrackAllProperties<POCO>();

            POCO model = new POCO
            {
                Color = "Red",
                Height = 67.4,
                StartTime = new DateTime(2015, 5, 5)
            };

            Db.POCOs.Add(model);
            Db.SaveChanges();

            model.AssertAuditForAddition(Db, model.Id, null,
                x=>x.Color,
                x=>x.Id,
                x=>x.Height,
                x=>x.StartTime);
        }

        [TestMethod]
        public async Task Can_Override_annotation_based_configuration_for_entity_skipTracking()
        {
            var model = new NormalModel();
            EntityTracker
                .OverrideTracking<NormalModel>()
                .Disable();

            string userName = RandomText;

            Db.NormalModels.Add(model);
            await Db.SaveChangesAsync(userName);

            model.AssertNoLogs(Db, model.Id);
        }

        [TestMethod]
        public void Can_Override_annotation_based_configuration_for_property()
        {
            var model = new TrackedModelWithMultipleProperties
            {
                Category = RandomChar, //tracked ->skipped
                Description = RandomText, //skipped
                IsSpecial = true, //tracked -> skipped
                StartDate = new DateTime(2015, 5, 5), //skipped
                Name = RandomText, //tracked
                Value = RandomNumber //skipped -> Tracked
            };

            EntityTracker
                .OverrideTracking<TrackedModelWithMultipleProperties>()
                //enable tracking for value
                .Enable(x => x.Value)
                //disable for isSpecial
                .Disable(x => x.IsSpecial)
                .Disable(x => x.Category);

            Db.TrackedModelsWithMultipleProperties.Add(model);

            string userName = RandomText;

            Db.SaveChanges(userName);

            model.AssertAuditForAddition(Db, model.Id, userName,
                x => x.Id,
                x => x.Name,
                x => x.Value);
        }

        //TODO: can track CHAR properties ? NO
    }
}
