#!/bin/bash
set -e

# Define paths
TEST_PROJECT_PATH="./weatherStation/WeatherStation.Tests/WeatherStation.Tests.csproj"
TEST_ASSEMBLY_PATH="./weatherStation/WeatherStation.Tests/bin/Debug/net10.0/WeatherStation.Tests.dll"
COVERAGE_OUTPUT="./coverage/coverage.cobertura.xml"
REPORT_DIR="./coverage/report"

# Ensure you're in the project root
cd /Users/peterlarsson/Projects/weatherStation

# Clean and build the solution
echo "Building the solution..."
dotnet clean
dotnet build

# Run the tests (without any coverage collectors)
echo "Running tests..."
dotnet test --no-build

# Run coverlet.console to generate coverage
echo "Generating coverage report with coverlet.console..."
dotnet coverlet "$TEST_ASSEMBLY_PATH" \
    --target "dotnet" \
    --targetargs "test $TEST_PROJECT_PATH --no-build" \
    --format "cobertura" \
    --output "$COVERAGE_OUTPUT" \
    --exclude-by-file "**/*g.cs"

# Generate the HTML report
echo "Generating HTML report..."
dotnet reportgenerator \
    "-reports:$COVERAGE_OUTPUT" \
    "-targetdir:$REPORT_DIR" \
    "-filefilters:-*g.cs"
