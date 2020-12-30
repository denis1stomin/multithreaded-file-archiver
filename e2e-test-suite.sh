#!/bin/bash
# End-to-end test cases for GzipArchiver tool.
#

PROJ_PATH="./GzipArchiver/GzipArchiver.csproj"
SOURCE_PATH="./uncompressed.txt"
ARCHIVE_PATH="./compressed.gz"
EXTRACTED_PATH="./decompressed.txt"
RUN_CMD="dotnet run -c Release"
BUILD_CMD="dotnet build -c Release"


###########################################
# First build main project
$BUILD_CMD $PROJ_PATH

###########################################
# without any arguments
OUTPUT=$($RUN_CMD -p $PROJ_PATH)
EXIT_CODE=$(echo $?)
if (("1" -ne "$EXIT_CODE")); then
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
echo $(date) - Generating test data...
cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 10000 | head -n 100000 >$SOURCE_PATH
echo $(date) - Generated test file $SOURCE_PATH with size $(stat -c%s "$SOURCE_PATH")

rm $ARCHIVE_PATH* --force
OUTPUT=$($RUN_CMD -p $PROJ_PATH compress $SOURCE_PATH $ARCHIVE_PATH)
cat $ARCHIVE_PATH >/dev/null
ls $ARCHIVE_PATH*
# todo - check exit code and message
echo "$(date) - Finished compressing test archive."

rm $EXTRACTED_PATH --force
OUTPUT=$($RUN_CMD -p $PROJ_PATH decompress $ARCHIVE_PATH $EXTRACTED_PATH)
cat $EXTRACTED_PATH >/dev/null
diff $SOURCE_PATH $EXTRACTED_PATH | wc -c
echo "$(date) - Finished decompressing test archive."


###########################################
# main scenario with file bigger than RAM
# ...
#cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 1000000 | head -n 1000000 >$SOURCE_PATH
