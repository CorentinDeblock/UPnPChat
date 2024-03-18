# 0.3.0

- (host) Instead of having a host socket. You have now a host socket and a client socket that connect to that host
- Added rpc
- Removed data handler interface for a callback aproach
- Removed the need to socket data type for host and client. You can now send every type of data as long there is a data handler to handle that
- Added reliable data size protocol
- Added socket id
- Added socket data session
- Added one to one message
- Changed UTF8 string encoding to Unicode instead to respect string data size
- Implemented AES message encryption
 
# 0.2.0

- Added multi directional communication between socket (lobby)
- Added Host and Client connection type
- Added SocketConnection class as a base for all type of connection. SocketConnection handle pure socket functionnality
- Fixed abrupt network disconnection issue

# 0.1.0

- Removed test code
- Added asynchronous data receiver and sender
- Added basic chat interaction