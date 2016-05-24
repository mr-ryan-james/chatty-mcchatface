# chatty-mcchatface

To try out this app, navigate to https://chatty-mcchatface.herokuapp.com

OR, if you would rather set it up on your own machine, follow the below:

1. Make sure you have mongo installed, and start an instance on your machine
2. Clone the git repo
3. npm install everything
4. open up a terminal, and cd into the cloned directory, and type in
5. npm start
6. Register a user for yourself.

The app will seem rather underwhelming by yourself, but either get a friend, or open up an incognito Chrome window and talk with yourself. I talk to myself all the time! It's fun! That's not weird at all, right?

Right??

Guys?


#### Technologies used in this application
1. Node.js v6.1.0
2. Angular 2 RC1
3. Mongoose/Mongo
4. Typescript

#### Tested in Chrome, Firefox, Safari, all on MAC OS X

#### Detailed explanation of what's happening

![alt tag](https://raw.githubusercontent.com/puhfista/chatty-mcchatface/master/highlevel.png)

This application starts off by having a person identify themselves in a registration/login component. 

A user sees all other users in the application, and can add them into a chat. 

The chatroom model consists of an array of users, an array of chats, an array of lastreads, and a created/last updated date. 

I am using a denormalized strategy here. Instead of having to read someone's first name, last name for every chatroom that is created, I simply store those pieces of information with the user _id. That way I don't have to go grab that information every time I need it.
Ask yourself, how often do people change their names when using an application? I propose it is less expensive to update all of someone's chat information the few times they change their name (if ever), than to read that information from the User store every time we open a chat.

I'm storing the abbreviated user information with every chat instance. After going down the rabbit hole a bit, I realized I could optimize the application a bit. Before I took this to production, I would want to change this:

Chatroom: {
    _id: ObjectId,
    Users: [{firstName, lastName, Id}],
    Chats: [
        {
            AbbreviatedUser: {firstName, lastName, Id}, 
            Text,
            Date
        }],
    LastReads: [LastReadModel],
    Date: Date
}

To this -

Chatroom: {
    _id: ObjectId,
    Users: [{firstName, lastName, Id}],
    Chats: [
        {
            UserId, 
            Text,
            Date
        }],
    LastReads: [LastReadModel],
    Date: Date
}

And in the UI, store one instance of the first name, last name, and use that same object when iterating through the chats. I would still keep storing first name, last name information in the chat, to save a cross lookup against the User store. Again, we can go find every one of these
instances and change them in the future, if the user changes their name. That isn't going to happen very often. Loading the chats is going to happen a lot in a moderatly used application. 



When a user creates a chatroom, that chatroom is persisted to Mongo, and an event is broadcasted via socket.io, and all users who are a part of that chatroom will see it show up in their list of chatrooms. They are free to join or not join at this point. Users who are not in a chatroom will
see the last two most recent chats automatically refreshed as they are sent in the chatroom.

If a user joins the chatroom, they will see a live feed, again thanks to the magic of socketio. 

Authentication in the application is handled with JWTs (JSON Web Tokens). After a user registers/logs in, a token is sent down with the user information, which is stored in the local store. When this token expires, if the user is in the middle of doing something, they will be automatically
redirected back to the login screen. 

#### Things not yet implemented in this alpha release of Chatty McChatface
"Last read" information. I want to indicate to the users visually what chats of unread chats in them. 

The way I would accomplish this is best explained by pointing to the lastreadschema:

const _lastReadSchema = {
    userId: mongoose.Schema.Types.ObjectId,
    lastReadDate: Date
}

Every chatroom has an array of these for every user. Every time you join a chatroom, I update the corresponding "last read" object for that user. Any chatroom that has an "created/updated" date that is after this date would 
have some sort of visual indication. 

#### Cool bonus things that I may actually do

I obviously copied the name for this app from Boaty McBoatface, the famous boat name that never came to be. 
Google also copied the naming idea for their deep-learning project Tensorflow.

Google trained Tensorflow to break down the composition of speech. They named that projecdt Parsey McParseface. 

You can read more about that here:

https://github.com/tensorflow/models/tree/master/syntaxnet

Eventually, I would want to allow users the option to have Google break down their sentence structures in real time. Does this have any practical value? Not really. Would it be super cool? Yes, yes it would. 