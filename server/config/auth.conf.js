const appConst = require('../constants/app.json');


module.exports = class AuthConfig {
    static getSecret() {
      const secret = (process.env.NODE_ENV === 'production') ? process.env.APP_SECRET
                                                          : dbConst.app_secret;
    }
};
