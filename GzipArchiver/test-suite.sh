rm input.txt --force
rm output.gz --force

# without any arguments
OUTPUT=$(dotnet run)
if (("$?" -ne "1")); then
    echo Failed
fi
echo $OUTPUT | grep -e "Not enough2"

# not enough args
