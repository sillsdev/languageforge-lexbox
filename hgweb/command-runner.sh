#!/bin/bash

# Define the list of allowed commands
allowed_commands=("verify" "tip" "tipdate" "ldmlzip" "reposizeinkb" "wesaylexentrycount" "lexentrycount" "flexprojectid" "flexwritingsystems" "flexmodelversion" "recover" "healthz" "invalidatedircache")

# Get the project code and command name from the URL
IFS='/' read -ra PATH_SEGMENTS <<< "$PATH_INFO"
project_code="${PATH_SEGMENTS[1]}"
command_name="${PATH_SEGMENTS[2]}"

# Ensure the project code and command name are safe to use in a shell command
if [[ ! "$project_code" =~ ^[a-z0-9][a-z0-9-]*$ ]] || [[ ! "$command_name" =~ ^[a-zA-Z0-9]+$ ]]; then
    echo "Content-type: text/plain"
    echo "Status: 400 Bad Request"
    echo ""
    echo "Invalid project code or command name."
    echo "Project code: $project_code"
    echo "Command name: $command_name"
    exit 1
fi

# Check if the command is in the list of allowed commands
if [[ ! " ${allowed_commands[@]} " =~ " ${command_name} " ]]; then
    echo "Content-type: text/plain"
    echo "Status: 400 Bad Request"
    echo ""
    echo "Invalid command. Allowed commands are: ${allowed_commands[*]}"
    echo "Command name: $command_name"
    exit 1
fi

if [[ $command_name == "healthz" ]]; then
    echo "lexbox-version: $APP_VERSION"
    echo "Status: 200 OK"
    echo "Content-type: text/plain"
    echo ""
    echo "$APP_VERSION"
    exit 0
fi

if [[ $command_name == "ldmlzip" ]]; then
    # Preflight check: ldml zip access is only allowed if LexiconSettings.plsx contains addToSldr="true"
    first_char=$(echo $project_code | cut -c1)
    if (chg --cwd /var/hg/repos/$first_char/$project_code cat -r tip CachedSettings/SharedSettings/LexiconSettings.plsx | grep '<WritingSystems' | grep 'addToSldr="true"' >/dev/null); then
        CONTENT_TYPE="application/zip"
    else
        echo "Content-type: text/plain"
        echo "Status: 403 Forbidden"
        echo ""
        echo "Forbidden. Project does not allow sharing writing systems with SLDR or project does not exist"
        exit 1
    fi
fi

CONTENT_TYPE="${CONTENT_TYPE:-text/plain}"
# Start outputting the result right away so the HTTP connection won't be timed out
echo "Content-type: ${CONTENT_TYPE}"
echo ""

# Run the hg command, simply output to stdout
first_char=$(echo $project_code | cut -c1)
# Ensure NFS cache is refreshed in case project repo changed in another pod (e.g., project reset)
ls /var/hg/repos/$first_char/$project_code/.hg >/dev/null 2>/dev/null  # Don't need output; this is enough to refresh NFS dir cache
# Sometimes invalidatedircache is called after deleting a project, so the cd would fail. So exit fast in that case.
[ "x$command_name" = "xinvalidatedircache" ] && exit 0
cd /var/hg/repos/$first_char/$project_code
case $command_name in

    lexentrycount)
        # The \b for word boundary is necessary to distinguish LexEntry from LexEntryType and similar
        chg cat -r tip Linguistics/Lexicon/Lexicon_{01,02,03,04,05,06,07,08,09,10}.lexdb | grep -c '<LexEntry\b'
        ;;

    wesaylexentrycount)
        # Can't predict .lift filename, but we can ask Mercurial for it
        LIFTFILE=$(chg manifest -r tip | grep '\.lift$' | head -n 1)
        # The \b for word boundary is not necessary for .lift files
        [ -n "${LIFTFILE}" ] && (chg cat -r tip "${LIFTFILE}" | grep -c '<entry') || echo 0
        ;;

    flexprojectid)
        # grep -o extracts only what matches the pattern; -P turns on Perl-compatible regex, and \K is a Perl regex flag
        # that means "consider the previous text a lookbehind assertion, and don't include it in the match."
        # https://perldoc.perl.org/perlre#%5CK says the `\K` construct "may be significantly more efficient" than "standard" lookbehind assertions like `(?<=...)`
        chg cat -r tip General/LanguageProject.langproj | sed -n -e '/<LangProject/,/>/p' | grep -oP 'guid="\K[^"]+'
        ;;

    flexwritingsystems)
        chg cat -r tip General/LanguageProject.langproj | sed -n -e '/<AnalysisWss>/,/<\/AnalysisWss>/p' -e '/<VernWss>/,/<\/VernWss>/p' -e '/<CurAnalysisWss>/,/<\/CurAnalysisWss>/p' -e '/<CurVernWss>/,/<\/CurVernWss>/p' -e '/<CurPronunWss>/,/<\/CurPronunWss>/p'
        ;;

    flexmodelversion)
        chg cat -r tip FLExProject.ModelVersion
        ;;

    tip)
        chg tip --template '{node}'
        ;;

    tipdate)
        chg tip --template '{date|hgdate}'
        ;;

    reposizeinkb)
        du -ks .hg | cut -f1
        ;;

    ldmlzip)
        # -p '.' so that resulting zipfiles will *not* have the project name in the file paths
        chg archive -p '.' -t zip -r tip -I 'CachedSettings/WritingSystemStore/*.ldml' -
        ;;

    verify)
        # Env var PYTHONUNBUFFERED required for commands like verify and recover, so that output can stream back to the project page
        export PYTHONUNBUFFERED=1
        # Need a timeout so hg verify won't take forever on the "checking files" step
        timeout 5 chg verify 2>&1
        ;;

    *)
        # Env var PYTHONUNBUFFERED required for commands like verify and recover, so that output can stream back to the project page
        PYTHONUNBUFFERED=1 chg $command_name 2>&1
        ;;
esac
