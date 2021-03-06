﻿using Cadena.Data;

namespace StarryEyes.Models.Timelines.Statuses
{
    public class UserModel
    {
        public static UserModel Get(TwitterUser user)
        {
            return new UserModel(user);
        }

        private UserModel(TwitterUser user)
        {
            User = user;
        }

        public TwitterUser User { get; }
    }
}