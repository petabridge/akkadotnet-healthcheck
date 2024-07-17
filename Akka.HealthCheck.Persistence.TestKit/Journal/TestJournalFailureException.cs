//-----------------------------------------------------------------------
// <copyright file="TestJournalFailureException.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Akka.HealthCheck.Persistence.TestKit.Journal;

[Serializable]
public class TestJournalFailureException : Exception
{
    public TestJournalFailureException() { }
    public TestJournalFailureException(string message) : base(message) { }
    public TestJournalFailureException(string message, Exception inner) : base(message, inner) { }
    protected TestJournalFailureException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}