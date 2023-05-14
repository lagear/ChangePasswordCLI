# Spoon Password Replacement Program

This program is designed to replace encrypted passwords in Spoon scripts with new passwords. It automates the process of searching for encrypted passwords in the specified directory, replacing them with the provided plain-text passwords, and verifying the replacements.

## Prerequisites

- [.NET Core SDK](https://dotnet.microsoft.com/download) (version 6 or later)
- [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)
- [Sharprompt](https://www.nuget.org/packages/Sharprompt)

## Installation

1. Clone the repository or download the source code files.
2. Open a command prompt or terminal and navigate to the project directory.
3. Restore the required NuGet packages by running the following command:

dotnet restore


## Configuration

The program uses a configuration file named `appsettings.json` to store the directory paths. You need to provide the paths for the following settings:

- `SpoonDirectoryPath`: The complete path of the Spoon directory.
- `ScriptDirectoryPath`: The complete path of the directory containing the scripts.

Make sure to update the `appsettings.json` file with the correct paths before running the program.

## Usage

1. Open a command prompt or terminal and navigate to the program directory.
2. Run the program using the following command:

dotnet run


3. The program will prompt you to enter the complete path of the Spoon directory. The value will be pre-filled with the `SpoonDirectoryPath` from the configuration file. Press Enter to confirm or provide a new path.
4. The program will check if the Spoon directory and the encryption file exist. If not, it will continue to prompt for a valid Spoon directory path and encryption file path until both are found.
5. Next, the program will prompt you to enter the complete path of the script directory. The value will be pre-filled with the `ScriptDirectoryPath` from the configuration file. Press Enter to confirm or provide a new path.
6. The program will check if the script directory exists. If not, it will continue to prompt for a valid script directory path until it is found.
7. A backup of the script directory will be created in a ZIP file with a timestamp appended to the filename. The ZIP file will be saved in the current directory.
8. The program will display the path of the created ZIP file.
9. The program will search for passwords in the script directory. It will display the found passwords along with their associated usernames and types (SQL or Oracle).
10. You will be prompted to enter the new plain-text passwords for SQL and Oracle.
11. The program will encrypt the new passwords using the provided encryption file.
12. A confirmation prompt will be displayed. If confirmed, the program will replace the old passwords with the new encrypted passwords in the script files.
13. The program will then verify the replacements by searching for the new passwords in the script directory.
14. If any errors or inconsistencies are found, corresponding error messages will be displayed.
15. If the replacements and verifications are successful, a success message will be displayed.
16. Press any key to exit the program.

## Contributing

Contributions are welcome! If you find any issues or have suggestions for improvement, please open an issue or submit a pull request.

## License

This program is licensed under the [MIT License](https://opensource.org/licenses/MIT). Feel free to use, modify, and distribute the code as permitted by the license.
