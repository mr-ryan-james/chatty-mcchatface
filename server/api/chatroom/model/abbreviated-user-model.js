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
    }
}

module.exports = mongoose.Schema(_abbreviatedUserModel);