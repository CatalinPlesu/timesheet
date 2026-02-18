import karax / [karax, karaxdsl, vdom, kdom]
import ../state
import ../api

proc trackBtn(label, state: cstring; active: bool): VNode =
  let cls: cstring = if active: "btn-active" else: ""
  buildHtml(button(class = cls)):
    proc onclick(ev: Event; n: VNode) =
      doToggle(state)
    text label

proc renderTracking*(): VNode =
  buildHtml(main(class = "container")):
    h2: text "Tracking"
    if successMsg != "":
      p(class = "success"): text successMsg
    if errorMsg != "":
      p(class = "error"): text errorMsg

    article:
      header: text "Current state"
      if loading:
        p(`aria-busy` = "true"): text "Loading…"
      else:
        p(class = "state-label"): text trkState
        if trkState != "Idle" and trkDurHours > 0.0:
          p: text fmtDur(trkDurHours)

    article:
      header: text "Toggle"
      tdiv(class = "grid-3"):
        trackBtn("Commute", "Commuting", trkState == "Commuting")
        trackBtn("Work",    "Working",   trkState == "Working")
        trackBtn("Lunch",   "Lunch",     trkState == "Lunch")
