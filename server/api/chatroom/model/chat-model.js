"use strict";

const mongoose = require('mongoose');

const abbreviatedUserSchema = require("./abbreviated-user-model.js");

const _chatSchema = {
    user: abbreviatedUserSchema,
    text:String,
    date: { 
        type: Date, 
        default: Date.now 
    }
}

module.exports = mongoose.Schema(_chatSchema);