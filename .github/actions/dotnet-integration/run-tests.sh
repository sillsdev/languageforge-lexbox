dotnet restore
dotnet build
pwsh backend/Testing/bin/Debug/net7.0/playwright.ps1 install
apt update && apt install patch
patch -u ./backend/Testing/bin/Debug/net7.0/runtimes/linux-x64/native/Mercurial/mercurial/httppeer.py <<EOF
--- a/mercurial/httppeer.py     Sun Feb 01 18:47:04 2015 -0600
+++ b/mercurial/httppeer.py     Wed Jul 19 16:24:19 2023 +0700
@@ -20,8 +20,8 @@
             while chunk:
                 yield zd.decompress(chunk, 2**18)
                 chunk = zd.unconsumed_tail
-    except httplib.HTTPException:
-        raise IOError(None, _('connection ended unexpectedly'))
+    except httplib.HTTPException as e:
+        raise IOError(None, _('connection ended unexpectedly') + ': ' + repr(e))
     yield zd.flush()

 class httppeer(wireproto.wirepeer):
EOF
dotnet test --logger trx --results-directory ./testresults --filter Category=Integration
