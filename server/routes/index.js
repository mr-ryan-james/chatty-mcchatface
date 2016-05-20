"use strict";

const UserRoutes = require('../api/user/routes/user-routes');
const ChatroomRoutes = require('../api/chatroom/routes/chatroom-routes');


module.exports = class Routes {
  static init(app, router) {
    UserRoutes.init(router);
    ChatroomRoutes.init(router);


    app.use('/', router);
    app.use('/chatty/*', function (req, res) {
      
      if(req.baseUrl.indexOf("tsconfig.json") > -1){
        res.sendFile(process.cwd() + '/client/tsconfig.json');
        return;
      }
      
      res.sendFile(process.cwd() + '/client/index.html');
    });
  }
}
