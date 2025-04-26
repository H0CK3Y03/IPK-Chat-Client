# IPK Project 2: Client for a chat server using the IPK25-CHAT protocol
#### Author: Adam Veselý
#### Login: xvesela00
---
## Contents
1. **Theory and Overview**
2. **Code structure**
3. **Implementation details**
4. **Testing**
5. **Bibliography**
---
## Theory and Overview
- This project implements a client application that can communicate with a remote server using the IPK25-CHAT protocol. The protocol has two variants - each built on a different transport protocol: `TCP [RFC9293]` and `UDP [RFC768]`.
- The project was built and tested to work on certain properties:
    - Server port: `4567`
    - Network protocol: `IPv4`
    - Transport protocols: `TCP` and `UDP`
    - Charset: `us-ascii`
- The project is built by using `make` in the root folder and executing the `ipk25chat-client` executable file with the required arguments:
    - `./ipk25chat-client -t <"tcp"|"udp"> -s <"serveraddress">`
- Additional/optional arguments: `-p <"port"> -d <"udptimeout"> -r <"udpretries">`
    - `-g` or `--debug` to enter debugging mode and print additional info to help pinpoint problems
    - `-h` or `--help` as a standalone argument to print the options when executing the executable file
- The protocol defines the following message types to correctly represent the behaviour of each party communicating with this protocol:

| Type name | Notes                       | Description
| --------- | --------------------------- | -----------
| `AUTH`    | This is a _request message_ | Used for client authentication (signing in) using a user-provided username, display name and password
| `BYE`     |                             | Either party can send this message to indicate that the conversation/connection is to be terminated
| `CONFIRM` | `UDP`&nbsp;only | Only leveraged in specific protocol variants (UDP) to explicitly confirm the successful delivery of the message to the other party on the application level
| `ERR`     |                             | Indicates that an error has occurred while processing the other party's last message; this directly results in a graceful termination of the communication
| `JOIN`    | This is a _request message_ | Represents the client's request to join a chat channel by its identifier
| `MSG`     |                             | Contains user display name and a message designated for the channel they're joined in
| `PING`    | `UDP`&nbsp;only             | Periodically sent by a server to all its clients who are using the UDP variant of this protocol as an aliveness check mechanism
| `REPLY`   |                             | Some messages (requests) require a positive/negative confirmation from the other side; this message contains such data
- The user input commands with their respective arguments are:
    - `/auth {username} {secret} {displayname}` which is sent as `AUTH {username} AS {displayname} USING {secret}\r\n`
    - `/join {channelID}` which is sent as `JOIN {channelID} AS {displayname}\r\n`
    - `/rename {displayname}` sets the `{displayname}` locally to be used in the future
    - `/help` prints the options in stdout
    - `{message}` which is sent as `MSG FROM {displayname} IS {message}\r\n`
    - `/bye` which is sent as `BYE FROM {displayname}\r\n`
    - `/error` and any exceptions which is sent as `ERR FROM {displayname} IS {message}\r\n`, errors are locally printed as: `ERROR: {message}\n`
- Received messages further include:
    - `REPLY {"OK"|"NOK"} IS {message}` as a reply to a `auth` or `join` request. It is then locally printed as: `Action success: {message}\n` for `REPLY OK ...` and `Action Failure: {message}\n` for `REPLY NOK ...`
    - Received messages are printed locally as: `{displayname}: {message}\n`
    - Received errors are printed as: `ERROR FROM {displayname}: {message}\n`
---
## Code structure
- All source files are located in `src/`, the executable builds into the root folder of the project using `make` in the root
- `ArgParser.cs` parses and stores command line arguments when running the executable `ipk25chat-client`
- `CancellationSource.cs` keeps track of the cancel token, to ensure termination of all async processes
- `ChatClientFSM.cs` is the finite state machine that directs everything according to the current FSM state
- `ClientMessageBuilder.cs` builds messages that are later sent to the server
- `ClientMessageHandler.cs` handles all messages sent by the server to the client
- `CommandParser.cs` parses user input commands during the runtime of the application
- `Debugger.cs` serves as a debugging tool and logger, where it prints any relevant info
- `ITransportClient.cs` is the interface for the TCP and UDP clients
- `Program.cs` is the main program where the necessary classes are created
- `TCPClient.cs` is the TCP specific implementation of the `ITransportClient.cs` interface
- `Timer.cs` is the timer that monitors whether or not replies come from the server in the required amount of time
- `UDPClient.cs` is the UDP specific implementation of the `ITransportClient.cs` interface (currently non-functional)
---
## Implementation details
- The `Program.cs` file initialises the `ArgParser`, `ITransportClient` and `ChatClientFSM` classes. It also monitors the `Ctrl + C` keypress, where it terminates the program gracefully. The majority of the program gets called by `await fsm.RunAsync()` which is the FSM
- The `ChatClientFSM.cs` is responsible for changing states and expecting certain clinet/server behaviour for the given state. From here, there are calls made to the `TCPClient.cs` file and `UDPClient.cs` file. In each state the program reads user input and server messages asynchronously, allowing both to happen at the same time without any conflicts. Any errors are printed through the `Debugger` class along with any additional debugging information if the application is run with the `-g` or `--debug` flags.
- The `TCPClient.cs` is responsible for the sending and reading of messages to and from the server. It takes pre-built messages using the `ClientMessageBuilder` class to correctly build all messages sent to the server. Any received messages from the server are passed to the `ClientMessageHandler` class to be parsed and handled in a required way
- Similarly to the `TCPClient.cs` file, the `UDPClient.cs` file handles the `UDP` version of the application, building UDP headers and handling dynamic ports according to received messages and user input. Both transport protocols use the same FSM.
---
## Testing
- The main testing method for the TCP transport protocol was using **netcat** and manually typing any desired commands/messages, simulating server-client interactions.
    - `nc -l -p 4567` was used to open the port 4567 on localhost and simulated the server. It was open on a seperate bash terminal.
    - `./ipk25chat-client -t tcp -s 127.0.0.1` was used to connect to the localhost server on a different bash terminal and simulated the client-side relation of the client-server interactions.
    - For example:
        - Client: `/auth a b c` - server receives `AUTH a AS C USING b\r\n`
        - Server sends either `REPLY OK IS FINE` (client prints: `Action Success: FINE`), `REPLY NOK IS NOT FINE` (client prints `Action Failure: NOT FINE`) or server waits 5s and client terminates the connection with an error message: `ERR FROM c IS ERROR: No reply from server. Exiting...\r\n`
        - If the reply received was `OK` and we have debugging enabled, we can see we have entered the `open` state. From here we can manually test interactions again.
- Additional testing occured through publically available student tests (mentioned in the Bibliography section)
- For `UDP` (currently non-functional) I used a similar approach, where I manually simulated all interactions:
    - `sudo tcpdump -i lo udp port 4567 or src port 4567 -X` on one terminal
    - `./ipk25chat-client -t udp -s 127.0.0.1` on a another terminal
    - I then monitored the incoming packets on the "server" terminal, sadly I did not have enough time to debug and fix the issue. I will attempt to do so in the future.
---
## Bibliography
[ChatGpt] OpenAI. ChatGpt (GPT-4) [online]. Used to help with Bibliographic citations and explanation of some harder-to-grasp concepts.

Crocker, D. H., & Overell, P. (1997, November). Augmented BNF for Syntax Specifications: ABNF (RFC 2234). IETF. https://www.ietf.org/rfc/rfc2234.txt

Faculty of Information Technology, Brno University of Technology, "IPK Projects - Project 2: Client behaviour, input, and commands," [Online]. Available: https://git.fit.vutbr.cz/NESFIT/IPK-Projects/src/branch/master/Project_2#client-behaviour-input-and-commands - Used for documentation purposes and main theory/overview.

V. Malashchuk and T. Hobza, "VUT_IPK_CLIENT_TESTS," GitHub repository, 2025. [Online]. Available: https://github.com/Vlad6422/VUT_IPK_CLIENT_TESTS - Used for more advanced testing purposes.
