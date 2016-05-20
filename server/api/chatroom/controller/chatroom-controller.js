"use strict";

const ChatroomDao = require("../dao/chatroom-dao");

function _extractAbbreviatedUser(fullUser) {
    return {
        _id: fullUser._id instanceof String ? fullUser._id : fullUser._id.toString(),
        firstName: fullUser.firstName,
        lastName: fullUser.lastName
    }
};

module.exports = class ChatRoomController {
    static getAll(req, res) {
        ChatroomDao
            .getAll(req.user)
            .then(chatrooms => res.status(200).json(chatrooms))
            .catch(error => res.status(400).json(error));
    }

    static createChatroom(req, res) {
        let _users = req.body.map(_extractAbbreviatedUser);
        _users.push(_extractAbbreviatedUser(req.user));

        ChatroomDao
            .createChatroom(_users)
            .then(chatroom => res.status(201).json(chatroom))
            .catch(error => res.status(400).json(error));
    }

    static updateChatroom(req, res) {
        let _chatroom = req.body;

        ChatroomDao
            .updateChatroom(_chatroom)
            .then(chatroom => res.status(200).json(chatroom))
            .catch(error => res.status(400).json(error));
    }

    static get(req, res) {
        let _id = req.params.id;

        ChatroomDao
            .get(_id)
            .then(chatroom => res.status(200).json(chatroom))
            .catch(error => res.status(400).json(error));
    }

    static delete(req, res) {
        let _id = req.params.id;

        ChatroomDao
            .delete(_id)
            .then(() => res.status(200).end())
            .catch(error => res.status(400).json(error));
    }

}