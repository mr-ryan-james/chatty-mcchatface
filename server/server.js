'use strict';

const PORT = process.env.PORT || 3333;

const os = require('os');
const http = require('http');
const express = require('express');
const RoutesConfig = require('./config/routes.conf');
const DBConfig = require('./config/db.conf');
const AuthConfig = require('./config/auth.conf.js')
const Routes = require('./routes/index');
const Socket = require('./socket/index');

const app = express();

RoutesConfig.init(app);
DBConfig.init();
Routes.init(app, express.Router());


let server = http.createServer(app)
  .listen(PORT, () => {
    console.log(`up and running @: ${os.hostname()} on port: ${PORT}`);
    console.log(`enviroment: ${process.env.NODE_ENV}`);
  });

Socket.init(server);