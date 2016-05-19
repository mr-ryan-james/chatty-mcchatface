"use strict";

const ChatController = require('../controller/chat-controller');

module.exports = class ChatRoutes {
    static init(router) {
        router
            .route('/api/chat')
            .get(auth.authorize, auth.userContextRequired, ChatController.getAll)
            .post(ChatController.createChatroom)
            .put(ChatController.updateChatroom);

        router
            .route('/api/chat/:id')
            .get(ChatController.get)
            .delete(ChatController.deleteUser);
    }
}