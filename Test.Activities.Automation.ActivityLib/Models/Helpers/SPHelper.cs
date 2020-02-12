using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;

namespace Test.Activities.Automation.ActivityLib.Models.Helpers
{
    public static class SPHelper
    {
        public static SPUser GetLookUpUser(SPList originList, SPListItem item, string lookUpField, string originField)
        {
            var userLookUpField = item.Fields.GetField(lookUpField);
            var userFieldLookUpValue =
                userLookUpField.GetFieldValue(item[lookUpField].ToString()) as SPFieldLookupValue;
            var rootMentorField = originList.Fields.GetField(originField);
            var rootMentorItem = originList.GetItemById(userFieldLookUpValue.LookupId);
            var rootMentorFieldValue =
                rootMentorField.GetFieldValue(rootMentorItem[originField].ToString()) as SPFieldUserValue;

            return rootMentorFieldValue.User;
        }

        public static IEnumerable<string> GetMultiChoiceValue(SPListItem item, string fieldName)
        {
            var field = item.Fields.GetField(fieldName);
            var fieldValue =
                field.GetFieldValue(item[fieldName].ToString()) as
                    SPFieldMultiChoiceValue;
            
            var res = new List<string>();
            for (int i = 0; i < fieldValue.Count; i++)
            {
                res.Add(fieldValue[i]);
            }

            return res;
        }
    }
}
