"use strict";

const UserController = require('../controller/user-controller');
const auth = require("../../../auth");

module.exports = class UserRoutes {
    static init(router) {
      router
        .route('/api/user')
        .get(auth.authorize, auth.userContextRequired, UserController.getAllOthers)
        .post(UserController.createUser)
        .put(UserController.loginUser);

      router
        .route('/api/user/:id')
        .get(UserController.get)
        .delete(UserController.deleteUser);
    }
}
