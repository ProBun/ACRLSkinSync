Uploading skins to the FTP
- New View
- select skin
- parse and check meets conditions
	max 2048px in either dimension
	max 10MB per file
	only upload valid files .dds
	check for ui.json, preview etc
- if files don't meet requirements resize and compress (imagemagik?)
- do above in temp folder
- launch showroom/viewr of compressed files for user verfication
- yes -> upload
- resave originals so that they are newer than compressed versions and don't get overwritten? 
- Button on current screen to take to upload screen
