# Media File Date Fixer
This tool changes file system dates to match the date/time information found in the meta data of photo and video files.

Supported file formats are JPEG, TIFF, MP4, MOV, PNG, ARW, CR2, NEF and ORF.

![Screenshot](https://raw.githubusercontent.com/schellingb/MediaFileDateFixer/master/README.png)

## Download
You can find the download under the [Releases page](https://github.com/schellingb/MediaFileDateFixer/releases/latest).

## Usage
On launch, after selecting a directory, the program will show a list of files with mismatching date/time information.

### File List
 - Checkbox:   If enabled, the file is marked to be updated
 - Name:       Full path of the media file
 - File Date:  File modification time/date (from file system)
 - Meta Date:  Creation date of media file (from meta data)
 - Detection:  Kind of time difference (see [Detection States](#detection-states))
 - Error:      Empty unless there was an error while extracting the meta data
 - Difference: Days/hours/minutes/seconds difference between the two time stamps

Upon startup, the list will have all rows disabled.
A row can be activated by either clicking the checkbox or by clicking the `Meta Date` cell.
Multiple rows can be selected by holding down the shift key, then they all can be toggled active by pressing the space bar.
The to-be-applied time stamp is highlighted with a green background.

The list can be re-sorted by clicking on the column headers.

Double clicking the `Name` cell of a row will open the file with the associated default application.

Double clicking the `Detection` cell of a row will show a list of all meta data entries found in the media file.

### Buttons
 - Open Folder:                  Open a different directory of media files
 - Filter Results:               Select which [detection states](#detection-states) are shown in the list
 - Enable All:                   Enable all rows with mismatching dates shown in the list
 - Disable All:                  Disable all rows
 - Apply Meta Date To File Date: Update the file system time stamps of all active rows
 - Offset:                       Set an offset to be applied to the file date (see [Time Zone Differences](#time-zone-differences))
 - Progress Bar:                 Shows the progress during loading and applying

### Detection States
 - Matching Dates:       The two dates matched (filtered out by default)
 - Small Difference:     A difference of less than 10 seconds
 - Normal Difference:    Any difference not covered by the other groups
 - Big Difference:       A difference larger than a year
 - Time Zone Difference: A difference that looks like a time zone offset (almost exactly on the hour)
 - Applied:              Files get marked as `Applied` after pressing the `Apply` button
 - No Meta Date:         Files which contain no time stamp in the meta data (filtered out by default)
 - Format Error:         Files which had an error during loading (filtered out by default)

### Time Zone Differences
File systems don't store time zone information with the file but store Coordinated Universal Time (UTC)
while displaying local time stamps to the user according to the time zone setting of the operating system.
Times stored in the meta data of media files is usually in local time at the time of creation. Most cameras
have no time zone setting anyway so this program displays all meta dates as such.

Because of this difference, media created in a different time zone will need to have an offset applied.
For example, if you have media taken in Singapore Standard Time (GMT+8) and you are now located in
Japan Standard Time (GMT+9), you have to set offset to +1 hour before applying the times to the file system.

## Command line
If launched via command line, it takes a path to the initial directory to be listed as the first argument.

## Credits
This program uses [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet) by Drew Noakes and contributors.

## License
Media File Date Fixer is available under the [Apache License Version 2.0](http://www.apache.org/licenses/LICENSE-2.0).
