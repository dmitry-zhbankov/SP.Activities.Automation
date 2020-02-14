using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Test.Activities.Automation.ActivityLib.Models.Activity.Classes;

namespace Test.Activities.Automation.ActivityLib.Models.Activity.Services.Tests
{
    [TestClass()]
    public class EnsureServiceTests
    {
        [TestMethod()]
        public void CheckActivityDateTest()
        {
            var ensureService = new EnsureService(null);
            var maxDate = DateTime.Now.Date;
            var minDate = DateTime.Now.AddMonths(-1).Date;

            IEnumerable<ActivityInfo> actualActivities = new List<ActivityInfo>()
            {
                new ActivityInfo()
                {
                    Activity="Activity 1",
                    Date=minDate.AddDays(-1),
                },
                new ActivityInfo()
                {
                    Activity="Activity 2",
                    Date=maxDate
                },
                new ActivityInfo()
                {
                    Activity="Activity 3",
                    Date=maxDate.AddDays(1)
                },
                new ActivityInfo()
                {
                    Activity="Activity 4",
                    Date=maxDate.AddDays(-1)
                },
                new ActivityInfo()
                {
                    Activity="Activity 5",
                    Date=minDate
                },
                new ActivityInfo()
                {
                    Activity="Activity 6",
                    Date=minDate.AddDays(1)
                }
            };

            ensureService.CheckActivityDate(ref actualActivities, minDate, maxDate);

            var expectedActivities = new List<ActivityInfo>()
            {
                new ActivityInfo()
                {
                    Activity="Activity 2",
                    Date=maxDate
                },
                new ActivityInfo()
                {
                    Activity="Activity 4",
                    Date=maxDate.AddDays(-1)
                },
                new ActivityInfo()
                {
                    Activity="Activity 5",
                    Date=minDate
                },
                new ActivityInfo()
                {
                    Activity="Activity 6",
                    Date=minDate.AddDays(1)
                }
            };

            CollectionAssert.AreEqual(expectedActivities, actualActivities.ToList());
        }

        [TestMethod()]
        public void CheckActivityPathsTest()
        {
            var ensureService = new EnsureService(null);

            IEnumerable<ActivityInfo> actualActivities = new List<ActivityInfo>()
            {
                new ActivityInfo()
                {
                    UserId = 1,
                    Activity="Activity 1",
                    Paths = new List<string>()
                    {
                        "Path 1",
                    }
                },
                new ActivityInfo()
                {
                    UserId = 2,
                    Activity="Activity 2",
                    Paths = new List<string>()
                    {
                        "Path 1",
                        "Path 2"
                    }
                },
                new ActivityInfo()
                {
                    UserId = 3,
                    Activity="Activity 1",
                },
            };

            var members = new List<Member>()
            {
                new Member()
                {
                    MentorId=1
                },
                new Member()
                {
                    RootMentorId= 2
                },
                new Member()
                {
                    MentorId = 3,
                    Paths=new List<string>()
                    {
                        "Path 1",
                        "Path 3"
                    }
                },
            };

            var expectedActivities = new List<ActivityInfo>()
            {
                new ActivityInfo()
                {
                    UserId = 1,
                    Activity="Activity 1",
                    Paths = new List<string>()
                    {
                        "Path 1",
                    }
                },
                new ActivityInfo()
                {
                    UserId = 2,
                    Activity="Activity 2",
                    Paths = new List<string>()
                    {
                        "Path 1",
                        "Path 2"
                    }
                },
                new ActivityInfo()
                {
                    UserId = 3,
                    Activity="Activity 1",
                    Paths=new List<string>()
                    {
                        "Path 1",
                        "Path 3"
                    }
                },
            };

            ensureService.CheckActivityPaths(ref actualActivities, members);

            CollectionAssert.AreEqual(actualActivities.ToList(), expectedActivities);
        }

        [TestMethod()]
        public void CheckActivityUserTest()
        {
            var ensureService = new EnsureService(null);

            IEnumerable<ActivityInfo> actualActivities = new List<ActivityInfo>()
            {
                new ActivityInfo()
                {
                    UserId = 1,
                    Activity="Activity 1",
                },
                new ActivityInfo()
                {
                    UserId = 2,
                    Activity="Activity 2",
                },
                new ActivityInfo()
                {
                    UserId = 3,
                    Activity="Activity 3",
                },
                new ActivityInfo()
                {
                    UserId = 4,
                    Activity="Activity 4",
                },
                new ActivityInfo()
                {
                    UserId = 5,
                    Activity="Activity 5",
                },
            };

            var members = new List<Member>()
            {
                new Member()
                {
                    MentorId = 1
                },
                new Member()
                {
                    RootMentorId= 2
                },
                new Member()
                {
                    MentorId = 3,
                    RootMentorId = 3
                },
            };

            var expectedActivities = new List<ActivityInfo>()
            {
                new ActivityInfo()
                {
                    UserId = 1,
                    Activity="Activity 1",
                },
                new ActivityInfo()
                {
                    UserId = 2,
                    Activity="Activity 2",
                },
                new ActivityInfo()
                {
                    UserId = 3,
                    Activity="Activity 3",
                },
            };

            ensureService.CheckActivityUser(ref actualActivities, members);

            CollectionAssert.AreEqual(actualActivities.ToList(), expectedActivities);
        }

        [TestMethod()]
        public void EnsureTest()
        {
            
        } 
    }
}
