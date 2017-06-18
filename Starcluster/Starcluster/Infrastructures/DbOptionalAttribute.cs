using System;

namespace Starcluster.Infrastructures
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DbOptionalAttribute : Attribute
    {
    }
}