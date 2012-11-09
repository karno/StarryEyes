using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Operations
{
    public class UpdateProfileOperation : OperationBase<TwitterUser>
    {
        public AuthenticateInfo AuthInfo { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string Location { get; set; }

        public string DescriptionText { get; set; }

        protected override IObservable<TwitterUser> RunCore()
        {
            return AuthInfo.UpdateProfile(
                Name,
                Url,
                Location,
                DescriptionText);

        }
    }
}