'use strict'

const jwt = require("jsonwebtoken");
const authConfig = require("../config/auth.conf.js")
const userDao = require("../api/user/dao/user-dao");

module.exports = class Auth {
    static generateToken(userId) {

        let obj = {
            id: userId,
        };

        return jwt.sign(obj, authConfig.getSecret(), {
            expiresIn: "6w"
        });
    }

    static authorize(req, res, next) {
        var token = req.body.token || req.query.token || req.headers['x-access-token'];

        if (!token) {

            return res.status(403).send({
                message: 'No token provided.'
            });
        }

        jwt.verify(token, authConfig.getSecret(), function (err, decoded) {
            if (err) {
                return res.status(401).json({
                    message: 'Failed to authenticate token.'
                });
            } else {
                req.decoded = decoded;
                next();
            }
        });
    }

    static userContextRequired(req, res, next) {
        if (!req.decoded) {

            return res.status(403).send({
                message: 'Please authorize before requiring user context.'
            });
        }

        let user = userDao.get(req.decoded.id)
            .then(user => {
                req.user = user;
                next();
            })
            .catch(error => {
                throw error;
            });
    }
}