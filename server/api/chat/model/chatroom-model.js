'use strict'

const mongoose = require("mongoose"); 
const chatSchema = require("./chat-model.js");
const lastReadSchema = require("./lastread-model.js");

const _chatRoomSchema = {
    title: String,
    chats: [chatSchema],
    userIds: [Schema.Types.ObjectId],
    lastReads: [lastReadSchema],
    date: { 
        type: Date, 
        default: Date.now 
    }
}

module.exports = mongoose.Schema(_chatRoomSchema);