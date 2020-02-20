using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Test.Activities.Automation.ActivityLib.Services;

namespace Test.Activities.Automation.ActivityLib.Models.Activity.Services.Tests
{
    [TestClass()]
    public class EnsureServiceTests
    {
        //[TestMethod()]
        //public void CheckActivityDateTest()
        //{
        //    var ensureService = new EnsureService(null);
        //    var maxDate = DateTime.Now.Date;
        //    var minDate = DateTime.Now.AddMonths(-1).Date;

        //    IEnumerable<ActivityInfo> actualActivities = new List<ActivityInfo>()
        //    {
        //        new ActivityInfo()
        //        {
        //            Activity = "Activity 1",
        //            Date = minDate.AddDays(-1),
        //        },
        //        new ActivityInfo()
        //        {
        //            Activity = "Activity 2",
        //            Date = maxDate
        //        },
        //        new ActivityInfo()
        //        {
        //            Activity = "Activity 3",
        //            Date = maxDate.AddDays(1)
        //        },
        //        new ActivityInfo()
        //        {
        //            Activity = "Activity 4",
        //            Date = maxDate.AddDays(-1)
        //        },
        //        new ActivityInfo()
        //        {
        //            Activity = "Activity 5",
        //            Date = minDate
        //        },
        //        new ActivityInfo()
        //        {
        //            Activity = "Activity 6",
        //            Date = minDate.AddDays(1)
        //        }
        //    };

        //    ensureService.CheckActivityDate(ref actualActivities, minDate, maxDate);

        //    var expectedActivities = new List<ActivityInfo>()
        //    {
        //        new ActivityInfo()
        //        {
        //            Activity = "Activity 2",
        //            Date = maxDate
        //        },
        //        new ActivityInfo()
        //        {
        //            Activity = "Activity 4",
        //            Date = maxDate.AddDays(-1)
        //        },
        //        new ActivityInfo()
        //        {
        //            Activity = "Activity 5",
        //            Date = minDate
        //        },
        //        new ActivityInfo()
        //        {
        //            Activity = "Activity 6",
        //            Date = minDate.AddDays(1)
        //        }
        //    };

        //    CollectionAssert.AreEqual(expectedActivities, actualActivities.ToList());
        //}

        //[TestMethod()]
        //public void CheckActivityPathsTest()
        //{
        //    var ensureService = new ActivityLib.Services.EnsureService.EnsureService(null);

        //    IEnumerable<ActivityInfo> actualActivities = new List<ActivityInfo>()
        //    {
        //        new ActivityInfo()
        //        {
        //            UserId = 1,
        //            Activity = "Activity 1",
        //            Paths = new List<string>()
        //            {
        //                "Path 1",
        //            }
        //        },
        //        new ActivityInfo()
        //        {
        //            UserId = 2,
        //            Activity = "Activity 2",
        //            Paths = new List<string>()
        //            {
        //                "Path 1",
        //                "Path 2"
        //            }
        //        },
        //        new ActivityInfo()
        //        {
        //            UserId = 3,
        //            Activity = "Activity 1",
        //        },
        //    };

        //    var members = new List<Member>()
        //    {
        //        new Member()
        //        {
        //            MentorId = 1
        //        },
        //        new Member()
        //        {
        //            RootMentorId = 2
        //        },
        //        new Member()
        //        {
        //            MentorId = 3,
        //            Paths = new List<string>()
        //            {
        //                "Path 1",
        //                "Path 3"
        //            }
        //        },
        //    };

        //    var expectedActivities = new List<ActivityInfo>()
        //    {
        //        new ActivityInfo()
        //        {
        //            UserId = 1,
        //            Activity = "Activity 1",
        //            Paths = new List<string>()
        //            {
        //                "Path 1",
        //            }
        //        },
        //        new ActivityInfo()
        //        {
        //            UserId = 2,
        //            Activity = "Activity 2",
        //            Paths = new List<string>()
        //            {
        //                "Path 1",
        //                "Path 2"
        //            }
        //        },
        //        new ActivityInfo()
        //        {
        //            UserId = 3,
        //            Activity = "Activity 1",
        //            Paths = new List<string>()
        //            {
        //                "Path 1",
        //                "Path 3"
        //            }
        //        },
        //    };

        //    ensureService.CheckActivityPaths(ref actualActivities, members);

        //    CollectionAssert.AreEqual(actualActivities.ToList(), expectedActivities);
        //}

        //[TestMethod()]
        //public void CheckActivityUserTest()
        //{
        //    var ensureService = new ActivityLib.Services.EnsureService.EnsureService(null);

        //    IEnumerable<ActivityInfo> actualActivities = new List<ActivityInfo>()
        //    {
        //        new ActivityInfo()
        //        {
        //            UserId = 1,
        //            Activity = "Activity 1",
        //        },
        //        new ActivityInfo()
        //        {
        //            UserId = 2,
        //            Activity = "Activity 2",
        //        },
        //        new ActivityInfo()
        //        {
        //            UserId = 3,
        //            Activity = "Activity 3",
        //        },
        //        new ActivityInfo()
        //        {
        //            UserId = 4,
        //            Activity = "Activity 4",
        //        },
        //        new ActivityInfo()
        //        {
        //            UserId = 5,
        //            Activity = "Activity 5",
        //        },
        //    };

        //    var members = new List<Member>()
        //    {
        //        new Member()
        //        {
        //            MentorId = 1
        //        },
        //        new Member()
        //        {
        //            RootMentorId = 2
        //        },
        //        new Member()
        //        {
        //            MentorId = 3,
        //            RootMentorId = 3
        //        },
        //    };

        //    var expectedActivities = new List<ActivityInfo>()
        //    {
        //        new ActivityInfo()
        //        {
        //            UserId = 1,
        //            Activity = "Activity 1",
        //        },
        //        new ActivityInfo()
        //        {
        //            UserId = 2,
        //            Activity = "Activity 2",
        //        },
        //        new ActivityInfo()
        //        {
        //            UserId = 3,
        //            Activity = "Activity 3",
        //        },
        //    };

        //    ensureService.CheckActivityUser(ref actualActivities, members);

        //    CollectionAssert.AreEqual(actualActivities.ToList(), expectedActivities);
        //}

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
