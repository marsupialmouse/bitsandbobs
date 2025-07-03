#!/bin/bash
#
# Script that needs to be run manually by developers in order to ensure that
# the repository is configured correctly.
#
# The script simply links to the configuration files version in .config directory
# The following lines can be run manually in the command line as well.
# They modify the local .git/config file, which you can also modify by hand.
#
# The "optional" one may be omitted or removed from .git/config

# Note: The path here is relative to .git directory
git config --replace-all include.path ../.config/gitconfig.required

# On non-Windows systems, make scripts executable.
# This should not be necessary, as git should already check them out as executable, but doing it here
# just in case.
if command -v chmod > /dev/null; then
    chmod +x \
        .config/hooks/*
fi
