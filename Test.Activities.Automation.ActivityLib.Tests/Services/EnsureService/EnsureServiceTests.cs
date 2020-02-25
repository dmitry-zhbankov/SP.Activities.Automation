using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Activities.Automation.ActivityLib.Models;

namespace Test.Activities.Automation.ActivityLib.Tests.Services.EnsureService
{
    [TestClass()]
    public class EnsureServiceTests
    {
        [TestMethod()]
        public void EnsureTest()
        {
            var ensureService = new ActivityLib.Services.EnsureService(null);
            
            var members = new List<SpMember>()
            {
                new SpMember()
                {
                    UserId = 1,
                    MentorLookupId = 1,
                },
                new SpMember()
                {
                    UserId = 2,
                    RootMentorLookupId = 2,
                    MentorLookupId = 2,
                },
                new SpMember()
                {
                    UserId = 3,
                    RootMentorLookupId = 3,
                },
            };

            var spActivities = new List<SpActivity>()
            {
                new SpActivity()
                {
                    SpMember = members[0],
                    Activities = new List<string>()
                    {
                        "Mentoring"
                    },
                    Year = 2020,
                    Month = 1,
                    Paths = new List<string>()
                    {
                        "Path 1",
                        "Path 2",
                    }
                },
                new SpActivity()
                {
                    SpMember = members[1],
                    Activities = new List<string>()
                    {
                        "Mentoring",
                        "Root Mentoring"
                    },
                    Year = 2020,
                    Month = 1,
                    Paths = new List<string>()
                    {
                        "Path 3",
                    }
                },
                new SpActivity()
                {
                    SpMember = members[1],
                    Activities = new List<string>()
                    {
                        "Development"
                    },
                    Year = 2020,
                    Month = 2,
                    Paths = new List<string>()
                    {
                        "Path 4",
                    }
                },
            };

            var activities = new List<ActivityInfo>()
            {
                new ActivityInfo()
                {
                    UserId = 1,
                    Activity = "Development",
                    Paths = new List<string>()
                    {
                        "Path 3"
                    },
                    Date = new DateTime(2020, 1, 1)
                },
                new ActivityInfo()
                {
                    UserId = 2,
                    Activity = "Development",
                    Paths = new List<string>()
                    {
                        "Path 1"
                    },
                    Date = new DateTime(2020, 2, 1)
                },
                new ActivityInfo()
                {
                    UserId = 3,
                    Activity = "Development",
                    Paths = new List<string>()
                    {
                        "Path 3"
                    },
                    Date = new DateTime(2020, 2, 1)
                },
            };

            var actualSpActivities = ensureService.Ensure(spActivities, activities, members);

            var expectedSpActivite = new List<SpActivity>()
            {
                new SpActivity()
                {
                    SpMember = members[0],
                    Activities = new List<string>()
                    {
                        "Development",
                        "Mentoring",
                    },
                    Year = 2020,
                    Month = 1,
                    Paths = new List<string>()
                    {
                        "Path 1",
                        "Path 2",
                        "Path 3",
                    }
                },
                new SpActivity()
                {
                    SpMember = members[1],
                    Activities = new List<string>()
                    {
                        "Development"
                    },
                    Year = 2020,
                    Month = 2,
                    Paths = new List<string>()
                    {
                        "Path 1",
                        "Path 4",
                    }
                },
                new SpActivity()
                {
                    SpMember = members[2],
                    Activities = new List<string>()
                    {
                        "Development"
                    },
                    Year = 2020,
                    Month = 2,
                    Paths = new List<string>()
                    {
                        "Path 3",
                    }
                },
            };

            CollectionAssert.AreEqual(actualSpActivities.ToList(), expectedSpActivite);
        }
    }
}
