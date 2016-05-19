"use strict";

const mongoose = require('mongoose');


const _abbreviatedUserModel = {
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
    }
}

module.exports = mongoose.Schema(_abbreviatedUserModel);