## 09-04-2025
### Added
- ReadUserInputAsyn() function to ClientMessageBuilder.cs
- ChatClientFSM.cs [only start state is functional]
### Updated
- Debugger.cs PrintHelp() function
### Changed
- CommandParser.cs no longer uses PrintHelp() from Debugger.cs in case of "/help" user input
### Fixed
- TCPClient.cs DisconnectAsync() function no longer prints hardcoded messages
### Notes
- Authentication attempts are successful, however the user will get trapped in an infinite loop after changing the FSM state from `start` to `auth`. Manual termination with `ctrl + z` is required.
---
## 08-04-2025
### Added
- More functionality to Debugger.cs for printing
- TCPClient.cs and partial functionality
- ITransportClient.cs as an interface for TCP and UDP client variations
- ClientMessageBuilder.cs to construct messages for sending to the server
- CommandParser.cs to parse user input during runtime
### Updated
- .gitignore
- Makefile
- TCPClient.cs to use Debugger.cs print functions
### Changed
- Program.cs to accompany future implementations
---
## 06-04-2025
### Added
- Readme.md
- CHANGELOG.md
- Makefile
- LICENSE
- project2.sln
- src/Program.cs
- src/ArgParser
- src/Debugger.cs
- src/project2.csproj
- .gitignore