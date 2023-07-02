# Discord Dialogue
 a discord bot that gathers all messages(sentances) said in a server and stores them in a dataset. 

# Dataset documentation
 Gathered sentances are split into words. The words are layed out on a dataset. Each line of the dataset is one word.
 The line starts with the word. Then the unique ID of the word in brackets. Example: "here(16702420440804575377)".
 The ID is made up of an unsigned long(ulong). Each word gets one, after the bracketed ID comes the pointers.
 The pointers are ID's with wheights strapped to the word, they point to other words used together.
 To signify the start of a pointer a ">" is placed. Following it is the unique ID of a pointer. Example: ">8550945603721123070".
 After the pointer, a interger of the wheight of said pointer is placed. The wheight indicates how many times that pointer has been used.
 The pointers are sorted by use of this wheight, it is also used to make prediction easier and more cohearent.
 To signify the start of a wheight a "^" is placed. After that indicator the interger is placed. Example: "^4".
 After that the cycle repeats for any more pointers. Here is an example of 3 pointers: ">9904587785882601485^1>6874912168670054529^1>1920564224340893869^2".
 Here is an example of a full word: "french(3390027714710160172)>17478102515992830987^1>14621114671688366770^1>5120379740791764018^1>10681523470316633301^1"
 To signify the first word in a sentance a "~" is placed after the unique ID of the word. Example: "think(4639753057128184689)~". After that the pointers are placed.
 To signify the last word in a sentance a "<" is placed after the uniqye ID of a word. Example: "navy(6609155505895385199)<". There are no pointers following.
 The dataset is stored in plain text in a .txt file. The file can become very big rapidly for that reason. Compression might come in the future.
