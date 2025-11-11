#!/bin/bash

# SQLite Database Testing Script for FilthyRodriguez
# This script helps you test database persistence features

set -e

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║     FilthyRodriguez SQLite Database Testing                  ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check if we're in the right directory
if [ ! -f "HtmlTestApp.csproj" ]; then
    echo -e "${RED}Error: Please run this script from the HtmlTestApp directory${NC}"
    exit 1
fi

# Clean up old database
if [ -f "filthy_rodriguez_test.db" ]; then
    echo -e "${YELLOW}🗑️  Removing old database...${NC}"
    rm filthy_rodriguez_test.db
fi

# Check if appsettings.Sqlite.json exists
if [ ! -f "appsettings.Sqlite.json" ]; then
    echo -e "${RED}Error: appsettings.Sqlite.json not found${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Configuration found${NC}"
echo ""

# Build the application
echo -e "${YELLOW}🔨 Building application...${NC}"
dotnet build

echo ""
echo -e "${GREEN}✓ Build complete${NC}"
echo ""

# Run the application
echo -e "${YELLOW}🚀 Starting application with SQLite database...${NC}"
echo ""
echo "Commands to test:"
echo "  1. Open http://localhost:5000 in your browser"
echo "  2. Make a test payment with: 4242 4242 4242 4242"
echo "  3. Check the database: sqlite3 filthy_rodriguez_test.db 'SELECT * FROM stripe_transactions;'"
echo ""
echo "Press Ctrl+C to stop the server"
echo ""

export ASPNETCORE_ENVIRONMENT=Sqlite
dotnet run
