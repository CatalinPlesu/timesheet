import karax / [karax, karaxdsl, vdom, kdom]
import std / jsffi
import ../state
import ../api

proc groupBtn(label, gb: cstring): VNode =
  let cls: cstring = if gb == entGroupBy: "secondary" else: "outline secondary"
  buildHtml(button(class = cls)):
    proc onclick(ev: Event; n: VNode) =
      entGroupBy = gb
      entPage = 1
      fetchEntries()
    text label

proc entryRow(e: JsObject): VNode =
  let id     = e["id"].toStr
  let state  = e["state"].toStr
  let start  = fmtDateTime(e["startedAt"].toStr)
  let endt   = if e["endedAt"].isNull: cstring "—"
               else: fmtDateTime(e["endedAt"].toStr)
  let dur    = if e["durationHours"].isNull: cstring "—"
               else: fmtDur(e["durationHours"].toFloat)
  let active = e["isActive"].toBool
  buildHtml(tr):
    td: text state
    td: text start
    td: text endt
    td: text dur
    td:
      if not active:
        let eid = id
        button(class = "outline secondary btn-compact"):
          proc onclick(ev: Event; n: VNode) =
            doDeleteEntry(eid)
          text "✕"

proc renderEntries*(): VNode =
  buildHtml(main(class = "container")):
    h2: text "Entries"
    if successMsg != "":
      p(class = "success"): text successMsg
    if errorMsg != "":
      p(class = "error"): text errorMsg

    tdiv(class = "groupby-bar"):
      groupBtn("Day",   "Day")
      groupBtn("Week",  "Week")
      groupBtn("Month", "Month")
      groupBtn("Year",  "Year")

    if loading:
      p(`aria-busy` = "true"): text "Loading…"
    elif entEntries.isNull or entEntries.jsLen == 0:
      p: text "No entries."
    else:
      figure:
        table(role = "grid"):
          thead:
            tr:
              th: text "Type"
              th: text "Started"
              th: text "Ended"
              th: text "Duration"
              th: text ""
          tbody:
            for i in 0 ..< entEntries.jsLen:
              entryRow(entEntries[i])

      let totalPages = if entData.isNull: 1
                       else: entData["totalPages"].toInt
      tdiv(class = "pagination"):
        if entPage > 1:
          button(class = "outline"):
            proc onclick(ev: Event; n: VNode) =
              dec entPage
              fetchEntries()
            text "← Prev"
        p:
          text cstring("Page " & $entPage & " / " & $totalPages)
        if entPage < totalPages:
          button(class = "outline"):
            proc onclick(ev: Event; n: VNode) =
              inc entPage
              fetchEntries()
            text "Next →"
