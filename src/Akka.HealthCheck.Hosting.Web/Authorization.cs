// -----------------------------------------------------------------------
// <copyright file="Authorization.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;

namespace Akka.HealthCheck.Hosting.Web
{
    public interface IAuthorization
    { }

    public sealed class DefaultAuthorization : IAuthorization
    {
        public static DefaultAuthorization Instance = new DefaultAuthorization();
        private DefaultAuthorization()
        { }
    }

    public sealed class PolicyNameAuthorization : IAuthorization
    {
        public PolicyNameAuthorization(params string[] policyNames)
        {
            PolicyNames = policyNames;
        }

        public string[] PolicyNames { get; }
    }

    public sealed class AuthorizeDataAuthorization : IAuthorization
    {
        public AuthorizeDataAuthorization(params IAuthorizeData[] data)
        {
            Data = data;
        }

        public IAuthorizeData[] Data { get; }
    }
}