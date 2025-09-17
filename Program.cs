using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace DatabasePDFGenerator
{
    public class Program
    {
        private static readonly string ConfigFileName = "config.json";
        private static readonly string OutputDirectory = "output";

        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Database PDF Generator Starting...");
                
                var generator = new DatabasePDFGenerator();
                bool success = await generator.ProcessRecordAsync();
                
                if (success)
                {
                    Console.WriteLine("Process completed successfully!");
                }
                else
                {
                    Console.WriteLine("Process failed!");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }

    public class DatabasePDFGenerator
    {
        private readonly string _configFileName;
        private readonly string _outputDirectory;
        private Config _config;

        public DatabasePDFGenerator(string configFileName = "config.json", string outputDirectory = "output")
        {
            _configFileName = configFileName;
            _outputDirectory = outputDirectory;
        }

        public async Task<bool> ProcessRecordAsync()
        {
            try
            {
                // Load configuration
                _config = LoadConfig();
                if (_config == null)
                {
                    Console.WriteLine("Failed to load configuration");
                    return false;
                }

                Console.WriteLine($"Processing record ID: {_config.Id}");

                // Connect to database and retrieve data
                var (title, content) = await RetrieveDataFromDatabaseAsync();
                if (string.IsNullOrEmpty(title) || content == null)
                {
                    Console.WriteLine("Failed to retrieve data from database");
                    return false;
                }

                Console.WriteLine($"Retrieved record: {title}");

                // Decode content
                var decodedContent = DecodeContent(content);
                if (decodedContent == null)
                {
                    Console.WriteLine("Failed to decode content");
                    return false;
                }

                // Save as PDF
                var pdfPath = await SaveAsPdfAsync(title, decodedContent);
                if (string.IsNullOrEmpty(pdfPath))
                {
                    Console.WriteLine("Failed to save PDF");
                    return false;
                }

                Console.WriteLine($"PDF saved successfully: {pdfPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in processing: {ex.Message}");
                return false;
            }
        }

        private Config LoadConfig()
        {
            try
            {
                if (!File.Exists(_configFileName))
                {
                    CreateDefaultConfig();
                    Console.WriteLine($"Config file {_configFileName} not found. Created default config. Please update it with your settings.");
                    return null;
                }

                string jsonContent = File.ReadAllText(_configFileName);
                var config = JsonConvert.DeserializeObject<Config>(jsonContent);

                if (config == null)
                {
                    Console.WriteLine("Failed to deserialize configuration");
                    return null;
                }

                // Validate required fields
                if (string.IsNullOrEmpty(config.Id) || 
                    string.IsNullOrEmpty(config.Server) || 
                    string.IsNullOrEmpty(config.Database) ||
                    string.IsNullOrEmpty(config.Username) || 
                    string.IsNullOrEmpty(config.Password))
                {
                    Console.WriteLine("Missing required configuration fields");
                    return null;
                }

                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return null;
            }
        }

        private void CreateDefaultConfig()
        {
            var defaultConfig = new Config
            {
                Id = "1",
                Server = "VALSEG23",
                Database = "GREEN",
                Username = "your_username",
                Password = "your_password",
                TrustedConnection = false
            };

            string jsonContent = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
            File.WriteAllText(_configFileName, jsonContent);
            Console.WriteLine($"Created default config file: {_configFileName}");
        }

        private async Task<(string title, byte[] content)> RetrieveDataFromDatabaseAsync()
        {
            string connectionString = BuildConnectionString();
            
            using var connection = new SqlConnection(connectionString);
            
            try
            {
                await connection.OpenAsync();
                Console.WriteLine("Successfully connected to database");

                string query = "SELECT Title, Content FROM dbo.tblSessionFormImage WHERE ID = @Id";
                
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", _config.Id);
                
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    string title = reader["Title"]?.ToString() ?? "";
                    byte[] content = reader["Content"] as byte[];
                    
                    return (title, content);
                }
                else
                {
                    Console.WriteLine($"No record found with ID: {_config.Id}");
                    return (null, null);
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return (null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected database error: {ex.Message}");
                return (null, null);
            }
        }

        private string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = _config.Server,
                InitialCatalog = _config.Database,
                UserID = _config.Username,
                Password = _config.Password,
                IntegratedSecurity = _config.TrustedConnection,
                ConnectTimeout = 30
            };

            return builder.ConnectionString;
        }

        private byte[] DecodeContent(byte[] content)
        {
            try
            {
                if (content == null)
                    return null;

                // Try to decode as base64
                string base64String = Encoding.UTF8.GetString(content);
                return Convert.FromBase64String(base64String);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding content: {ex.Message}");
                return null;
            }
        }

        private async Task<string> SaveAsPdfAsync(string title, byte[] content)
        {
            try
            {
                // Create output directory if it doesn't exist
                Directory.CreateDirectory(_outputDirectory);

                // Sanitize filename
                string safeTitle = SanitizeFileName(title);
                string pdfFileName = $"{safeTitle}.pdf";
                string pdfPath = Path.Combine(_outputDirectory, pdfFileName);

                // Create PDF document
                using var document = new Document(PageSize.A4);
                using var writer = PdfWriter.GetInstance(document, new FileStream(pdfPath, FileMode.Create));
                
                document.Open();

                // Try to determine content type and handle accordingly
                if (content != null && content.Length > 0)
                {
                    // Check if it's an image by trying to create an iTextSharp Image
                    try
                    {
                        var image = iTextSharp.text.Image.GetInstance(content);
                        
                        // Scale image to fit on page
                        image.ScaleToFit(document.PageSize.Width - 40, document.PageSize.Height - 40);
                        image.Alignment = Element.ALIGN_CENTER;
                        
                        document.Add(image);
                    }
                    catch
                    {
                        // If not an image, try to treat as text
                        try
                        {
                            string textContent = Encoding.UTF8.GetString(content);
                            var font = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                            
                            var paragraph = new Paragraph(textContent, font);
                            document.Add(paragraph);
                        }
                        catch
                        {
                            // If all else fails, add a reference to the binary data
                            var font = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                            var paragraph = new Paragraph($"Binary content from database record\nContent length: {content.Length} bytes\nUnable to display content directly", font);
                            document.Add(paragraph);
                        }
                    }
                }
                else
                {
                    var font = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                    var paragraph = new Paragraph("No content found in database record", font);
                    document.Add(paragraph);
                }

                document.Close();
                
                return pdfPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving PDF: {ex.Message}");
                return null;
            }
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "untitled";

            // Remove invalid characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            // Replace spaces with underscores
            fileName = fileName.Replace(' ', '_');

            // Limit length
            if (fileName.Length > 100)
            {
                fileName = fileName.Substring(0, 100);
            }

            return fileName.Trim();
        }
    }

    public class Config
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("server")]
        public string Server { get; set; } = "";

        [JsonProperty("database")]
        public string Database { get; set; } = "";

        [JsonProperty("username")]
        public string Username { get; set; } = "";

        [JsonProperty("password")]
        public string Password { get; set; } = "";

        [JsonProperty("trusted_connection")]
        public bool TrustedConnection { get; set; } = false;
    }
}