
# Discord dialogue
### A discord bot that gathers all messages(sentences) said in a server and stores them in a dataset and generates replies using them.

## Github: [DiscordDialogue](https://github.com/Ee0Rk/DiscordDialogue) Invite: [Bot invite link](https://discord.com/api/oauth2/authorize?client_id=1124775606157058098&permissions=76800&scope=bot%20applications.commands) Website: [Site](http://datastash-ee0rk.pythonanywhere.com/docs/dialogue)
# Dataset documentation
	Gathered sentences are split into words. 
	The words are laid out on a dataset. Each line of the dataset is one word.
	The line starts with the word. 
	Then the unique ID of the word is in brackets. 
	Example: "here(16702420440804575377)".
	The ID is made up of an unsigned long(ulong). 
	Each word gets one, after the bracketed ID comes the pointers.
	The pointers are IDs with weights strapped to the word, they point to other words used together.
	To signify the start of a pointer a ">" is placed. 
	Following it is the unique ID of a pointer. Example: ">8550945603721123070".
	After the pointer, an integer of the weight of said pointer is placed. 
	The weight indicates how many times that pointer has been used.
	The pointers are sorted by the use of this weight, it is also used to make prediction easier and more coherent.
	To signify the start of a weight a "^" is placed. 
	After that indicator, the integer is placed. Example: "^4".
	After that, the cycle repeats for any more pointers. 
	Here is an example of 3 pointers: ">9904587785882601485^1>6874912168670054529^1>1920564224340893869^2".
	Here is an example of a full word: "french(3390027714710160172)>17478102515992830987^1>14621114671688366770^1>5120379740791764018^1"
	To signify the first word in a sentence a "~" is placed after the unique ID of the word. 
	Example: "think(4639753057128184689)~". After that, the pointers are placed.
	To signify the last word in a sentence a "<" is placed after the unique ID of a word. 
	Example: "navy(6609155505895385199)<". There are no pointers following.
	The dataset is stored in plain text in a .txt file. 
	The file can become very big rapidly for that reason. 
	Compression might come in the future.
# Commands
|Command|Arguments|Description|
|-|-|-
|/train|/train **URL** |Trains the server-specific dataset with a URL with training data
|/setchannel|**None**|Sets the active channel of the bot. Type the command in the channel you want
|/quote|**None**|Replies with a random quote from supreme ruler Zhironovsky of the LDPR
|/useglobal|**None**|Changes the status of whether to reply with the global dataset or guild-specific one
|/video|**None**|Replies with a random video of supreme ruler Zhironovksy of the LDPR
|Mention|**None**|Responds with a generated sentence from the data gathered. If you change the global status it can generate a sentence based on data gathered from all servers the bot is a part of
___
# Dev commands
### This is for developers only, only a few select people have the ability to use these commands. If you want to be a candidate for this, message *"ainawastaken"* on Discord.
|Command|Arguments|Description|
|-|-|-
|/reset|**None**|Resets ALL guild/user-specific data stored by the bot. Handy when the storage structure changes and it needs to be reset for the new version. It wipes ALL data stored including user data, per guild settings, per guild datasets, and sentences. Does not wipe the global dataset
___
