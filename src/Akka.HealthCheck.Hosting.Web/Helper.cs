// -----------------------------------------------------------------------
// <copyright file="Utility.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Akka.HealthCheck.Hosting.Web
{
    public static class Helper
    {
        public static class Names
        {
            public static readonly string Liveness = string.Join("-", Tags.Liveness);
            public static readonly string Readiness = string.Join("-", Tags.Readiness);
            public static readonly string ClusterLiveness = string.Join("-", Tags.ClusterLiveness);
            public static readonly string ClusterReadiness = string.Join("-", Tags.ClusterReadiness);
            public static readonly string PersistenceLiveness = string.Join("-", Tags.PersistenceLiveness);
        }
        
        public static class Tags
        {
            public static readonly string[] Liveness = { "akka", "live", "node" };
            public static readonly string[] ClusterLiveness = { "akka", "live", "cluster" };
            public static readonly string[] PersistenceLiveness = { "akka", "live", "persistence" };
            public static readonly string[] Readiness = { "akka", "ready", "node" };
            public static readonly string[] ClusterReadiness = { "akka", "ready", "cluster" };
            public static readonly string[] Live = { "akka", "live" };
            public static readonly string[] Ready = { "akka", "ready" };
            public static readonly string[] Akka = { "akka" };
        }

        internal static string ToPath(this string[] tags)
            => string.Join("/", tags);
        
        public static class Filters
        {
            public static readonly Func<HealthCheckRegistration, bool> Liveness = healthCheck =>
                healthCheck.Tags.IsSupersetOf(Tags.Liveness);
            public static readonly Func<HealthCheckRegistration, bool> Readiness = healthCheck =>
                healthCheck.Tags.IsSupersetOf(Tags.Readiness);
            public static readonly Func<HealthCheckRegistration, bool> ClusterLiveness = healthCheck =>
                healthCheck.Tags.IsSupersetOf(Tags.ClusterLiveness);
            public static readonly Func<HealthCheckRegistration, bool> ClusterReadiness = healthCheck =>
                healthCheck.Tags.IsSupersetOf(Tags.ClusterReadiness);
            public static readonly Func<HealthCheckRegistration, bool> PersistenceLiveness = healthCheck =>
                healthCheck.Tags.IsSupersetOf(Tags.PersistenceLiveness);

            public static readonly Func<HealthCheckRegistration, bool> AllLiveness = healthCheck =>
                healthCheck.Tags.IsSupersetOf(Tags.Live);

            public static readonly Func<HealthCheckRegistration, bool> AllReadiness = healthCheck =>
                healthCheck.Tags.IsSupersetOf(Tags.Ready);
            public static readonly Func<HealthCheckRegistration, bool> All = healthCheck =>
                healthCheck.Tags.IsSupersetOf(Tags.Akka); 
        }
        
        public static Task JsonResponseWriter(HttpContext context, HealthReport healthReport)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var serializer = new JsonSerializer();
            var jsonWriter = new JTokenWriter();
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("status");
            jsonWriter.WriteValue(healthReport.Status.ToString());
            jsonWriter.WritePropertyName("results");
            jsonWriter.WriteStartObject();

            foreach (var healthReportEntry in healthReport.Entries)
            {
                jsonWriter.WritePropertyName(healthReportEntry.Key);
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("status");
                jsonWriter.WriteValue(healthReportEntry.Value.Status.ToString());
                jsonWriter.WritePropertyName("description");
                jsonWriter.WriteValue(healthReportEntry.Value.Description);
                jsonWriter.WritePropertyName("data");
                jsonWriter.WriteStartObject();

                foreach (var item in healthReportEntry.Value.Data)
                {
                    jsonWriter.WritePropertyName(item.Key);
                    serializer.Serialize(jsonWriter, item.Value, item.Value?.GetType() ?? typeof(object));
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();

            return context.Response.WriteAsync(jsonWriter.Token?.ToString() ?? "");
        }
    }
}