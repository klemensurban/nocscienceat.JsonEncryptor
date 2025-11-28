namespace JsonEncryptor;

internal class Options
{
    public string? JsonFilePath { get; set; }
    public string? CertificateThumbprint { get; set; }
    public bool UseCurrentUserStore { get; set; }
}