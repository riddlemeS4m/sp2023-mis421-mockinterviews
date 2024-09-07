using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Data.Access
{
    public class GlobalConfigVarSeed
    {
        private static readonly Dictionary<string, string> DefaultValues = new()
        {
            { "zoom_link", "https://mockinterviews.uamishub.com" },
            { "zoom_link_visible", "0" },
            { "disruption_banner", "0" },
            { "interview_index_hours", "3" }
        };

        public static List<Setting> SeedGlobalConfigVars(Dictionary<string, Setting> existingConfigVars)
        {
            var list = new List<Setting>();
            var missingConfigVarNames = DefaultValues.Keys.Except(existingConfigVars.Keys);

            foreach (var missingConfigVarName in missingConfigVarNames)
            {
                var configVar = new Setting
                {
                    Name = missingConfigVarName,
                    Value = DefaultValues[missingConfigVarName]
                };

                list.Add(configVar);
            }

            return list;
        }
    }
}
