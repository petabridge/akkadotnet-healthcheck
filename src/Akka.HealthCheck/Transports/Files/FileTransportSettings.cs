namespace Akka.HealthCheck.Transports.Files
{
    /// <inheritdoc />
    /// <summary>
    /// Used to write out the liveness / readiness status messages when <see cref="F:Akka.HealthCheck.ProbeTransport.File" />
    /// is used.
    /// </summary>
    public sealed class FileTransportSettings : ITransportSettings
    {
        public FileTransportSettings(string filePath)
        {
            FilePath = filePath;
        }

        /// <summary>
        /// The relative or absolute path of where the file should be written.
        /// </summary>
        public string FilePath { get; }

        public ProbeTransport TransportType => ProbeTransport.File;
        public string StartupMessage => $"Writing probe data to [{FilePath}]";
    }
}
