# Database PDF Generator

A standalone C# executable that connects to a SQL Server database, retrieves session form image data, decodes the content, and saves it as a PDF file.

## Features

- Connects to SQL Server database (VALSEG23/PLATFORM)
- Executes SQL query to retrieve Title and Content from dbo.tblSessionFormImage
- Decodes base64 encoded content
- Saves content as PDF with the title as filename
- Configurable via JSON config file
- Self-contained executable (no .NET runtime required on target machine)

## Setup

### Prerequisites

1. .NET 8.0 SDK or higher
2. Access to the VALSEG23/PLATFORM database
3. Windows machine (for the executable)

### Installation

1. Clone or download the project files
2. Configure the application by editing `config.json`:
```json
{
    "id": "1",
    "server": "VALSEG23",
    "database": "PLATFORM",
    "username": "your_username",
    "password": "your_password",
    "trusted_connection": false
}
```

### Configuration

Edit the `config.json` file with your database credentials:

- `id`: The ID of the record to retrieve from tblSessionFormImage
- `server`: Database server name (VALSEG23)
- `database`: Database name (PLATFORM)
- `username`: Your database username
- `password`: Your database password
- `trusted_connection`: Set to true if using Windows authentication

## Usage

### Building and running from source:
```bash
# Windows
build.bat

# Linux/Mac
./build.sh
```

### Running the executable:
```bash
./publish/DatabasePDFGenerator.exe
```

The build process creates a self-contained executable in the `publish` directory that can be run on any Windows machine without requiring .NET installation.

## Output

- The application creates an `output` directory
- PDF files are saved with the title from the database as the filename
- Logs are displayed in the console

## Error Handling

The application includes comprehensive error handling for:
- Database connection issues
- SQL query failures
- Content decoding errors
- PDF generation problems
- File system errors
- Configuration validation

## Dependencies

- **System.Data.SqlClient**: SQL Server database connectivity
- **iTextSharp**: PDF generation and manipulation
- **Newtonsoft.Json**: JSON configuration file handling

## Project Structure

- `Program.cs`: Main application logic
- `DatabasePDFGenerator.csproj`: Project file with dependencies
- `config.json`: Configuration file
- `build.bat` / `build.sh`: Build scripts
- `README.md`: This documentation

## Troubleshooting

1. **Database Connection Issues**: 
   - Verify your database credentials
   - Ensure the SQL Server is accessible from your network
   - Check firewall settings

2. **Build Issues**:
   - Ensure .NET 8.0 SDK is installed
   - Run `dotnet restore` to restore NuGet packages

3. **Content Decoding Issues**: 
   - The application assumes content is base64 encoded
   - Modify the `DecodeContent` method if your data uses a different encoding

4. **PDF Generation Issues**: 
   - Ensure the output directory is writable
   - Check available disk space
   - Verify the content can be processed as an image or text

5. **Runtime Issues**:
   - Ensure the executable has proper permissions
   - Check that config.json is in the same directory as the executable