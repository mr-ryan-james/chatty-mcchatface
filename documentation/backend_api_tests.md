# Backend API Tests

This file contains curl commands to test the .NET backend API running on `http://localhost:5124`.

## Initial Setup

```bash
# Check if server is running
lsof -i -P -n | grep LISTEN | grep 5124
```

## Authentication

### Register Users

```bash
# Register User 1
curl -i -X POST http://localhost:5124/api/auth/register -H "Content-Type: application/json" -d '{"firstName": "Test", "lastName": "User", "email": "test@example.com", "password": "Password123!"}'

# Register User 2 (for chat testing)
curl -i -X POST http://localhost:5124/api/auth/register -H "Content-Type: application/json" -d '{"firstName": "Another", "lastName": "User", "email": "another@example.com", "password": "Password123!"}'
```

### Login and Get Token

```bash
# Login User 1 (test@example.com) - Manually copy the 'token' value from the response JSON below
curl -i -X POST http://localhost:5124/api/auth/login -H "Content-Type: application/json" -d '{"email": "test@example.com", "password": "Password123!"}'

# Set the token variable (replace YOUR_JWT_TOKEN with the actual token)
TOKEN="YOUR_JWT_TOKEN"
# IMPORTANT: You must replace YOUR_JWT_TOKEN with a valid JWT token from the login response above.
# All authenticated requests below will fail without a real token. The $TOKEN variable is used in all subsequent authenticated requests.
```

## User Management

### Get All Users

```bash
# Get all users (Auth Required)
curl -i -X GET http://localhost:5124/api/users -H "Authorization: Bearer $TOKEN"

# Note the ID of 'another@example.com' from the response for later use (e.g., USER_ID_2=...)
```

### Get Specific User

```bash
# Get specific user by ID (Auth Required)
# Replace USER_ID_TO_GET with an actual ID from the previous step
curl -i -X GET http://localhost:5124/api/users/USER_ID_TO_GET -H "Authorization: Bearer $TOKEN"
```

## Chatroom Management

### Create Chatroom

```bash
# Create a new chatroom with User 2 (Auth Required)
# Replace USER_ID_2 with the actual ID of 'another@example.com'
curl -i -X POST http://localhost:5124/api/chatrooms -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d '{"title": "Test Chatroom", "userIds": [USER_ID_2]}'

# Note the 'id' of the created chatroom from the response JSON for later use (e.g., CHATROOM_ID=...)
```

### Get All Chatrooms

```bash
# Get all chatrooms the authenticated user is part of (Auth Required)
curl -i -X GET http://localhost:5124/api/chatrooms -H "Authorization: Bearer $TOKEN"
```

### Get Specific Chatroom

```bash
# Get details for a specific chatroom (Auth Required)
# Replace CHATROOM_ID_TO_GET with the actual ID noted earlier
curl -i -X GET http://localhost:5124/api/chatrooms/CHATROOM_ID_TO_GET -H "Authorization: Bearer $TOKEN"
```

### Add Message to Chatroom

```bash
# Send a message to a chatroom (Auth Required)
# Replace CHATROOM_ID with the actual ID noted earlier
curl -i -X POST http://localhost:5124/api/chatrooms/CHATROOM_ID/chats -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d '{"text": "Hello from curl!"}'
```

### Update Chatroom

```bash
# Update chatroom details (Auth Required)
# Replace CHATROOM_ID with the actual ID noted earlier
curl -i -X PUT http://localhost:5124/api/chatrooms/CHATROOM_ID -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d '{"title": "Updated Test Chatroom", "addUserIds": [], "removeUserIds": []}'
```

### Delete Chatroom

```bash
# Delete a chatroom (Auth Required)
# Replace CHATROOM_ID with the actual ID noted earlier
curl -i -X DELETE http://localhost:5124/api/chatrooms/CHATROOM_ID -H "Authorization: Bearer $TOKEN"
```

## User Deletion

```bash
# Delete the authenticated user's account (Auth Required)
# Replace USER_ID_SELF with the actual ID of test@example.com
# (You can get this from the token payload or from the /api/users endpoint)
curl -i -X DELETE http://localhost:5124/api/users/USER_ID_SELF -H "Authorization: Bearer $TOKEN"
```
