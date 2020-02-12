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
        public static SPUser GetLookUpUserValue(SPList originList, SPListItem item, string lookUpField, string originField)
        {
            var userLookUpField = item.Fields.GetField(lookUpField);
            var userFieldLookUpValue =
                userLookUpField.GetFieldValue(item[lookUpField].ToString()) as SPFieldLookupValue;
            var rootMentorField = originList.Fields.GetField(originField);
            var rootMentorItem = originList.GetItemById(userFieldLookUpValue.LookupId);
            var rootMentorFieldValue =
                rootMentorField.GetFieldValue(rootMentorItem[originField].ToString()) as SPFieldUserValue;

            return rootMentorFieldValue?.User;
        }

        public static SPUser GetUserValue(SPListItem item, string userField)
        {
            var field = item.Fields.GetField(userField);
            var fieldValue =
                field.GetFieldValue(item[userField].ToString()) as SPFieldUserValue;
            var user = fieldValue?.User;

            return user;
        }

        public static int GetIntValue(SPListItem item, string field)
        {
            var res = Convert.ToInt32(item[field]);

            return res;
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

        public static void SetMultiChoiceValue(SPListItem item, string multiChoiceField, IEnumerable<string> multiChoiceValue)
        {
            var value = new SPFieldMultiChoiceValue();
            foreach (var choiceValue in multiChoiceValue) value.Add(choiceValue);
            item[multiChoiceField] = value;
        }
    }
}
