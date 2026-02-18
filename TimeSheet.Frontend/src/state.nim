## Global application state, types, and pure helpers.
##
## Compile-time flag: -d:API_BASE=http://localhost:5191
## Set to empty string for production (relative paths via nginx).
const API_BASE* {.strdefine.} = ""

import std / jsffi

# ─── JS glue ───────────────────────────────────────────────────────────────────

proc jsParseJson*(s: cstring): JsObject {.importjs: "JSON.parse(#)".}
proc jsStringify*(o: JsObject): cstring  {.importjs: "JSON.stringify(#)".}
proc jsLen*(o: JsObject): int   {.importjs: "#.length".}
proc isNull*(o: JsObject): bool {.importjs: "(# == null)".}
proc toStr*(o: JsObject): cstring {.importjs: "String(#)".}
proc toFloat*(o: JsObject): float {.importjs: "Number(#)".}
proc toInt*(o: JsObject): int     {.importjs: "Number(#)|0".}
proc toBool*(o: JsObject): bool   {.importjs: "!!(#)".}

proc jsNow*(): float {.importjs: "Date.now()".}
proc msToDateStr*(ms: float): cstring {.importjs: "(new Date(#)).toISOString().split('T')[0]".}

proc lsGet*(k: cstring): cstring    {.importjs: "(localStorage.getItem(#)||'')".}
proc lsSet*(k, v: cstring)          {.importjs: "localStorage.setItem(#,#)".}
proc lsRemove*(k: cstring)          {.importjs: "localStorage.removeItem(#)".}

# ─── Page enum ─────────────────────────────────────────────────────────────────

type Page* = enum pgLogin, pgTracking, pgEntries, pgAnalytics

# ─── App state ─────────────────────────────────────────────────────────────────

var
  page*        = pgLogin
  token*       = cstring ""
  errorMsg*    = cstring ""
  successMsg*  = cstring ""
  loading*     = false

  # Login
  mnemonicVal* = cstring ""

  # Tracking
  trkState*    = cstring "Idle"
  trkStartedAt* = cstring ""
  trkDurHours* = 0.0

  # Entries
  entPage*     = 1
  entGroupBy*  = cstring "Day"
  entData*:    JsObject
  entEntries*: JsObject

  # Analytics
  anaData*:      JsObject
  anaTab*        = cstring "stats"   # "stats" | "chart" | "calendar"
  anaPeriod*     = 30                # 7, 30, 90, 365
  anaStats*:     JsObject
  anaChart*:     JsObject
  anaBreakdown*: JsObject

# ─── Token helpers ─────────────────────────────────────────────────────────────

proc saveToken*(t: cstring) =
  token = t
  lsSet("ts_token", t)

proc clearToken*() =
  token = cstring ""
  lsRemove("ts_token")

proc authHeaders*(): seq[tuple[key, val: cstring]] =
  @[("Content-Type", cstring "application/json"),
    ("Authorization", cstring("Bearer " & $token))]

# ─── Pure helpers ──────────────────────────────────────────────────────────────

proc fmtDur*(h: float): cstring =
  let totalMin = int(h * 60.0)
  let hrs  = totalMin div 60
  let mins = totalMin mod 60
  if hrs > 0: cstring($hrs & "h " & $mins & "m")
  else:        cstring($mins & "m")

proc fmtDateTime*(s: cstring): cstring =
  ## Trims ISO 8601 to "YYYY-MM-DD HH:MM" for compact display.
  if s == "" or s.isNull: return cstring "—"
  var r = $s
  if r.len > 16: r = r[0 .. 15]
  if r.len > 10: r[10] = ' '   # replace 'T' with space
  cstring r
