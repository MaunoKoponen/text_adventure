Unity project, a text adventure game engine, using generative AI to aid development


This an experimantal game project, created to test generative AI capabilities for game development.

**Background**

The rise of large language models and generative image creation technologies like Stable Diffusion and Midjourney, starting from 2022, have a substantial influence on shaping the expectations and predictions about the future of game development processes.

In this project my goal is to experiment with currently available tools, and find good practices and workflows for game development in a small team, or even for a solo game creator.

As generative AI 3d object creation does not yet (end of 2023) give any real advantage in game development, I chose to make a game project that is based on traditional text adventure format. Also, I decided to make the game traditional, "non gen ai" in sense that the game content creation is not based on on-the-fly content generation while player is engaging with the game, but is produced during game development and is thus "static". This is to separate the two use cases clearly: gen AI aided game development and gen AI as a game engine.  

**Generative AI is used it this project**

Code/data structure:
- Deciding the structure of data: ChatGPT 4 was prompted to get the initial json format for the room/dialogue data
- Game code: ChatGPT 4 was used to get the initial game code. (As complexity grew, more human developer involvement is needed)      

Visuals: 
- Character/location images are created with StableDiffusion, Automatic1111 local installation.
- Item graphics are creatd first with Dalle-3 and then modified with StableDiffusion to make variations
- UI elements: base images created with Dalle-3, then modified in Krita.
- Content creation: room description json files are generated prompting ChatGPT (and local LLM with OobaBooga text generation web-UI using TheBloke_Mythalion-13B-GPTQ model) and then refined by hand.

**Main features**

- Rooms: - locations with possibility for dialogue with npcs. room access can be set to be based on item in inventory, flag set
- Dialogues: have branching, can trigger receiving items, setting flags, starting/concluding quests
- Quests Log: quests are logged in a journal, status updted when concluded
- Items: items with description, effect, price, icon
- Inventory: Inventory showing owned item with item icon, description
- Shop: NPC's can have shop functionality triggered during conversation. Shop view shows items available for purchase, prices, and player inventory 
- Map: (wip) current location, visited/ known locations shown on map
- Combat (wip) attacking enemy, using items during combat, death leading to a resurrection sequence
- Savegame: Game state is preserved between session,starting a new game from menu will erase previous progress  

**Status of the project:**
Most features are working, mostly bugfree. Combat is very wip. The content of game, "story" is unfinished, there is just some content to show/test most features.

**Notes on content, highlighting game mechanics**

- Muttons Horn Tavern: Bartender has a shop: "Show me you wares"
- Cartograpers guild: 2 quests can be started here, Additionally, the Adventurer dialogue triggers location flag (but map counterpart of the feature not working yet)
- City gates: shows how to restrict access to room based on items. You need a key to get to the forest.
- Cemetery: shows combat system. Also shows how 2 locations can be used to "fake" a change in location - after beating the enemy, re-entering cemetery will show change in the environment
- Guesthouse: shows how to conclude a quest

**Notes about prompting for images**

Image prompting: all location / npc images have the prompt included in the image data for those interested seeing the exact prompts.


**Notes about prompting for Room descriptions**
 
There is no ready "template" for the room data prompting yet. Best approace is to give a general description of the game to LLM and then add example data, and then tell what changes/features you wish.
Note: the json dialogue data uses "numbering" of responses, in format  1# , 2# - this is mainly for developers convenience, and the numbering  will be removed by code.


Here is the actual prompt used to create first version of Cartographers Guild room description with ChatGPT-4:
```
Topic: json for Cartographers guild
Hi! Im making a text adventure, that used json as "rooms" - here is  a sample json:
{
  "room_id": "temple_of_lost_souls",
  "description": "You find yourself standing in the enigmatic Temple of Lost Souls. The cold, gray stones that make up its walls seem to pulsate with an eerie glow. In t he center, a dimly lit altar surrounded by flickering candles casts long, dancing shadows. There is a temple maid  standing nearby.",
  "items": [],
  "actions": [
    {
      "action_id": "temple_maid_1",
      "flag_false" : "HasSoulStone",
      "action_description": "Talk to temple maid"
    },
    {
      "action_id": "temple_maid_2",
      "flag_true" : "HasSoulStone",
      "action_description": "Talk to temple maid"
    }
  ],
  "dialogues": [
    {
      "npc_name": "temple_maid_1",
      "dialogue_image": "temple_maid",
      "dialogues": [
        {
          "message": "Greetings, traveler. You stand in the Temple of Lost Souls. What brings you to this sacred place?",
          "responses": [
            {
              "text": "What is this temple for?",
              "next_step": 1
            },
            {
              "text": "I'm just exploring.",
              "next_step": -1
            },
            {
              "text": "I felt drawn here.",
              "next_step": 3
            }
          ]
        },
        {
          "message": "1# This temple is a sanctuary for souls that have been detached from their bodies. They find solace and refuge here until they are ready to move on.",
          "responses": [
            {
              "text": "How does a soul become detached?",
              "next_step": 2
            },
            {
              "text": "That sounds eerie.",
              "next_step": -1
            },
            {
              "text": "Can they be reconnected?",
              "next_step": 4
            }
          ]
        },
        {
          "message": "2# Many ways, traveler. Some by force, some by choice. Battles, sorcery, forbidden rituals... the realms of magic and might are fraught with peril.",
          "responses": [
            {
              "text": "I'll be careful.",
              "next_step": -1
            },
            {
              "text": "Have you seen many lost souls?",
              "next_step": 5
            }
          ]
        },
        {
          "message": "3# The temple has a way of calling to those who need it. Listen to its whispers; perhaps there's a message for you.",
          "responses": [
            {
              "text": "I'll heed your advice.",
              "next_step": -1
            }
          ]
        },
        {
          "message": "4# Some say there are ancient rituals that can bind a soul back to its body. But such knowledge is dangerous and often comes with a price.",
          "responses": [
            {
              "text": "A price I'm willing to pay.",
              "next_step": -1
            },
            {
              "text": "I'll keep that in mind.",
              "next_step": -1
            }
          ]
        },
        {
          "message": "5# Yes, many. Some are peaceful, while others are tormented. But all find a moment of rest in this temple.",
          "responses": [
            {
              "text": "I hope they find peace.",
              "next_step": 6
            }
          ]
        },
        {
          "message": "6# In your travels, this Soul Stone might aid you. Would you accept it?",
          "responses": [
            {
              "text": "Yes, thank you.",
              "setFlagTrue": "HasSoulStone",
              "getItem": "Soul Stone",
              "next_step": 7
            },
            {
              "text": "No, I'm fine.",
              "next_step": -1
            }
          ]
        },
        {
          "message": "7# Here, use it wisely",
          "responses": [
            {
              "text": "Thank you",
              "next_step": -1
            }
          ]
        }   
      ]
    },
    {
      "npc_name": "temple_maid_2",
      "dialogue_image": "temple_maid",
      "dialogues": [
        {
          "message": "Greetings again, traveler",
          "responses": [
            {
              "text": "I'm just exploring. dont mind me",
              "next_step": -1
            }
          ]
        }
      ]
    }
  ],
  "exits": [
    {
      "exit_name": "Exit to the town  square",
      "leads_to": "town_square",
      "conditions": [],
      "conditions_not": []
    }
  ]
}
dialogues responses are numbered 0#...N# so its easy to follow the dialogue. Flags can be set to be true of false, and item can be given or received. I would want you to make a Room in this format, location being Cartographers Guild, and in there are old adventurer, that can give rumour about a old copper mine outside a location (come up with cool name), and Guild leader, who ask player if he/her is interested in joinig guild - if so, first quest is given (come up with interesting place to locate and investigate)

```


**Notes about adding new content**

(wip)

In many cases, if you are going to use a certain flag, it makes sense to set it false in the beginning of new game (doable only on code side currently)
This is done currently in RoomManager And in SettingsUI (needs fixing naturally): playerData.SetFlag("HasSoulStone","false");

For items to work, Items need to be in the Items class and also in the ItemRegistry List 
