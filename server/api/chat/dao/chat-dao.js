"use strict";

const mongoose = require('mongoose');
const Promise = require('bluebird');
const chatroomSchema = require('../model/chatroom-model.js');
const _ = require('lodash');

chatroomSchema.statics.createChatroom = (chatroom) => {
    return new Promise((resolve, reject) => {
      if (!_.isObject(chatroom))
          return reject(new TypeError('Chatroom is not a valid object.'));

      let _chatroom = new Chatroom(chatroom);

      _chatroom.save((err, saved) => {
        err ? reject(err)
            : resolve(saved);
      });
    });
}

const Chatroom = mongoose.model('Chatroom', chatroomSchema);

module.exports = Chatroom;