using System;
using System.Collections.Generic;
using System.Linq;

using MediaBrowser.Model.Plugins;

namespace EmbyMyShowsMe.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public List<UserConfig> Users { get; set; } = new List<UserConfig>();


        public UserConfig GetUserById(string id)
        {
            var gid = id.Replace("-", "");
            return Users.FirstOrDefault(c => c.Id == gid);
        }

        public UserConfig GetUserByGuid(Guid id)
        {
            var gid = id.ToString().Replace("-", "");
            return Users.FirstOrDefault(c => c.Id == gid);
        }

        public void AddUser(UserConfig user)
        {
            var index = Users.FindIndex(u => u.Id == user.Id);
            if (index == -1)
            {
                Users.Add(user);
            }
            else
            {
                Users[index] = user;
            }
            Plugin.Instance.SaveConfiguration();
        }

        public void RemoveUser(UserConfig user)
        {
            if (Users.Contains(user))
            {
                Users.Remove(user);
                Plugin.Instance.SaveConfiguration();
            }
        }
    }
}
