"use strict";

const mongoose = require('mongoose');


const _chatSchema = {
    firstName: {
        type: String,
        required: true,
    },
    lastName: {
        type: String,
        required: true
    },
    userId: {
        type: Schema.Types.ObjectId,
        required: true
    },
    text:String,
    date: { 
        type: Date, 
        default: Date.now 
    }
}

module.exports = mongoose.Schema(_chatSchema);