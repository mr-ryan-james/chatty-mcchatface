"use strict";

const UserRoutes = require('../api/user/routes/user-routes');


module.exports = class Routes {
  static init(app, router) {
    UserRoutes.init(router);


    app.use('/', router);
    app.use('/chatty/*', function (req, res) {
      res.sendfile(process.cwd() + '/client/index.html');
    });
  }
}
