const appConst = require('../constants/app.json');


module.exports = class AuthConfig {
    
    static getSecret() {
      return (process.env.NODE_ENV === 'production') ? process.env.APP_SECRET : appConst.app_secret;
    }
};
