"use strict";

const UserRoutes = require('../api/user/routes/user-routes');
const ChatRoutes = require('../api/chat/routes/chat-routes');


module.exports = class Routes {
  static init(app, router) {
    UserRoutes.init(router);
    ChatRoutes.init(router);


    app.use('/', router);
    app.use('/chatty/*', function (req, res) {
      res.sendFile(process.cwd() + '/client/index.html');
    });
  }
}
