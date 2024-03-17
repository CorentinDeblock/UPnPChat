# UPnPChat

UPnPChat is a simple projet for learning networking with c#, DO NOT use it in real life scenario.

UPnPChat will use peer to peer to communicate, DO NOT use it with people that you cannot trust.

If you still decide to use it, be aware that UPnPChat author are not responsible if anything bad happend.

## Task

- [x] Setup project for basic socket communication
- [x] Create basic chat interaction (Sending/Receiving message)
- [x] Fix abrupt network disconnection issue (lost internet, etc...)
- [x] Implement RPC protocol
- [x] Allow multi directional communication ("lobby")
- [x] Send structured data (username, message, etc...)
- [x] Create reliable data size protocol for sending and receiving message
- [x] Implement socket id
- [x] Implement socket data session
- [x] Implement one to one message
- [ ] Implement UPnP protocol

## Bug

- [ ] When the client press do /q. The server still listen for the client

## Should be improve

- [ ] RPC is working but it's kind of bad... Calling ServerRPC on the server side does not work, same the other way around. It is intended but should be better handled