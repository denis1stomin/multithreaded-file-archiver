#!/bin/bash
# End-to-end test cases for GzipArchiver tool.
#

PROJ_PATH=./GzipArchiver/GzipArchiver.csproj
SOURCE_PATH=./uncompressed.txt
ARCHIVE_PATH=./compressed.gz
EXTRACTED_PATH=./decompressed.txt


###########################################
# without any arguments
OUTPUT=$(dotnet run -p $PROJ_PATH)
if (("$?" -ne "")); then
    echo Failed
fi

if !((echo $OUTPUT | grep -q "Not enough input parameters")); then
    echo Wrong error message
fi


###########################################
# other bad arguments cases
# ...


###########################################
# main scenarios with average file
cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 1000 | head -n 2000 >$SOURCE_PATH

rm $ARCHIVE_PATH* --force
OUTPUT=$(dotnet run -p $PROJ_PATH compress $SOURCE_PATH $ARCHIVE_PATH)
cat $ARCHIVE_PATH >/dev/null
ls $ARCHIVE_PATH*
# todo - check exit code and message

rm $EXTRACTED_PATH --force
OUTPUT=$(dotnet run -p $PROJ_PATH decompress $ARCHIVE_PATH $EXTRACTED_PATH)
cat $EXTRACTED_PATH >/dev/null
diff $SOURCE_PATH $EXTRACTED_PATH | wc -c


###########################################
# main scenario with file bigger than RAM
# ...
#cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 1000000 | head -n 1000000 >$SOURCE_PATH
