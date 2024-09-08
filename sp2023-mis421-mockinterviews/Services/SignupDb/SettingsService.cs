using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Services.SignupDb
{
    public class SettingsService : GenericSignupDbService<Setting>
    {
        public SettingsService(ISignupDbContext context) : base(context)
        {
        }

        public async Task<Setting> GetSettingByName(string name, string defaultValue = "")
        {
            var setting = await _dbSet.FirstOrDefaultAsync(x => x.Name == name);
            
            if(setting == null && string.IsNullOrEmpty(defaultValue))
            {
                throw new Exception($"Setting '{name}' does not exist.");
            }

            return setting ?? new Setting { Name = name, Value = defaultValue };           
        }

        public async Task<int> GetIntegerSettingByName(string name, string defaultValue = "")
        {
            var setting = await GetSettingByName(name, defaultValue);

            if(int.TryParse(setting.Value, out int result))
            {
                return result;
            }

            if(int.TryParse(defaultValue, out int defaultResult))
            {
                return defaultResult;
            }

            throw new Exception($"Setting '{name}': Neither the setting value nor the default value are integers.");
        }

        public async Task<int> GetHoursInAdvance()
        {
            var value = await GetIntegerSettingByName(SettingsConstants.InterviewIndexHours.Name, SettingsConstants.InterviewIndexHours.DefaultValue);

            if(value <= 0)
            {
                throw new Exception($"Setting '{SettingsConstants.InterviewIndexHours.Name}' cannot be negative or 0.");
            }

            return value;
        }

        public async Task<int> GetMaximumTimeslotSignups()
        {
            var value =  await GetIntegerSettingByName(SettingsConstants.MaximumTimeslotSignups.Name, SettingsConstants.MaximumTimeslotSignups.DefaultValue);

            if(value <= 0)
            {
                throw new Exception($"Setting '{SettingsConstants.MaximumTimeslotSignups.Name}' cannot be negative or 0.");
            } 

            return value;
        }
    }
}