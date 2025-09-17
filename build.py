#!/usr/bin/env python3
"""
Build script to create standalone executable
"""

import subprocess
import sys
import os

def build_executable():
    """Build the standalone executable using PyInstaller."""
    try:
        # PyInstaller command
        cmd = [
            "pyinstaller",
            "--onefile",  # Create a single executable file
            "--windowed",  # Hide console window (remove this if you want to see console output)
            "--name=DatabasePDFGenerator",
            "--add-data=config.json;.",  # Include config file
            "--hidden-import=pyodbc",
            "--hidden-import=reportlab",
            "--hidden-import=PIL",
            "main.py"
        ]
        
        print("Building standalone executable...")
        print(f"Command: {' '.join(cmd)}")
        
        # Run PyInstaller
        result = subprocess.run(cmd, check=True, capture_output=True, text=True)
        
        print("Build completed successfully!")
        print(f"Executable location: dist/DatabasePDFGenerator.exe")
        
    except subprocess.CalledProcessError as e:
        print(f"Build failed: {e}")
        print(f"Error output: {e.stderr}")
        sys.exit(1)
    except Exception as e:
        print(f"Unexpected error during build: {e}")
        sys.exit(1)

if __name__ == "__main__":
    build_executable()