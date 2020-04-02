﻿namespace BGC.Users
{
    public class UserData : ProfileData
    {
        public override bool IsDefault => false;

        //Constructor
        public UserData(string userName, string loggingName = "")
            : base(userName, loggingName)
        {

        }
    }
}