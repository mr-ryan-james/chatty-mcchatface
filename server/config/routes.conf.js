"use strict";

const morgan = require('morgan');
const bodyParser = require('body-parser');
const contentLength = require('express-content-length-validator');
const helmet = require('helmet');
const express = require('express');

const staticJsFiles = [
    "/node_modules/es6-shim/es6-shim.min.js",
    "/node_modules/zone.js/dist/zone.js",
    "/node_modules/reflect-metadata/Reflect.js",
    "/node_modules/systemjs/dist/system.src.js",
    "/node_modules/zone.js/dist/zone.js",
    "/node_modules/reflect-metadata/Reflect.js",
    "/node_modules/systemjs/dist/system.src.js"
]

module.exports = class RouteConfig {
    static init(application) {
        let _root = process.cwd();
        let _clientFiles =  '/client';

        staticJsFiles.forEach(function(staticJsFile){
            application.use(express.static(_root + staticJsFile));
        });

        application.use(express.static(_root + _clientFiles));
        application.use(bodyParser.json());
        application.use(morgan('dev'));
        application.use(contentLength.validateMax({max: 999}));
        application.use(helmet());
    }
}
