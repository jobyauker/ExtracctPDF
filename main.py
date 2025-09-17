#!/usr/bin/env python3
"""
Database PDF Generator
Connects to VALSEG23/GREEN database, retrieves session form image data,
decodes content, and saves as PDF with the title as filename.
"""

import sys
import os
import json
import base64
import pyodbc
from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import letter
from reportlab.lib.utils import ImageReader
from io import BytesIO
import logging

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class DatabasePDFGenerator:
    def __init__(self, config_file="config.json"):
        """Initialize the PDF generator with configuration."""
        self.config_file = config_file
        self.config = self.load_config()
        self.connection = None
        
    def load_config(self):
        """Load configuration from JSON file."""
        try:
            if not os.path.exists(self.config_file):
                self.create_default_config()
                logger.error(f"Config file {self.config_file} not found. Created default config. Please update it with your settings.")
                sys.exit(1)
                
            with open(self.config_file, 'r') as f:
                config = json.load(f)
                
            # Validate required fields
            required_fields = ['id', 'server', 'database', 'username', 'password']
            for field in required_fields:
                if field not in config:
                    logger.error(f"Missing required field '{field}' in config file")
                    sys.exit(1)
                    
            return config
            
        except json.JSONDecodeError as e:
            logger.error(f"Invalid JSON in config file: {e}")
            sys.exit(1)
        except Exception as e:
            logger.error(f"Error loading config: {e}")
            sys.exit(1)
    
    def create_default_config(self):
        """Create a default configuration file."""
        default_config = {
            "id": "1",
            "server": "VALSEG23",
            "database": "GREEN",
            "username": "your_username",
            "password": "your_password",
            "driver": "{ODBC Driver 17 for SQL Server}",
            "trusted_connection": "no"
        }
        
        with open(self.config_file, 'w') as f:
            json.dump(default_config, f, indent=4)
            
        logger.info(f"Created default config file: {self.config_file}")
    
    def connect_to_database(self):
        """Establish connection to SQL Server database."""
        try:
            connection_string = (
                f"DRIVER={self.config['driver']};"
                f"SERVER={self.config['server']};"
                f"DATABASE={self.config['database']};"
                f"UID={self.config['username']};"
                f"PWD={self.config['password']};"
                f"Trusted_Connection={self.config.get('trusted_connection', 'no')};"
            )
            
            self.connection = pyodbc.connect(connection_string)
            logger.info("Successfully connected to database")
            
        except pyodbc.Error as e:
            logger.error(f"Database connection error: {e}")
            sys.exit(1)
        except Exception as e:
            logger.error(f"Unexpected error connecting to database: {e}")
            sys.exit(1)
    
    def execute_query(self, record_id):
        """Execute SQL query to retrieve Title and Content."""
        try:
            cursor = self.connection.cursor()
            query = "SELECT Title, Content FROM dbo.tblSessionFormImage WHERE ID = ?"
            
            cursor.execute(query, (record_id,))
            result = cursor.fetchone()
            
            if result is None:
                logger.error(f"No record found with ID: {record_id}")
                return None, None
                
            title, content = result
            logger.info(f"Retrieved record: {title}")
            return title, content
            
        except pyodbc.Error as e:
            logger.error(f"Database query error: {e}")
            return None, None
        except Exception as e:
            logger.error(f"Unexpected error executing query: {e}")
            return None, None
    
    def decode_content(self, content):
        """Decode the content (assuming it's base64 encoded)."""
        try:
            if content is None:
                return None
                
            # Try to decode as base64
            decoded_bytes = base64.b64decode(content)
            return decoded_bytes
            
        except Exception as e:
            logger.error(f"Error decoding content: {e}")
            return None
    
    def save_as_pdf(self, title, content_bytes, output_dir="output"):
        """Save the decoded content as a PDF file."""
        try:
            # Create output directory if it doesn't exist
            Path(output_dir).mkdir(exist_ok=True)
            
            # Sanitize filename
            safe_title = "".join(c for c in title if c.isalnum() or c in (' ', '-', '_')).rstrip()
            safe_title = safe_title.replace(' ', '_')
            pdf_filename = f"{safe_title}.pdf"
            pdf_path = os.path.join(output_dir, pdf_filename)
            
            # Create PDF
            c = canvas.Canvas(pdf_path, pagesize=letter)
            width, height = letter
            
            # Try to determine content type and handle accordingly
            if content_bytes:
                # Check if it's an image
                try:
                    # Try to create an image from the bytes
                    img_buffer = BytesIO(content_bytes)
                    img = ImageReader(img_buffer)
                    
                    # Calculate image dimensions to fit on page
                    img_width, img_height = img.getSize()
                    scale = min(width / img_width, height / img_height) * 0.8
                    
                    # Center the image on the page
                    x = (width - img_width * scale) / 2
                    y = (height - img_height * scale) / 2
                    
                    c.drawImage(img, x, y, width=img_width * scale, height=img_height * scale)
                    
                except Exception:
                    # If not an image, try to treat as text
                    try:
                        text_content = content_bytes.decode('utf-8')
                        lines = text_content.split('\n')
                        
                        y_position = height - 50
                        for line in lines:
                            if y_position < 50:
                                c.showPage()
                                y_position = height - 50
                            c.drawString(50, y_position, line[:80])  # Limit line length
                            y_position -= 15
                            
                    except Exception:
                        # If all else fails, save as binary data reference
                        c.drawString(50, height - 50, f"Binary content from database record")
                        c.drawString(50, height - 70, f"Content length: {len(content_bytes)} bytes")
                        c.drawString(50, height - 90, f"Unable to display content directly")
            
            c.save()
            logger.info(f"PDF saved successfully: {pdf_path}")
            return pdf_path
            
        except Exception as e:
            logger.error(f"Error saving PDF: {e}")
            return None
    
    def process_record(self):
        """Main processing function."""
        try:
            # Connect to database
            self.connect_to_database()
            
            # Get ID from config
            record_id = self.config['id']
            logger.info(f"Processing record ID: {record_id}")
            
            # Execute query
            title, content = self.execute_query(record_id)
            if title is None or content is None:
                logger.error("Failed to retrieve data from database")
                return False
            
            # Decode content
            decoded_content = self.decode_content(content)
            if decoded_content is None:
                logger.error("Failed to decode content")
                return False
            
            # Save as PDF
            pdf_path = self.save_as_pdf(title, decoded_content)
            if pdf_path is None:
                logger.error("Failed to save PDF")
                return False
            
            logger.info(f"Successfully processed record and saved PDF: {pdf_path}")
            return True
            
        except Exception as e:
            logger.error(f"Unexpected error in processing: {e}")
            return False
        finally:
            if self.connection:
                self.connection.close()
                logger.info("Database connection closed")

def main():
    """Main entry point."""
    try:
        generator = DatabasePDFGenerator()
        success = generator.process_record()
        
        if success:
            logger.info("Process completed successfully")
            sys.exit(0)
        else:
            logger.error("Process failed")
            sys.exit(1)
            
    except KeyboardInterrupt:
        logger.info("Process interrupted by user")
        sys.exit(1)
    except Exception as e:
        logger.error(f"Unexpected error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()