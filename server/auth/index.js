'use strict'

const jwt = require("jsonwebtoken");
const authConfig = require("../config/auth.conf.js")

module.exports = class Auth {
    static generateToken(userId){
        
        obj = {
                id: userId,
            };
        
        return jwt.sign(obj, authConfig.getSecret(), {
                        expiresInMinutes: 1200
        });
    }
    
    static authorize(req, res, next){
        // check header or url parameters or post parameters for token
        var token = req.body.token || req.query.token || req.headers['x-access-token'];

        // decode token
        if (token) {

            // verifies secret and checks exp
            jwt.verify(token, authConfig.getSecret(), function(err, decoded) {      
            if (err) {
                return res.json({ 
                    success: false, 
                    message: 'Failed to authenticate token.' 
                });    
            } else {
                // if everything is good, save to request for use in other routes
                req.decoded = decoded;    
                next();
            }
            });

        } else {
            // if there is no token
            // return an error
            return res.status(403).send({ 
                success: false, 
                message: 'No token provided.' 
            });
            
        }
        
    }
}