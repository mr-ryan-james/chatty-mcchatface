# chatty-mcchatface

To try out this app, navigate to https://chatty-mcchatface.herokuapp.com

OR, if you would rather set it up on your own machine, follow the below:

1. Make sure you have mongo installed, and start an instance on your machine
2. Clone the git repo
3. npm install everything
4. open up a terminal, and cd into the cloned directory, and type in
5. npm start
6. Register a user for yourself.

The app will seem rather underwhelming by yourself, but either get a friend, or open up an incognito
Chrome window and talk with yourself. I talk to myself all the time! It's fun! That's not weird at
all, right?

Right??

Guys?

## Running Locally (Modernized Version)

This project now consists of a .NET 8 backend API and an Angular frontend.

**Prerequisites:**

-   .NET 8 SDK
-   Node.js (v18+ recommended) and npm
-   Run `npm install` inside the `angular-client` directory:
    ```bash
    cd angular-client
    npm install
    cd ..
    ```

**Running the Application:**

From the root project directory (`/Users/ryanpfister/Dev/chatty-mcchatface`), run the following
command:

```bash
npm run dev
```

This will concurrently:

-   Start the .NET backend API (listening on `http://localhost:5124` and `https://localhost:7045`).
-   Start the Angular frontend development server (available at `http://localhost:4200`).

Open your browser to `http://localhost:4200` to use the application.

### Running with Docker

This project includes a `Dockerfile` to build and run the application in a container. To handle
secrets securely when running with Docker, follow these steps:

1.  **Prerequisites:**

    -   Docker installed and running.
    -   Node.js installed (for the secrets generation script).
    -   Ensure your .NET user secrets are configured correctly for the
        `dotnet_server/ChattyMcChatface.Api` project (as described in the Configuration (Secrets)
        section below).

2.  **Generate Docker Secrets File:** Before building or running the container, execute the helper
    script from the project root directory (`/Users/ryanpfister/Dev/chatty-mcchatface`) to convert
    your user secrets into a Docker-compatible JSON file:

    ```bash
    node generate-docker-secrets.js
    ```

    This creates an `appsettings.Docker.json` file in the project root (this file is ignored by
    git).

3.  **Build the Docker Image:** From the project root directory, build the image:

    ```bash
    docker build -t chatty-mcchatface .
    ```

4.  **Run the Docker Container:** Run the container, mounting the generated
    `appsettings.Docker.json` file as a volume. This makes the secrets available to the application
    inside the container.

    ```bash
    # Example: Run container named 'chatty', mapping host port 8080 to container port 8080
    docker run -d --name chatty -p 8080:8080 -v "$(pwd)/appsettings.Docker.json:/app/appsettings.Docker.json:ro" chatty-mcchatface
    ```

    -   `-d`: Run in detached mode.
    -   `--name chatty`: Assign a name to the container.
    -   `-p 8080:8080`: Map port 8080 on your host to port 8080 in the container.
    -   `-v "$(pwd)/appsettings.Docker.json:/app/appsettings.Docker.json:ro"`: Mount the generated
        secrets file into the container's `/app` directory as `appsettings.Docker.json`. The `:ro`
        makes it read-only inside the container for added security.
    -   `chatty-mcchatface`: The name of the image to run.

    The application should now be accessible at `http://localhost:8080`.

### Configuration (Secrets)

The .NET backend requires API keys and other sensitive configuration for AI providers (like OpenAI,
Azure, Vertex AI, etc.). These are managed using the .NET Secret Manager, which keeps them separate
from the source code.

During development, these secrets need to be set up for the API project. If you followed the setup
instructions involving external secret files, this should already be done.

To view the secrets currently configured for the API project on your local machine, run the
following command from the project root directory (`/Users/ryanpfister/Dev/chatty-mcchatface`):

```bash
dotnet user-secrets list --project dotnet_server/ChattyMcChatface.Api
```

## Getting Started for Developers

This section provides a high-level overview for developers looking to contribute to either the
backend or frontend of ChattyMcChatface.

### Project Structure

The project is organized into two main parts:

-   **`dotnet_server/`**: Contains the .NET 8 backend API.
    -   `ChattyMcChatface.Api/`: ASP.NET Core Web API project (controllers, SignalR hub,
        `Program.cs`).
    -   `ChattyMcChatface.Core/`: Core business logic, services (including AI providers), DTOs.
    -   `ChattyMcChatface.Data/`: Entity Framework Core setup, DbContext, entities, migrations
        (using SQLite).
    -   `ChattyMcChatface.Tests.Unit/`: Unit tests.
    -   `ChattyMcChatface.Tests.Integration/`: Integration tests (may require secrets).
-   **`angular-client/`**: Contains the Angular frontend application.
    -   `src/app/`: Main application code (modules, components, services).
    -   `src/app/shared/services/`: Core services for API interaction (e.g., `chat.service.ts`,
        `auth.service.ts`, `signalr.service.ts`).
    -   `src/environments/`: Environment configuration (API URLs).

### Backend Development (.NET)

-   **Technology**: .NET 8, ASP.NET Core Web API, Entity Framework Core, SignalR.
-   **Database**: SQLite (`dotnet_server/ChattyMcChatface.Data/chatty.db`). Migrations are managed
    via EF Core (`dotnet ef migrations add ...`, `dotnet ef database update ...` within the
    `dotnet_server/ChattyMcChatface.Api` directory).
-   **Running**: Navigate to `dotnet_server/ChattyMcChatface.Api` and run `dotnet run`.
-   **Testing**: Navigate to the respective test project directory (`Tests.Unit` or
    `Tests.Integration`) and run `dotnet test`. Integration tests require secrets.
-   **Configuration/Secrets**: API keys for AI providers are managed using .NET User Secrets. Set
    them using
    `dotnet user-secrets set "Provider:KeyName" "KeyValue" --project dotnet_server/ChattyMcChatface.Api`.
    See `secrets.example.json` for expected keys.
-   **AI Integration**: The system uses a provider model (`IAiProvider`) with specific
    implementations (OpenAI, Azure, Gemini, Claude, Vertex). `PersonaService` orchestrates responses
    using `AiFallbackUtil` and provider-specific delegates configured via factories and singleton
    model classes. See `documentation/ai_provider_development_guide.md` and
    `documentation/persona_model_mapping.md` for details.
-   **Real-time**: SignalR is used for real-time communication
    (`dotnet_server/ChattyMcChatface.Api/Hubs/ChatHub.cs`).

### Frontend Development (Angular)

-   **Technology**: Angular, TypeScript, RxJS.
-   **Running**: Navigate to `angular-client/` and run `npm install` (if needed), then `ng serve`.
    The app will be available at `http://localhost:4200`.
-   **API Interaction**: Uses Angular's `HttpClient` within services found in
    `src/app/shared/services/`. Base API URL is configured in `src/environments/`.
-   **Real-time**: Connects to the backend SignalR hub via `@microsoft/signalr` library, managed by
    `src/app/shared/services/signalr.service.ts`.
-   **Components**: Feature components are organized within `src/app/` (e.g., `chat/`, `user/`,
    `auth/`).

### Combined Development

The easiest way to run both backend and frontend for development is using the script in the root
`package.json`:

```bash
# From the root project directory
npm run dev
```

This uses `concurrently` to start both the .NET API and the Angular development server.

### Further Documentation

More detailed guides can be found in the `documentation/` directory, including:

-   `ai_provider_development_guide.md`
-   `persona_model_mapping.md`
-   `ai-persona-plan-overview.md` (and backend/frontend specifics)

---

#### Technologies used in this application

1. Node.js v6.1.0
2. Angular 2 RC1
3. Mongoose/Mongo
4. Typescript

#### Tested in Chrome, Firefox, Safari, all on MAC OS X

#### Detailed explanation of what's happening

![alt tag](https://raw.githubusercontent.com/puhfista/chatty-mcchatface/master/highlevel.png)

This application starts off by having a person identify themselves in a registration/login
component.

A user sees all other users in the application, and can add them into a chat.

The chatroom model consists of an array of users, an array of chats, an array of lastreads, and a
created/last updated date.

I am using a denormalized strategy here. Instead of having to read someone's first name, last name
for every chatroom that is created, I simply store those pieces of information with the user \_id.
That way I don't have to go grab that information every time I need it. Ask yourself, how often do
people change their names when using an application? I propose it is less expensive to update all of
someone's chat information the few times they change their name (if ever), than to read that
information from the User store every time we open a chat.

I'm storing the abbreviated user information with every chat instance. After going down the rabbit
hole a bit, I realized I could optimize the application a bit. Before I took this to production, I
would want to change this:

```

AbbreviatedUser: {firstName, lastName, Id}

Chatroom: {
    _id: ObjectId,
    Users: [AbbreviatedUser],
    Chats: [
        {
            AbbreviatedUser,
            Text,
            Date
        }],
    LastReads: [LastReadModel],
    Date: Date
}
```

To this ...

```
Chatroom: {
    _id: ObjectId,
    Users: [AbbreviatedUser],
    Chats: [
        {
            UserId,
            Text,
            Date
        }],
    LastReads: [LastReadModel],
    Date: Date
}
```

...and in the UI, store one instance of the first name, last name, and use that same object when
iterating through the chats. I would still keep storing first name, last name information in the
chat, to save a lookup against the User collection. Again, we can go find every one of these
instances and change them in the future, if the user changes their name. That isn't going to happen
very often. Loading the chats is going to happen a lot in a moderatly used application.

When a user creates a chatroom, that chatroom is persisted to Mongo, and an event is broadcasted via
socket.io, and all users who are a part of that chatroom will see it show up in their list of
chatrooms. They are free to join or not join at this point. Users who are not in a chatroom will see
the last two most recent chats automatically refreshed as they are sent in the chatroom.

If a user joins the chatroom, they will see a live feed of chats, again thanks to the magic of
socketio.

Authentication in the application is handled with JWTs (JSON Web Tokens). After a user
registers/logs in, a token is sent down with the user information, which is stored in the local
store. When this token expires, if the user is in the middle of doing something, they will be
automatically redirected back to the login screen.

#### Horizontal Scalability

![alt tag](https://raw.githubusercontent.com/puhfista/chatty-mcchatface/master/horizontal.png)

As indicated earlier, this application makes use of JWTs for user persistence. As users make
requests that require user context to the application, a JWT is sent in the HTTP header
(x-access-token) with each http request payload.

Currently, the JWT verifying signature is created on the production server using an app_secret saved
in an environment variable.

server/auth/index.js

```
    static generateToken(userId) {

        let obj = {
            id: userId,
        };

        return jwt.sign(obj, authConfig.getSecret(), {
            expiresIn: "1d"
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
```

server/config/auth.conf.js

```
module.exports = class AuthConfig {

    static getSecret() {
      return (process.env.NODE_ENV === 'production') ? process.env.APP_SECRET : appConst.app_secret;
    }
};
```

In Heroku and AWS, this environment variable (process.env.APP_SECRET) is automatically propogated to
all children processes within the load balancing process. With minimal effort, we could store this
secret in a Redis instance or any other persistent store that we could then pull down as needed into
the application.

If you want to have fun plugging in your token and seeing what the result looks like, you can do so
fairly simply.

Assuming you are authenticated into Chatty-McChatface ->

1. Open up Developer Tools in Chrome
2. Navigate to the Resources bar
3. Expand "Local Storage"
4. Click on https://chatty-mcchatface.herokuapp.com ⋅⋅\* You should see a "user" key and a "token"
   key.
5. Copy the entire value of the Token key.
6. Go to https://jwt.io/
7. Paste the oken into the "Encoded" field on the page.

There should be 3 sections seperated by . in the JWT. The first is a base64 encoded header, the
second is a base64 encoded payload (which shows the user id and expiration), and the third is the
verifying signature.  
These three components make up our JWT.

#### Things not yet implemented in this alpha release of Chatty McChatface

1. "Last read" information. I want to indicate to the users visually what chatrooms have unread
   chats in them.

    The way I would accomplish this is best explained by pointing to the lastreadschema:

    ```
    const _lastReadSchema = {
        userId: mongoose.Schema.Types.ObjectId,
        lastReadDate: Date
    }
    ```

    Every chatroom has an array of these for every user. Every time you join a chatroom, I update
    the corresponding "last read" object for that user. Any chatroom that has an "created/updated"
    date that is after this date would have some sort of visual indication.

2. Paging. We would need it for chatrooms with chats of any significant size, and for users who join
   quite a few chatrooms. With a bit of Mongo querying magic, paging could be done fairly simply.

#### Cool bonus things that I may actually do

I obviously copied the name for this app from Boaty McBoatface, the famous boat name that never came
to be. Google also copied the naming idea for their deep-learning project Tensorflow.

Google trained Tensorflow to break down the composition of speech. They named that project Parsey
McParseface.

You can read more about that here:

https://github.com/tensorflow/models/tree/master/syntaxnet

Eventually, I would want to allow users the option to have Google break down their sentence
structures in real time. Does this have any practical value? Not really. Would it be super cool?
Yes, yes it would.
