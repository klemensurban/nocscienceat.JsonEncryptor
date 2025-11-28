using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using nocscienceat.Aes256GcmRsaCryptoService;

namespace JsonEncryptor;

internal class Program
{
    static int Main(string[] args)
    {
        try
        { 
            Options? options = ParseArguments(args); if (options == null) { PrintUsage(); return 1; }

            if (string.IsNullOrWhiteSpace(options.JsonFilePath))
            {
                Console.WriteLine("Error: -f <pathToJsonFile> is required.");
                PrintUsage();
                return 1;
            }

            if (!File.Exists(options.JsonFilePath))
            {
                Console.WriteLine($"Error: File not found: {options.JsonFilePath}");
                return 1;
            }

            // Load JSON file
            string jsonContent = File.ReadAllText(options.JsonFilePath);

            JsonNode? root;
            try
            {
                root = JsonNode.Parse(jsonContent);
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Error: Failed to parse JSON file.");
                Console.WriteLine(ex.Message);
                return 1;
            }

            if (root == null)
            {
                Console.WriteLine("Error: JSON file is empty or invalid.");
                return 1;
            }

            // Process JSON, replacing "<ask>" values
            ProcessJsonNode(root, path: "");

            // Build result JSON string
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string resultJson = root.ToJsonString(jsonOptions);

            Console.WriteLine();
            Console.WriteLine("Resulting JSON:");
            Console.WriteLine(resultJson);

            Console.WriteLine("Press Enter to continue or ctrl-c to terminate");
            Console.ReadLine();
            byte[] plainText = Encoding.UTF8.GetBytes(resultJson);

            ReadOnlySpan<byte> plainTextSpan = plainText.AsSpan();
            if (string.IsNullOrWhiteSpace(options.CertificateThumbprint) ||
                options.CertificateThumbprint.Length is not (40 or 64) ||
                !options.CertificateThumbprint.All(Uri.IsHexDigit))
            {
                throw new ArgumentException("Certificate thumbprint must be a 40 or 64 character hexadecimal string.");
            }
            byte[] cipherText = CryptoService.Encrypt(plainTextSpan, options.CertificateThumbprint, options.CertificateThumbprint, !options.UseCurrentUserStore);

            string base64CipherText = Convert.ToBase64String(cipherText, Base64FormattingOptions.InsertLineBreaks);

            // Output the Base64-encoded ciphertext to a file with name <CertificateThumbprint>.encVault in the current directory
            string outputFileName = $"{options.CertificateThumbprint}.encVault";
            File.WriteAllText(outputFileName, base64CipherText);

            //readback and decrypt file to verify
            string readBackBase64 = File.ReadAllText(outputFileName);
            byte[] readBackCipherText = Convert.FromBase64String(readBackBase64);
            byte[] decryptedPlainText = CryptoService.Decrypt(readBackCipherText, options.CertificateThumbprint, options.CertificateThumbprint, !options.UseCurrentUserStore);
            string decryptedJson = Encoding.UTF8.GetString(decryptedPlainText);
            Console.WriteLine();
            Console.WriteLine("Decrypted JSON for verification:");
            Console.WriteLine(decryptedJson);


            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("An unexpected error occurred:");
            Console.WriteLine(ex);
            return 1;
        }
    }

    private static void ProcessJsonNode(JsonNode? node, string path)
    {
        if (node == null) return;

        switch (node)
        {
            case JsonObject obj:
                // Copy entries to avoid modifying during enumeration
                var entries = new List<KeyValuePair<string, JsonNode?>>();
                foreach (var kvp in obj)
                {
                    entries.Add(kvp);
                }

                foreach (var kvp in entries)
                {
                    string childPath = string.IsNullOrEmpty(path)
                        ? kvp.Key
                        : $"{path}.{kvp.Key}";

                    // If the value itself is "<ask>", handle it here
                    if (kvp.Value is JsonValue valueNode &&
                        valueNode.TryGetValue<string>(out string? s) &&
                        s == "<ask>")
                    {
                        Console.Write($"Enter replacement for '{childPath}': ");
                        string? replacement = Console.ReadLine() ?? string.Empty;
                        obj[kvp.Key] = replacement;
                    }
                    else
                    {
                        // Recurse into complex nodes
                        ProcessJsonNode(kvp.Value, childPath);
                    }
                }
                break;

            case JsonArray arr:
                for (int i = 0; i < arr.Count; i++)
                {
                    string childPath = string.IsNullOrEmpty(path)
                        ? $"[{i}]"
                        : $"{path}[{i}]";

                    JsonNode? item = arr[i];

                    if (item is JsonValue valueNode &&
                        valueNode.TryGetValue<string>(out string? s) &&
                        s == "<ask>")
                    {
                        Console.Write($"Enter replacement for '{childPath}': ");
                        string? replacement = Console.ReadLine() ?? string.Empty;
                        arr[i] = replacement;
                    }
                    else
                    {
                        ProcessJsonNode(item, childPath);
                    }
                }
                break;

            case JsonValue valueNode:
                break;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  JsonEncryptor -f <pathToJsonFile> [-t <certificateThumbprint>] [-u]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -f <path>   Path to input JSON file (required).");
        Console.WriteLine("  -t <thumb>  Certificate thumbprint .");
        Console.WriteLine("  -u          Use CurrentUser certificate store instead of LocalMachine .");
    }

    private static Options? ParseArguments(string[] args)
    {
        if (args.Length == 0)
            return null;

        Options options = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg.ToLowerInvariant())
            {
                case "-f":
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: Missing value for -f.");
                        return null;
                    }
                    options.JsonFilePath = args[++i];
                    break;

                case "-t":
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: Missing value for -t.");
                        return null;
                    }
                    options.CertificateThumbprint = args[++i];
                    break;

                case "-u":
                    options.UseCurrentUserStore = true;
                    break;

                default:
                    Console.WriteLine($"Unknown argument: {arg}");
                    return null;
            }
        }

        return options;
    }


}