"use strict";

const mongoose = require('mongoose');

const _userSchema = {
    firstName: {type: String, required: true, trim: true},
    email: {type: String, required: true, trim: true, unique: true},
    createdAt: {type: Date, default: Date.now}
}

module.exports = mongoose.Schema(_userSchema);
