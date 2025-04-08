# Set the name of the project
PROJECT_NAME = ipk25-chat
SRC = src/

# Define the output directory for the published files
# OUTPUT_DIR = ./ipk25-chat

# Define the target runtime (you can adjust this as needed)
RUNTIME = linux-x64

# Default target: clean and publish the project
all: clean publish

# Publish the application as a single file
publish:
	dotnet publish -r $(RUNTIME) -c Release -o .
# able to add /p:PublishSingleFile=true to the end instead of going into csproj and defning it there

# Clean the build artifacts (bin, obj, and publish directories)
clean:
	rm -rf $(PROJECT_NAME) $(SRC)bin $(SRC)obj

# Restore NuGet packages (optional, but can be useful if there are package updates)
restore:
	dotnet restore

# Declare phony targets to avoid conflicts with files with the same name
# This ensures that the targets are always executed
# even if files with the same name exist in the directory
.PHONY: all clean publish restore