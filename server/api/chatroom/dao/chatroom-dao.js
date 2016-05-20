"use strict";

const mongoose = require('mongoose');
const Promise = require('bluebird');
const chatroomSchema = require('../model/chatroom-model.js');
const _ = require('lodash');

chatroomSchema.statics.createChatroom = (users) => {
    return new Promise((resolve, reject) => {
        if (!_.isObject(users))
            return reject(new TypeError('Chatroom is not a valid object.'));

        let _chatroom = new Chatroom();
        _chatroom.users = users;
        _chatroom.title = "Name Me!";

        _chatroom.save((err, saved) => {
            err ? reject(err) : resolve(saved);
        });
    });
}

chatroomSchema.statics.addChat = (id, text, user) => {
    return new Promise((resolve, reject) => {
        Chatroom.findByIdAndUpdate(
            id,
            {
                $push: {
                    "chats":
                    {
                        user: {
                            firstName: user.firstName,
                            lastName: user.lastName,
                            _id: user._id
                        },
                        text: text
                    }
                }
            },
            { safe: true, new: true },
            function (err, chatroom) {
                err ? reject(err) : resolve(chatroom.chats[chatroom.chats.length - 1]);
            }
        );
    })
}

chatroomSchema.statics.delete = (id) => {
    return new Promise((resolve, reject) => {
        if (!_isString(id))
            return reject(new TypeError("Id is not a valid string."));

        Chatroom
            .findByIdAndRemove(id)
            .exec((err, deleted) => {
                err ? reject(err) : resolve();
            });

    });
}

chatroomSchema.statics.get = (id) => {
    return new Promise((resolve, reject) => {
        Chatroom
            .findById(id)
            .exec((err, chatroom) => {
                err ? reject(err) : resolve(chatroom);
            });
    });
}

chatroomSchema.statics.getAll = (user) => {
    return new Promise((resolve, reject) => {
        let _query = { _id: { $ne: user.id } };
        Chatroom
            .find(_query)
            .exec((err, chatrooms) => {
                err ? reject(err) : resolve(chatrooms);
            })

    })
}

const Chatroom = mongoose.model('Chatroom', chatroomSchema);

module.exports = Chatroom;