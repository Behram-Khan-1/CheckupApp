# Goal Reminder System

This is an MVP implementation of a goal reminder system that outputs reminders to the console and chat UI when a goal's set time is reached.

## Features

- **Goal Tracking**: Extract goals from user messages and store them in Firebase
- **Time-based Reminders**: Get reminders when a goal's time is reached
- **Goal Management**: View, complete, and test reminders for goals
- **Command System**: Use special commands to interact with the system
- **Intent Classification**: Automatically detect user intent (goals, streak updates, greetings, etc.)

## Commands

The following commands are available in the chat:

- `/goals` or `show goals` - Display all your goals for today
- `/complete [goal text]` - Mark a goal as completed
- `/test [goal text]` - Test a reminder for a specific goal (appears after 3 seconds)
- `/help` - Show all available commands

## Setting Up Notification Sounds

To add notification sounds for goal reminders:

1. Create a `Resources` folder in your Unity project if it doesn't exist
2. Create a `Sounds` folder inside the `Resources` folder
3. Add an audio file named `notification.mp3` (or other supported format) to the `Sounds` folder
4. The system will automatically load this sound for notifications

## Future Enhancements

- Android notifications for goals
- Better time parsing for more flexible goal timing
- Goal categories and priorities
- Recurring goals
- Goal statistics and progress tracking

## Implementation Details

The system consists of the following key components:

- **GoalReminderManager**: Checks goal timings against current time and displays reminders
- **DatabaseManager**: Handles saving and loading goals and streaks from Firebase
- **ChatInputManager**: Processes user input, classifies intent, and handles special commands
- **ChatUIManager**: Displays messages in the chat interface
- **PromptBuilder**: Constructs prompts for the AI, including intent classification

The reminder check runs every minute by default, but this can be adjusted in the GoalReminderManager component.

## Streak Tracking

The system now supports tracking streaks (like NoFap, workout, meditation):

- Automatically detects when a user is updating a streak
- Stores streak information including name, status, current streak count, and longest streak
- Updates streak information in Firebase