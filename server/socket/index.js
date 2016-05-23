
const socketio = require('socket.io');
const EventEmitter = require('events').EventEmitter;
const emitter = new EventEmitter();

module.exports = class SocketClass{
    static init(server){
        let io = socketio.listen(server);

		emitter.on("eventPushed", function (event) {
			console.log('event "' + event.name + '": "' + event.message + '"');

			if(event.type) {
				io.sockets.in(event.type).emit(event.name, event.message);
			}
			else{
				io.emit(event.name, event.message);
			}
		});


		io.on('connection', function(socket){
			console.log('socket connected');

			socket.on("joinChatroom", function(chatroomId){
				console.log('joining chatroom ' + chatroomId);
				socket.join(chatroomId);
			});
			
			socket.on("listenForChatrooms", function(userId){
				console.log('listening for new chatrooms for user ' + userId);
				socket.join(userId);				
			});

		});
    }
	
	static broadcastEvent(event){
		emitter.emit("eventPushed", event);
		console.log('event pushed emitted');
	}
}