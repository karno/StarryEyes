using System;

namespace Codeplex.OAuth
{
    /// <summary>WebRequest HttpMethodType</summary>
    public enum MethodType
    {
        Get, Post
    }

    public static class MethodTypeExtensions
    {
        /// <summary>convert to UPPERCASE string</summary>
        public static string ToUpperString(this MethodType methodType)
        {
            switch (methodType)
            {
                case MethodType.Get:
                    return "GET";
                case MethodType.Post:
                    return "POST";
                default:
                    throw new ArgumentException();
            }
        }
    }
}