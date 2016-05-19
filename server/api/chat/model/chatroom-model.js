'use strict'

const mongoose = require("mongoose"); 
const chatSchema = require("./chat-model.js");
const lastReadSchema = require("./lastread-model.js");
const abbreviatedUserSchema = require("./abbreviated-user-model.js");

const _chatRoomSchema = {
    title: String,
    chats: [chatSchema],
    users: [abbreviatedUserSchema],
    lastReads: [lastReadSchema],
    date: { 
        type: Date, 
        default: Date.now 
    }
}

module.exports = mongoose.Schema(_chatRoomSchema);