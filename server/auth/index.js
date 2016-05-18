'use strict'

const jwt = require("jsonwebtoken");
const authConfig = require("../config/auth.conf.js")

module.exports = class Auth {
    static generateToken(userId){
        
        let obj = {
                id: userId,
            };
        
        return jwt.sign(obj, authConfig.getSecret(), {
                        expiresIn: "24h"
        });
    }
    
    static authorize(req, res, next){
        var token = req.body.token || req.query.token || req.headers['x-access-token'];

        if (!token) {
            
            return res.status(403).send({ 
                message: 'No token provided.' 
            });
        }

        jwt.verify(token, authConfig.getSecret(), function(err, decoded) {      
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
}