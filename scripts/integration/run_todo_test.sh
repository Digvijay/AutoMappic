#!/bin/bash
dotnet run --project samples/AutoMappic.Samples.TodoApi/AutoMappic.Samples.TodoApi.csproj --urls "http://localhost:5031" > todo_api.log 2>&1 &
API_PID=$!

echo "Waiting for API to start..."
sleep 5

echo "--- Initial Data ---"
curl -s http://localhost:5031/todo-lists | jq .

echo -e "\n--- Updating Todo List (Id 1) ---"
echo "We are adding an item, updating one, and deleting another (which is omitted)."
curl -s -X PUT http://localhost:5031/todo-lists/1 \
     -H "Content-Type: application/json" \
     -d '{
       "title": "Groceries (Updated via Smart-Sync)",
       "items": [
         { "id": 1, "description": "Milk (2 Gallons)", "isDone": true },
         { "id": 0, "description": "Coffee Beans", "isDone": false }
       ]
     }'

echo -e "\n\n--- Final Data ---"
curl -s http://localhost:5031/todo-lists | jq .

kill $API_PID
