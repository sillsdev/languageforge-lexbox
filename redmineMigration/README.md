# Migration scripts 

This folder contains scripts related to the transition from the old language depot VM to the new language depot running on the new lexbox service

## Scripts in this folder are intended to be run sequentially as follows

`loadSql/getSqlFromServer.sh` - download a SQL dump from language depot
`loadSql/loadSql.sh` - run up a docker container with the SQL dump loaded into it
`compareFolders/compareFolders.sh` - download a directory listing of folders on language depot, display a report, and create files for further analysis
`compareSql/getSqlProjectCodes.sh` - extract language depot project codes from the local mariadb docker instance
`compareSql/compareProjectDbAndFolders.sh` - create a report that shows projects that are missing either a folder or a SQL entry - i.e. the project is broken and cannot be used.  Conclusion: these projects should be removed before migration
