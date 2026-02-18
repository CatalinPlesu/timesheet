# Package
version     = "0.1.0"
author      = "TimeSheet"
description = "TimeSheet frontend — Nim/Karax SPA"
license     = "MIT"
srcDir      = "src"

# Dependencies
requires "nim >= 2.0.0"
requires "karax >= 1.3.0"

task buildjs, "Compile to JavaScript (release)":
  mkDir "dist"
  exec "nim js -d:release -d:danger --out:dist/app.js src/app.nim"

task devjs, "Compile to JavaScript (debug, respects API_BASE env var)":
  mkDir "dist"
  let apiBase = getEnv("API_BASE", "")
  let define  = if apiBase != "": " --define:API_BASE=" & apiBase else: ""
  exec "nim js" & define & " --out:dist/app.js src/app.nim"
