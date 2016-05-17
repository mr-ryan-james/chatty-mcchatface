"use strict";

const UserController = require('../controller/user-controller');

module.exports = class ControllerRoutes {
    static init(router) {
      router
        .route('/api/user')
        .get(UserController.getAll)
        .post(UserController.createUser)
        .put(UserController.loginUser);

      router
        .route('/api/user/:id')
        .get(UserController.get)
        .delete(UserController.deleteUser);
    }
}
