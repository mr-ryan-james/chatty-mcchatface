"use strict";

const ChatController = require('../controller/chatroom-controller');
const auth = require("../../../auth");

module.exports = class ChatRoomRoutes {
    static init(router) {
        router
            .route('/api/chatroom')
            .get(auth.authorize, auth.userContextRequired, ChatController.getAll)
            .post(auth.authorize, auth.userContextRequired, ChatController.createChatroom)
            .put(auth.authorize, ChatController.updateChatroom);

        router
            .route('/api/chatroom/:id')
            .get(ChatController.get)
            .delete(auth.authorize, ChatController.delete);
    }
}