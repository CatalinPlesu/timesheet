## API calls — thin wrappers over kajax that update global state.

import karax / [karax, kajax]
import std / jsffi
import state

proc url(path: cstring): cstring = cstring(API_BASE & $path)

proc apiGet*(path: cstring; cb: proc(ok: bool; data: JsObject)) =
  loading = true
  errorMsg = cstring ""
  ajaxGet(url(path), authHeaders(), proc(status: int; resp: cstring) =
    loading = false
    if status == 401:
      clearToken()
      page = pgLogin
      errorMsg = cstring "Session expired — please log in again."
      redraw()
      return
    if status >= 200 and status < 300:
      cb(true, jsParseJson(resp))
    else:
      errorMsg = cstring("Request failed (" & $status & ")")
      cb(false, jsParseJson("{}"))
    redraw()
  )

proc apiPost*(path, body: cstring; cb: proc(ok: bool; data: JsObject)) =
  loading = true
  errorMsg = cstring ""
  ajaxPost(url(path), authHeaders(), body, proc(status: int; resp: cstring) =
    loading = false
    if status == 401:
      clearToken()
      page = pgLogin
      errorMsg = cstring "Session expired — please log in again."
      redraw()
      return
    let data = if resp != "" and resp != nil: jsParseJson(resp)
               else: jsParseJson("{}")
    if status >= 200 and status < 300:
      cb(true, data)
    else:
      errorMsg = cstring("Request failed (" & $status & ")")
      cb(false, data)
    redraw()
  )

proc apiDelete*(path: cstring; cb: proc(ok: bool)) =
  loading = true
  errorMsg = cstring ""
  ajaxDelete(url(path), authHeaders(), proc(status: int; _: cstring) =
    loading = false
    if status == 401:
      clearToken()
      page = pgLogin
      errorMsg = cstring "Session expired — please log in again."
      redraw()
      return
    if status >= 200 and status < 300:
      cb(true)
    else:
      errorMsg = cstring("Delete failed (" & $status & ")")
      cb(false)
    redraw()
  )

# ─── Domain calls ──────────────────────────────────────────────────────────────

proc fetchCurrentState*() =
  apiGet("/api/tracking/current", proc(ok: bool; data: JsObject) =
    if not ok: return
    let s = data["state"]
    if not s.isNull: trkState = s.toStr
    else:            trkState = cstring "Idle"
    let dur = data["durationHours"]
    trkDurHours = if dur.isNull: 0.0 else: dur.toFloat
    let sat = data["startedAt"]
    trkStartedAt = if sat.isNull: cstring "" else: sat.toStr
  )

proc doToggle*(state: cstring) =
  apiPost("/api/tracking/toggle",
    cstring("{\"state\":\"" & $state & "\"}"),
    proc(ok: bool; data: JsObject) =
      if not ok: return
      let msg = data["message"]
      if not msg.isNull: successMsg = msg.toStr
      fetchCurrentState()
  )

proc fetchEntries*() =
  let url = cstring("/api/entries?page=" & $entPage &
                    "&pageSize=25&groupBy=" & $entGroupBy)
  apiGet(url, proc(ok: bool; data: JsObject) =
    if not ok: return
    entData    = data
    entEntries = data["entries"]
  )

proc doDeleteEntry*(id: cstring) =
  apiDelete(cstring("/api/entries/" & $id), proc(ok: bool) =
    if not ok: return
    successMsg = cstring "Entry deleted."
    fetchEntries()
  )

proc fetchAnalytics*() =
  apiGet("/api/analytics/daily-averages", proc(ok: bool; data: JsObject) =
    if ok: anaData = data
  )

proc doLogin*(mnemonic: cstring) =
  loading = true
  errorMsg = cstring ""
  let body = cstring("{\"mnemonic\":\"" & $mnemonic & "\"}")
  ajaxPost(url(cstring "/api/auth/login"),
    [(cstring "Content-Type", cstring "application/json")],
    body,
    proc(status: int; resp: cstring) =
      loading = false
      if status == 200:
        let data = jsParseJson(resp)
        saveToken(data["accessToken"].toStr)
        page = pgTracking
        errorMsg = cstring ""
        fetchCurrentState()
      else:
        errorMsg = cstring "Invalid mnemonic. Please try again."
      redraw()
  )
