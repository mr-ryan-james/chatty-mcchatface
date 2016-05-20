"use strict"

const mongoose = require("mongoose");

const _lastReadSchema = {
    userId: mongoose.Schema.Types.ObjectId,
    lastReadDate: Date
}

module.exports = mongoose.Schema(_lastReadSchema);