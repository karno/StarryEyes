﻿using System;
using System.Windows.Media;
using Cadena.Data;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public sealed class QuotedEvent : TwitterEventBase
    {
        public QuotedEvent(TwitterUser source, TwitterStatus status)
            : base(source, status.QuotedStatus?.User, status)
        {
            if (status.QuotedStatus == null)
            {
                throw new ArgumentException("specified status has not quote information.");
            }
        }

        public override string Title => "QT";

        public override string Detail => Source.ScreenName + ": " + TargetStatus;

        public override Color Background => MetroColors.Olive;
    }
}