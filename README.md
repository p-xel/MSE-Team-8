# Project 31

A 3D multiplayer card game based on the classic card game **Thirty-One**, built in Unity 6 using Photon Fusion for networking, UI Toolkit for the user interface, and PrimeTween for smooth animations.

---

## Prerequisites

- **Unity Editor**: Version `6000.4.0f1` (Unity 6)
- **Photon Engine Account**: Required to configure a custom multiplayer App ID (Typically an App ID is needed for the multiplayer to work but we're providing one). 

---

## Quick-Start Setup Guide

### 1. Clone & Open the Project
1. Clone this repository to your local machine:
   ```bash
   git clone <repository-url>
   ```
2. Open **Unity Hub**.
3. Click **Add** -> **Add project from disk** and select the cloned repository folder.
4. Ensure the Editor Version is set to **6000.4.0f1**. Open the project.

### 2. Scene Setup & Build Settings
Make sure the key scenes are added to your build settings so that the network launcher can load them correctly:
1. Open **File > Build Settings...**
2. Add the following scenes to the **Scenes In Build** list (ensure the path is correct):
   - `Assets/Scenes/MainMenu.unity`
   - `Assets/Scenes/MultiplayerScene.unity`
3. Make sure `MainMenu.unity` is at index `0`.

### 3. Running & Testing Locally

#### Solo vs. Bots (Offline Mode)
- Open `Assets/Scenes/MainMenu.unity` and press **Play** in the Unity Editor.
- Click **Solo Mode** to launch a game immediately against three local bots. This mode does not require other players to join.

#### Multiplayer Testing
- To test networking/synchronization between multiple clients on the same machine:
  1. Go to **File > Build Settings...** and click **Build And Run** to create a standalone build.
  2. Run the standalone build side-by-side with the Unity Editor.
  3. Log in or use the offline mode, click **Play** (Room Finder), and create/join a shared room.
- To bypass user authentication (which connects to the remote REST API), you can log in using:
  - **Username**: `admin`
  - **Password**: `admin`
  This will log you in locally and bypass the REST API auth requests.

---

## Project Structure & Architecture

- **Core Gameplay (`Assets/Scripts/CoreGameplay`)**:
  - `GameManager.cs`: Controls the main gameplay states (Lobby, Playing, GameOver), turns, round phases (Playing, LastRound, Shooting, Cooldown), and determines round/match winners.
  - `PlayerHand.cs`: Represents each player's hand, handles drawing, swapping cards, skipping, or knocking. Includes simple AI logic for bots when no input authority is present.
  - `GameDeck.cs`: Manages the deck (a custom 36-card deck excluding ranks 2 to 5).
  - `TableHand.cs`: Manages the three community cards placed on the table.
  - `ThreeDCardRaycaster.cs` & `PhysicalCard.cs`: Handle 3D mouse interaction, hover detection, and selection highlight animations.
- **UI System (`Assets/UI`)**:
  - Leverages **Unity UI Toolkit** with USS stylesheets for styling and components.
  - `MainMenuController.cs` & `MainMenuUI.cs`: Control UI screen flows (Login, Room Finder, Stats, Leaderboards, Settings).
  - `ApiClient.cs`: Interacts with the backend REST API hosted at `https://project31-unity-test.cleverapps.io/api` for authentication, stats tracking, leaderboard querying, and match recording.
- **Third-Party Packages**:
  - **PrimeTween**: Used for animating card UI transitions and height offsets.
