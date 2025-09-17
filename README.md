# Database PDF Generator

A standalone executable that connects to a SQL Server database, retrieves session form image data, decodes the content, and saves it as a PDF file.

## Features

- Connects to SQL Server database (VALSEG23/GREEN)
- Executes SQL query to retrieve Title and Content from dbo.tblSessionFormImage
- Decodes base64 encoded content
- Saves content as PDF with the title as filename
- Configurable via JSON config file

## Setup

### Prerequisites

1. Python 3.8 or higher
2. SQL Server ODBC Driver (ODBC Driver 17 for SQL Server recommended)
3. Access to the VALSEG23/GREEN database

### Installation

1. Install Python dependencies:
```bash
pip install -r requirements.txt
```

2. Configure the application by editing `config.json`:
```json
{
    "id": "1",
    "server": "VALSEG23",
    "database": "GREEN",
    "username": "your_username",
    "password": "your_password",
    "driver": "{ODBC Driver 17 for SQL Server}",
    "trusted_connection": "no"
}
```

### Configuration

Edit the `config.json` file with your database credentials:

- `id`: The ID of the record to retrieve from tblSessionFormImage
- `server`: Database server name (VALSEG23)
- `database`: Database name (GREEN)
- `username`: Your database username
- `password`: Your database password
- `driver`: ODBC driver to use
- `trusted_connection`: Set to "yes" if using Windows authentication

## Usage

### Running the Python script directly:
```bash
python main.py
```

### Building a standalone executable:
```bash
python build.py
```

This will create `dist/DatabasePDFGenerator.exe` which can be run on any Windows machine without requiring Python installation.

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

## Requirements

- pyodbc: Database connectivity
- reportlab: PDF generation
- Pillow: Image processing
- pyinstaller: Executable creation

## Troubleshooting

1. **Database Connection Issues**: Verify your database credentials and ensure the SQL Server is accessible
2. **ODBC Driver Issues**: Install the appropriate ODBC driver for your SQL Server version
3. **Content Decoding Issues**: The application assumes content is base64 encoded. Modify the decode_content method if your data uses a different encoding
4. **PDF Generation Issues**: Ensure the output directory is writable and you have sufficient disk space