#!/bin/bash
dotnet run --no-launch-profile --project samples/AutoMappic.Samples.TodoApi/AutoMappic.Samples.TodoApi.csproj --urls "http://localhost:5031" > api.log 2>&1 &
API_PID=$!

echo "Waiting for API to start..."
for i in {1..30}; do
  if curl -s http://localhost:5031/ > /dev/null; then
    echo "API is up!"
    break
  fi
  sleep 1
done

echo "--- Initial Data ---"
curl -s http://localhost:5031/todo-lists | jq .

echo -e "\n--- Updating Todo List (Id 1) ---"
curl -s -X PUT http://localhost:5031/todo-lists/1 \
     -H "Content-Type: application/json" \
     -d '{
       "title": "Groceries (Updated via Smart-Sync)",
       "items": [
         { "id": 1, "description": "Milk (2 Gallons)", "isDone": true },
         { "id": 0, "description": "Coffee Beans", "isDone": false }
       ]
     }'

echo -e "\n--- Final Data ---"
curl -s http://localhost:5031/todo-lists | jq .

kill $API_PID
