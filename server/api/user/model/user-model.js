"use strict";

const mongoose = require('mongoose');

const _userSchema = {
    firstName: {
        type: String,
        required: true,
        trim: true
    },
    lastName: {
        type: String,
        required: true,
        trim: true
    },
    email: {
        type: String,
        required: true,
        trim: true,
        unique: true,
        match: [/^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$/, 'Please fill a valid email address']
    },
    createdAt: {
        type: Date,
        default:
        Date.now
    }
}

let userSchema = mongoose.Schema(_userSchema);
userSchema.plugin(require('mongoose-bcrypt'));

module.exports = userSchema;
