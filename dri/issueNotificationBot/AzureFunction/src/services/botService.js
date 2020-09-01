const axios = require('axios');
const { MicrosoftAppCredentials } = require('botframework-connector');
const path = require('path');
const ENV_FILE = path.join(__dirname, '.env');
require('dotenv').config({ path: ENV_FILE });

module.exports = class BotService {
    constructor(context) {
        this.context = context;
        this.credentials = new MicrosoftAppCredentials(process.env.MicrosoftAppId, process.env.MicrosoftAppPassword);
        this.client = axios.create({
            baseURL: process.env.BotBaseUrl
        });
    }

    async sendData(body) {
        const token = await this.credentials.getToken();
        const response = await this.client.post('/api/data', body, {
            headers: { Authorization: `Bearer ${ token }` }
        });
        if (response.status !== 200) {
            this.context.error(JSON.stringify(response, null, 2));
        } else {
            this.context.log(`Successfully sent GitHub data to the bot. Response Code: ${ response.status }`);
        }
    }
}